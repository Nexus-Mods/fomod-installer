using System;
using System.Collections.Generic;
using System.Globalization;
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
#if NET9_0_OR_GREATER
            Application.SetHighDpiMode(HighDpiMode.DpiUnawareGdiScaled);
#endif
            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // oh dear god .NET, make the bloody localized error messages stop, it's the dumbest thing ever...
            Thread.CurrentThread.CurrentCulture
                = Thread.CurrentThread.CurrentUICulture
                = CultureInfo.DefaultThreadCurrentCulture
                = CultureInfo.DefaultThreadCurrentUICulture
                = CultureInfo.InvariantCulture;

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
                Exception cur = e;
                while (cur != null)
                {
                    Console.Error.WriteLine("{0}: {1}\n{2}", e.GetType().Name, e.Message, e.StackTrace);
                    cur = cur.InnerException;
                }
                return 1;
            }
        }
    }
}
