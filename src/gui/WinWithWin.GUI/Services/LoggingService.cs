using System;
using System.IO;
using System.Text;

namespace WinWithWin.GUI.Services
{
    /// <summary>
    /// Logging service that writes tweak operations and errors to log files.
    /// </summary>
    public static class LoggingService
    {
        private static readonly object _lock = new object();
        private static string? _logDirectory;
        private static string? _currentLogFile;

        /// <summary>
        /// Gets the logs directory path.
        /// </summary>
        public static string LogDirectory
        {
            get
            {
                if (_logDirectory == null)
                {
                    _logDirectory = Path.Combine(PathHelper.ApplicationDirectory, "logs");
                    if (!Directory.Exists(_logDirectory))
                    {
                        Directory.CreateDirectory(_logDirectory);
                    }
                }
                return _logDirectory;
            }
        }

        /// <summary>
        /// Gets the current session log file path.
        /// </summary>
        public static string CurrentLogFile
        {
            get
            {
                if (_currentLogFile == null)
                {
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                    _currentLogFile = Path.Combine(LogDirectory, $"WinWithWin_{timestamp}.log");
                }
                return _currentLogFile;
            }
        }

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        public static void LogInfo(string message)
        {
            WriteLog("INFO", message);
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        public static void LogWarning(string message)
        {
            WriteLog("WARN", message);
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        public static void LogError(string message, Exception? ex = null)
        {
            var fullMessage = ex != null 
                ? $"{message}\nException: {ex.GetType().Name}: {ex.Message}\nStack Trace: {ex.StackTrace}"
                : message;
            WriteLog("ERROR", fullMessage);
        }

        /// <summary>
        /// Logs a tweak application result.
        /// </summary>
        public static void LogTweakResult(string tweakId, string tweakName, bool enable, bool success, string? errorMessage = null)
        {
            var action = enable ? "Enable" : "Disable";
            var status = success ? "SUCCESS" : "FAILED";
            
            var message = $"Tweak [{action}] {tweakName} (ID: {tweakId}) - {status}";
            if (!success && !string.IsNullOrEmpty(errorMessage))
            {
                message += $"\n  Error: {errorMessage}";
            }

            if (success)
            {
                WriteLog("TWEAK", message);
            }
            else
            {
                WriteLog("ERROR", message);
            }
        }

        /// <summary>
        /// Logs session start.
        /// </summary>
        public static void LogSessionStart()
        {
            var sb = new StringBuilder();
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine($"  WinWithWin Session Started");
            sb.AppendLine($"  Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"  OS: {Environment.OSVersion}");
            sb.AppendLine($"  User: {Environment.UserName}");
            sb.AppendLine($"  Machine: {Environment.MachineName}");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            
            WriteLogRaw(sb.ToString());
        }

        /// <summary>
        /// Logs session end.
        /// </summary>
        public static void LogSessionEnd()
        {
            var sb = new StringBuilder();
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine($"  WinWithWin Session Ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            
            WriteLogRaw(sb.ToString());
        }

        /// <summary>
        /// Cleans up old log files (keeps last 10).
        /// </summary>
        public static void CleanupOldLogs(int keepCount = 10)
        {
            try
            {
                var logFiles = Directory.GetFiles(LogDirectory, "WinWithWin_*.log");
                if (logFiles.Length <= keepCount) return;

                // Sort by creation time descending
                Array.Sort(logFiles, (a, b) => 
                    File.GetCreationTime(b).CompareTo(File.GetCreationTime(a)));

                // Delete older files
                for (int i = keepCount; i < logFiles.Length; i++)
                {
                    try
                    {
                        File.Delete(logFiles[i]);
                    }
                    catch
                    {
                        // Ignore deletion errors
                    }
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        private static void WriteLog(string level, string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logLine = $"[{timestamp}] [{level,-5}] {message}";
            WriteLogRaw(logLine);
        }

        private static void WriteLogRaw(string content)
        {
            lock (_lock)
            {
                try
                {
                    File.AppendAllText(CurrentLogFile, content + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to write log: {ex.Message}");
                }
            }
        }
    }
}
