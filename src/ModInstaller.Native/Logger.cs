using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace ModInstaller.Native;

public static partial class Logger
{
    private static readonly string _mutexName = @"Global\FOMODLoggerMutex";
    private static readonly string _logFilePath = @$"{Environment.GetEnvironmentVariable("APPDATA")}\vortex_devel\FOMOD.ModInstaller.log";

    private static void Log(string message)
    {
        using var mutex = new Mutex(false, _mutexName);
        while (true)
        {
            try
            {
                if (!mutex.WaitOne(100)) continue;

                try
                {
                    using var fs = new FileStream(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read, 4096, FileOptions.SequentialScan);
                    using var sw = new StreamWriter(fs, Encoding.UTF8);
                    sw.Write("[C# ][");
                    sw.Write(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
                    sw.Write("] ");
                    sw.WriteLine(message);
                    return;
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
            catch (Exception) { /* ignored */ }
        }
    }
}