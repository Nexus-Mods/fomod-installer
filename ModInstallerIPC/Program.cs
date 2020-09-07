using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace ModInstallerIPC
{
    static class Program
    {
        static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            Console.Error.WriteLine(e.Exception.Message);
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.Error.WriteLine((e.ExceptionObject as Exception).Message);
        }

        static IDictionary<string, string> parseCommandline(string[] args)
        {
            IDictionary<string, string> result = new Dictionary<string, string>();
            int idx = 0;
            while (idx < args.Length)
            {
                if (args[idx].Equals("--pipe", StringComparison.InvariantCultureIgnoreCase))
                {
                    result["pipe"] = "true";
                } else
                {
                    result["$"] = args[idx];
                }
                ++idx;
            }

            return result;
        }

        static int Main(string[] args)
        {
            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);

            // Add the event handler for handling non-UI thread exceptions to the event.
            AppDomain.CurrentDomain.UnhandledException += new
                UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            IDictionary<string, string> cmdLine = parseCommandline(args);

            try
            {
                Server server = new Server(cmdLine["$"], cmdLine.ContainsKey("pipe"), false);
                server.HandleMessages();

                return 0;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Exception: " + e.Message);
                return 1;
            }
        }
    }
}
