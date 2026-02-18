using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace CostPulse.Services
{
    public class LoggingService
    {
        private readonly string _logPath;

        public LoggingService()
        {
            const string logFileName = "debuglog.txt";
            // Log next to the EXE
            string exePath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            _logPath = Path.Combine(exePath, logFileName);
        }

        public void Log(string message)
        {
            try
            {
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}";
                File.AppendAllText(_logPath, logEntry);
            }
            catch
            {
                // Swallow logging errors to avoid crashing the app
            }
        }

        public void LogError(string message, Exception ex)
        {
            Log($"ERROR: {message} - {ex.Message}{Environment.NewLine}{ex.StackTrace}");
        }
    }
}
