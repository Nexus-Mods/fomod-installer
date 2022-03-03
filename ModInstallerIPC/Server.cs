using FomodInstaller.Interface;
using FomodInstaller.ModInstaller;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ModInstallerIPC
{
    struct Defaults
    {
        public static int TIMEOUT_MS = 5000;
    }

    struct OutMessage
    {
        public string id;
        public object callback;
        public object data;
        public object error;
    }

    public class Helpers
    {
        /**
         * prepare a dynamic object from Json.Net to be passed into the fomod installer such that it
         * can be unpacked correctly
         */
        public static object ValueNormalize(object input)
        {
            if (input == null)
            {
                return null;
            }

            if (input.GetType() == typeof(JValue))
            {
                input = ((JValue)input).Value;
            }

            if (input.GetType() == typeof(JArray))
            {
                dynamic binder = new ArrayBinder((JArray)input);
                return (object[])binder;
            }
            else if (input.GetType() == typeof(JObject))
            {
                IDictionary<string, object> res = ((JObject)input).ToObject<IDictionary<string, object>>();
                if (res.ContainsKey("type") && ((string)res["type"] == "Buffer"))
                {
                    return Convert.FromBase64String((string)res["data"]);
                } else {
                    return new DictWrap(res);
                }
            }
            else if (input.GetType() == typeof(long))
            {
                // rebox System.Int64 to System.Int32 because that's what the installer is
                // expecting and I found no good way to do the cast implicitly as necessary.
                // my C#-fu is just to weak
                return (int)(long)input;
            }
            else
            {
                return input;
            }
        }

        /**
         * dynamic object allowing access to a dictionary through regular object syntax (obj.foobar instead of dict["foobar"]).
         * currently read only
         */
        public class DictWrap : DynamicObject
        {

            private IDictionary<string, object> mWrappee;
            public DictWrap(IDictionary<string, object> dict)
            {
                mWrappee = dict;
            }

            public override bool TryGetMember(GetMemberBinder binder, out object result)
            {
                if (mWrappee.ContainsKey(binder.Name))
                {
                    result = ValueNormalize(mWrappee[binder.Name]);
                    return true;
                }
                else
                {
                    result = null;
                    return false;
                }
            }
        }

        /**
         * Allow a dynamic list type (JArray) to be implicitly converted to an array or any enumerable
         * as needed
         **/
        private class ArrayBinder : DynamicObject
        {
            private readonly JArray mArray;

            public ArrayBinder(JArray arr)
            {
                mArray = arr;
            }

            public override bool TryConvert(ConvertBinder binder, out object result)
            {
                IEnumerable<object> arr = mArray.Select(token => ValueNormalize(token));

                // some kind of array
                if (binder.Type.IsArray)
                {
                    result = arr.ToArray();
                }
                else if (binder.Type.GetMethod("GetEnumerator") != null)
                {
                    result = arr;
                } else
                {
                    result = null;
                }
                return true;
            }
        }
    }

    /**
     * A dynamic object that acts as if it has any function the caller asks for.
     * The function signature can only be "Task<object> func(object arg)" or 
     * "Task<object> func(object[] args)"
     * Any call to that function will be relayed as an ipc to the client and the response returned
     **/
    public class Context : DynamicObject
    {
        private class BindLater : DynamicObject
        {
            private readonly string mName;
            private readonly Func<string, object[], Task<dynamic>> mCallIPC;

            public BindLater(string name, Func<string, object[], Task<dynamic>> callIPC)
            {
                mName = name;
                mCallIPC = callIPC;
            }

            public override bool TryConvert(ConvertBinder binder, out object result)
            {
                if (binder.Type.GenericTypeArguments[0].Name == "Object")
                {
                    // provides only one parameter
                    Func<object, Task<object>> res = args => mCallIPC(mName, new object[] { args });
                    result = res;
                } else
                {
                    // provides multiple parameters
                    Func<object[], Task<object>> res = args => mCallIPC(mName, args);
                    result = res;
                }

                return true;
            }
        }

        private readonly Func<string, object[], Task<dynamic>> mCallIPC;
        public Context(Func<string, object[], Task<object>> callIPC)
        {
            this.mCallIPC = callIPC;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = new BindLater(binder.Name, mCallIPC);
            return true;
        }
    }

    class Server
    {
        /**
         * Turn a proper object into an ExpandoObject
         */
        private static dynamic ToExpando(object value)
        {
            IDictionary<string, object> expando = new ExpandoObject();

            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(value.GetType()))
            {
                expando.Add(property.Name, property.GetValue(value));
            }

            return expando as ExpandoObject;
        }

        private class DeferContext
        {
            public Context plugin { get; private set; }
            public Context ini { get; private set; }
            public Context context { get; private set; }
            public Context ui { get; private set; }

            public DeferContext(string id, Func<string, string, string, object[], Task<object>> ContextIPC)
            {
                plugin = new Context((string name, object[] args) => ContextIPC(id, "plugin", name, args));
                ini = new Context((string name, object[] args) => ContextIPC(id, "ini", name, args));
                context = new Context((string name, object[] args) => ContextIPC(id, "context", name, args));
                ui = new Context((string name, object[] args) => ContextIPC(id, "ui", name, args));
            }
        }

        /**
         * serializer to be used in conversion to json that converts function into primitive types that can be serialized.
         * The functions are turned into an object like this: { "__callback": "<id>" }
         * The peer can then invoke that callback by sending { "command": "Invoke", data: { "requestId": "<id>", "callbackId": "<id>", "args": [...] } }
         * The requestId is the id of the initial call (TestSupported or Install) the peer initiated the communication with, callbackId is the id
         *   we sent with __callback and args are the parameters to be passed to the callback. It's the peers responsible to pass the correct types
         */
        private class FunctionSerializer : JsonConverter
        {
            public FunctionSerializer()
            {
            }

            public Dictionary<string, Delegate> callbacks { get; } = new Dictionary<string, Delegate>();

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                Type type = value.GetType();
                System.Func<object, Task<object>> func = (System.Func<object, Task<object>>)value;

                string id = GenerateId();
                callbacks[id] = func;

                JToken t = JToken.FromObject(new { __callback = id });

                t.WriteTo(writer);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
            }

            public override bool CanRead
            {
                get { return false; }
            }

            public override bool CanConvert(Type objectType)
            {
                var methods = objectType.GetMethods();
                return objectType.GetMethod("DynamicInvoke") != null;
            }
        }

        private class BufferSerializer : JsonConverter
        {
            public BufferSerializer() { }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                JToken res = JToken.FromObject(new
                {
                    type = "Buffer",
                    data = Convert.ToBase64String((byte[])value),
                });
                res.WriteTo(writer);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
            }

            public override bool CanRead
            {
                get { return false; }
            }

            public override bool CanConvert(Type objectType)
            {
                return objectType.IsArray && objectType.GetElementType() == typeof(byte);
            }
        }

        private readonly string mId;
        private readonly bool mListen;
        private bool mUsePipe = false;
        private Installer mInstaller;
        private Dictionary<string, TaskCompletionSource<object>> mAwaitedReplies;
        private Dictionary<string, Dictionary<string, Delegate>> mCallbacks;
        private Action<OutMessage> mEnqueue;

        /**
         * listens on a port and handles TestSupported/Install commands to deal with fomod installation
         */
        public Server(string id, bool pipe, bool listen)
        {
            mId = id;
            mListen = listen;
            mUsePipe = pipe;
            mInstaller = new Installer();

            mAwaitedReplies = new Dictionary<string, TaskCompletionSource<object>>();
            mCallbacks = new Dictionary<string, Dictionary<string, Delegate>>();
        }

        public void HandleMessages()
        {
            var enc = new UTF8Encoding(false);
            Stream streamIn = null;
            Stream streamOut = null;
            if (mUsePipe) {
                var pipeIn = new NamedPipeClientStream(mId);
                Console.Out.WriteLine("in pipe ready");
                pipeIn.Connect(30000);
                Console.Out.WriteLine("in pipe connected");

                var pipeOut = new NamedPipeServerStream(mId + "_reply");
                Console.Out.WriteLine("out pipe ready");

                pipeOut.WaitForConnection();
                Console.Out.WriteLine("out pipe connected");

                streamIn = pipeIn;
                streamOut = pipeOut;
            } else
            {
                // use a single network socket
                TcpClient client = new TcpClient();
                try
                {
                    client.Connect("localhost", Int32.Parse(mId));
                } catch (Exception e)
                {
                    Console.Error.WriteLine("failed to connect to local port {0}: {1}", mId, e.Message);
                    throw e;
                }
                NetworkStream stream = client.GetStream();
                streamIn = streamOut = stream;
            }
            // writer.AutoFlush = true;

            // a queue where dequeuing blocks while the queue is empty
            BlockingCollection<OutMessage> outgoing = new BlockingCollection<OutMessage>();

            mEnqueue = msg => outgoing.Add(msg);

            CancellationTokenSource cancelSignal = new CancellationTokenSource();

            Exception cancelEx = null;

            Task readerTask = Task.Run(() => {
                Thread.CurrentThread.Name = "reader loop";
                try
                {
                    ReaderLoop(streamIn, mEnqueue);
                    outgoing.CompleteAdding();
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("read loop failed: {0}", e.Message);
                    cancelSignal.Cancel();
                    cancelEx = e;
                };
            });
            Task writerTask = Task.Run(() => {
                Thread.CurrentThread.Name = "writer loop";
                try
                {
                    WriterLoop(streamOut, outgoing);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("write loop failed: {0}", e.Message);
                    cancelSignal.Cancel();
                    cancelEx = e;
                }
            });

            // run until either reader or writer signal for termination
            try
            {
                Task.WaitAll(new Task[] { readerTask, writerTask }, cancelSignal.Token);
            } catch (Exception err)
            {
                throw cancelEx ?? err;
            }
            finally
            {
                cancelSignal.Dispose();
            }
        }

        private void ReaderLoop(Stream stream, Action<OutMessage> onSend)
        {
            bool running = true;

            int bufferSize = 64 * 1024;
            int offset = 0;
            byte[] buffer = new byte[bufferSize];

            // this handles all messages from the client
            while (running)
            {
                int left = bufferSize - offset;

                // int numRead = reader.Read(buffer, offset, left);
                int numRead = stream.Read(buffer, offset, left);
                if (numRead == 0)
                {
                    running = false;
                }
                else if (numRead == left)
                {
                    // buffer too small, double it
                    bufferSize = bufferSize * 2;
                    byte[] newBuffer = new byte[bufferSize];
                    Array.Copy(buffer, newBuffer, buffer.Length);
                    offset = buffer.Length;
                    buffer = newBuffer;
                }
                else
                {
                    var input = System.Text.Encoding.UTF8.GetString(buffer, 0, numRead + offset);
                    // var input = new string(buffer, 0, numRead + offset);
                    offset = 0;

                    foreach (var msg in input.Split('\uFFFF'))
                    {
                        if (msg.Length == 0)
                        {
                            continue;
                        }

                        OnReceived(msg).ContinueWith(reply =>
                        {
                            onSend(reply.Result);
                        });
                    }
                }
            }
        }

        private void WriterLoop(Stream writer, BlockingCollection<OutMessage> outgoing)
        {
            bool running = true;

            try
            {
                while (running)
                {
                    OutMessage msg = outgoing.Take();
                    SendResponse(writer, msg);
                }
            } catch (InvalidOperationException)
            {
                // queue was closed by the producer
                return;
            }
        }

        private static string GenerateId()
        {
            return Guid.NewGuid().ToString("N");
        }

        private Task<object> ContextIPC(string targetId, string targetType, string name, object[] args)
        {
            string id = GenerateId();

            Task<object> result = AwaitReply(id);

            try
            {
                mEnqueue(new OutMessage
                {
                    id = id,
                    callback = new { id = targetId, type = targetType },
                    data = new { name, args }
                });
            } catch (Exception e)
            {
                Console.Error.WriteLine("Failed to enqueue message: {0}", e.Message);
            }

            return result;
        }

        private Task<object> AwaitReply(string id)
        {
            var repliesCompletion = new TaskCompletionSource<object>();
            mAwaitedReplies.Add(id, repliesCompletion);
            return repliesCompletion.Task;
        }

        private void SendResponse(Stream writer, OutMessage resp)
        {
            FunctionSerializer funcSer = new FunctionSerializer();
            BufferSerializer buffSer = new BufferSerializer();
            string serialized = JsonConvert.SerializeObject(new { resp.id, resp.callback, resp.data, resp.error }, funcSer, buffSer);
            mCallbacks[resp.id] = funcSer.callbacks;
            var input = System.Text.Encoding.UTF8.GetBytes(serialized + "\uFFFF");
            writer.Write(input, 0, input.Length);
        }

        private async Task<object> DispatchTestSupported(JObject data)
        {
            var files = new List<string>(data["files"].Children().ToList()
                .Select(input => input.ToString()));

            var allowedTypes = new List<string>(data["allowedTypes"].Children().ToList()
                .Select(input => input.ToString()));

            Dictionary<string, object> result = await mInstaller.TestSupported(files, allowedTypes);
            return result;
        }

        private async Task<object> DispatchInstall(string id, JObject data)
        {
            var files = new List<string>(data["files"].Children().ToList().Select(input => input.ToString()));
            var stopPatterns = new List<string>(data["stopPatterns"].Children().ToList().Select(input => input.ToString()));
            var pluginPath = data["pluginPath"].ToString();
            var scriptPath = data["scriptPath"].ToString();
            bool validate = true;
            JToken val;
            if (data.TryGetValue("validate", out val))
            {
                validate = val.ToObject<bool>();
            }

            dynamic choices;
            try
            {
                choices = data["fomodChoices"];
            }
            catch (RuntimeBinderException) {
                choices = null;
            }

            DeferContext context = new DeferContext(id, (targetId, targetType, name, args) => ContextIPC(targetId, targetType, name, args));
            CoreDelegates coreDelegates = new CoreDelegates(ToExpando(context));

            return await mInstaller.Install(files, stopPatterns, pluginPath, scriptPath, choices, validate, (ProgressDelegate)((int progress) => { }), coreDelegates);
        }

        private async Task<object> DispatchInvoke(JObject data)
        {
            string requestId = data["requestId"].ToString();
            string callbackId = data["callbackId"].ToString();
            var argsList = data["args"].ToList();
            dynamic[] args = data["args"].ToList().Select(input => Helpers.ValueNormalize(input)).ToArray();
            if (args.Length == 0)
            {
                args = new object[] { null };
            }
            object res = mCallbacks[requestId][callbackId].DynamicInvoke(args);
            return await (Task<object>)res;
        }

        private void DispatchReply(dynamic data)
        {
            string id = data.request.id;
            if (!mAwaitedReplies.ContainsKey(id))
            {
                // not waiting for this (any more)? Might have been a timeout
                return;
            }
            if (data.error != null)
            {
                mAwaitedReplies[id].SetException(new Exception(data.error.message.ToString()));
            }
            else
            {
                try
                {
                    mAwaitedReplies[id].SetResult(Helpers.ValueNormalize(data.data));
                } catch (RuntimeBinderException)
                {
                    mAwaitedReplies[id].SetException(new Exception(String.Format("No data and no error field in {0}", data.ToString())));
                }
            }
        }

        private async Task<object> Dispatch(string id, JObject data)
        {
            if (mAwaitedReplies.ContainsKey(id))
            {
                mAwaitedReplies[id].SetResult(data);
            }
            string command = data["command"].ToString();
            switch (command)
            {
                case "TestSupported": return await DispatchTestSupported(data);
                case "Install": return await DispatchInstall(id, data);
                case "Invoke": return await DispatchInvoke(data);
                case "Reply":
                    {
                        DispatchReply(data);
                        return null;
                    }
                case "Quit":
                    {
                        // don't think we need to do anything, this shuts down cleanly when the peer closes the socket
                        return "";
                    }
            }
            return "";
        }

        private async Task<OutMessage> OnReceived(string data)
        {
            JObject invocation;
            try
            {
                invocation = JObject.Parse(data);
            } catch (Exception e)
            {
                Console.Error.WriteLine("Failed to parse json (error: {0}): {1}", e.Message, data);
                return await Task.Run(() =>
                {
                    return new OutMessage { id = "parseerror", error = new { name = "ParseError", message = e.Message, stack = e.StackTrace } };
                });
            }
            string id = invocation["id"].ToString();
            return await Task.Run(async () =>
            {
                try
                {
                    return new OutMessage { id = id, data = await Dispatch(id, (JObject)invocation["payload"]) };
                }
                catch (Exception e)
                {
                    Exception err = e;
                    string message = e.Message;

                    while (err.InnerException != null)
                    {
                        err = err.InnerException;
                        message += "; " + err.Message;
                    }
                    return new OutMessage {
                        id = id,
                        error = new {
                            name = e.GetType().FullName,
                            message = message,
                            stack = e.StackTrace,
                            data = e.Data,
                        }
                    };
                }
            });
        }
    }
}
