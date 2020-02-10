using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ModInstallerIPC
{
    static class Program
    {
        static int Main(string[] args)
        {
            try
            {
                Console.WriteLine("started");

                Server server = new Server(int.Parse(args[0]), false);

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
