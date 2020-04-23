using FomodInstaller.Interface;
using FomodInstaller.ModInstaller;
using Microsoft.CSharp.RuntimeBinder;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Utils;

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
                return new ArrayBinder((JArray)input);
            }
            else if (input.GetType() == typeof(JObject))
            {
                IDictionary<string, object> res = ((JObject)input).ToObject<IDictionary<string, object>>();
                if (res.ContainsKey("type") && ((string)res["type"] == "Buffer"))
                {
                    JArray numArray = (JArray)res["data"];
                    IEnumerable<byte> result = numArray.Select(tok => tok.Value<byte>());
                    return result.ToArray();
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
         * (does this even work? is it necessary?)
         */
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

        private readonly int mPort;
        private readonly bool mListen;
        private Installer mInstaller;
        private NetMQQueue<OutMessage> mMessageQueue;
        private NetMQPoller mPoller;
        private Dictionary<string, TaskCompletionSource<object>> mAwaitedReplies;
        private Dictionary<string, Dictionary<string, Delegate>> mCallbacks;

        /**
         * listens on a port and handles TestSupported/Install commands to deal with fomod installation
         */
        public Server(int port, bool listen)
        {
            mPort = port;
            mListen = listen;
            mInstaller = new Installer();

            mAwaitedReplies = new Dictionary<string, TaskCompletionSource<object>>();
            mCallbacks = new Dictionary<string, Dictionary<string, Delegate>>();
        }

        public void HandleMessages()
        {
            using (var server = new PairSocket())
            using (mMessageQueue = new NetMQQueue<OutMessage>())
            using (mPoller = new NetMQPoller { server })
            {
                if (mListen)
                {
                    server.Bind("tcp://localhost:" + this.mPort);
                }
                else
                {
                    server.Connect("tcp://localhost:" + this.mPort);
                }
                mPoller.Add(mMessageQueue);

                mMessageQueue.ReceiveReady += (object sender, NetMQQueueEventArgs<OutMessage> args) =>
                {
                    SendResponse(server, mMessageQueue.Dequeue());
                };
                server.ReceiveReady += async (object sender, NetMQSocketEventArgs args) =>
                {
                    mMessageQueue.Enqueue(await OnReceived(args));
                };

                mPoller.Run();
            }
            mMessageQueue = null;
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
                // wow, ok, so, umm, this code is run in the context of the main/default appdomain but because
                // it's run "on behalf of" the sandbox appdomain we have to raise our permissions again.
                // Probably totally obvious to an experienced .net developer but I find this security system and
                // this api - surprising.
                // I didn't even know NetMQ was going to run unmanaged code or that it was my job to assert the
                // necessary right...
                new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Assert();
                mMessageQueue.Enqueue(new OutMessage
                {
                    id = id,
                    callback = new { id = targetId, type = targetType },
                    data = new { name, args }
                });
            } catch (Exception e)
            {
                Console.WriteLine("Failed to enqueue message: {0}", e.Message);
            }

            return result;
        }

        private Task<object> AwaitReply(string id)
        {
            var repliesCompletion = new TaskCompletionSource<object>();
            mAwaitedReplies.Add(id, repliesCompletion);
            return repliesCompletion.Task;
        }

        private void SendResponse(PairSocket server, OutMessage resp)
        {
            Msg msg = new Msg();
            FunctionSerializer funcSer = new FunctionSerializer();
            string serialized = JsonConvert.SerializeObject(new { resp.id, resp.callback, resp.data, resp.error }, funcSer);
            mCallbacks[resp.id] = funcSer.callbacks;
            byte[] encoded = Encoding.UTF8.GetBytes(serialized);
            msg.InitGC(encoded, encoded.Length);
            server.Send(ref msg, false);
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

            dynamic choices;
            try
            {
                choices = data["choices"];
            }
            catch (RuntimeBinderException) {
                choices = null;
            }

            DeferContext context = new DeferContext(id, (targetId, targetType, name, args) => ContextIPC(targetId, targetType, name, args));
            CoreDelegates coreDelegates = new CoreDelegates(ToExpando(context));

            return await mInstaller.Install(files, stopPatterns, pluginPath, scriptPath, choices, (ProgressDelegate)((int progress) => { }), coreDelegates);
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
                        mPoller.Stop();
                        return "";
                    }
            }
            return "";
        }

        private async Task<OutMessage> OnReceived(NetMQSocketEventArgs evt)
        {
            Msg msg = new Msg();
            msg.InitEmpty();
            evt.Socket.Receive(ref msg);
            JObject invocation = JObject.Parse(Encoding.UTF8.GetString(msg.Data));
            string id = invocation["id"].ToString();
            return await Task.Run(async () =>
            {
                try
                {
                    return new OutMessage { id = id, data = await Dispatch(id, (JObject)invocation["payload"]) };
                }
                catch (Exception e)
                {
                    return new OutMessage { id = id, error = new { name = e.GetType().FullName, message = e.Message, stack = e.StackTrace } };
                }
            });
        }
    }
}
