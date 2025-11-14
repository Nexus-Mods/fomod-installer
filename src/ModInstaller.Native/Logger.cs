using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace ModInstaller.Native;

public static partial class Logger
{
    private static readonly string _mutexName = @"Global\FOMODLoggerMutex";
#if DEBUG
    private static readonly string _logFilePath = @$"{Environment.GetEnvironmentVariable("APPDATA")}\vortex_devel\FOMOD.ModInstaller.log";
#else
    private static readonly string _logFilePath = @$"{Environment.GetEnvironmentVariable("APPDATA")}\vortex\FOMOD.ModInstaller.log";
#endif
    private static DateTime _lastDirectoryCheck = DateTime.MinValue;
    private static readonly TimeSpan _directoryCheckInterval = TimeSpan.FromSeconds(5);

    private static void Log(string message)
    {
        using var mutex = new Mutex(false, _mutexName);

        var timeout = DateTime.UtcNow.AddSeconds(1);
        while (DateTime.UtcNow < timeout)
        {
            // Ensure directory exists (cached check every 5 seconds)
            if (DateTime.UtcNow - _lastDirectoryCheck > _directoryCheckInterval)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(_logFilePath));
                    _lastDirectoryCheck = DateTime.UtcNow;
                }
                catch { /* ignore */ }
            }
            
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
            catch { Thread.Sleep(50); }
        }

        // Timeout reached - log could not be written
        // Fail silently to prevent hanging the application
    }
}