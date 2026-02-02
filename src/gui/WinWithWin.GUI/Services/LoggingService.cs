using System;
using System.IO;
using System.Security.Principal;
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
        /// Logs session start with environment details.
        /// </summary>
        public static void LogSessionStart()
        {
            var isAdmin = IsRunningAsAdmin();
            var adminStatus = isAdmin ? "YES ✓" : "NO ✗ (some tweaks may fail)";
            
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine($"  WinWithWin Session Started");
            sb.AppendLine($"  Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("───────────────────────────────────────────────────────────────");
            sb.AppendLine($"  OS: {Environment.OSVersion}");
            sb.AppendLine($"  Windows Version: {GetWindowsVersion()}");
            sb.AppendLine($"  User: {Environment.UserName}");
            sb.AppendLine($"  Machine: {Environment.MachineName}");
            sb.AppendLine($"  Running as Administrator: {adminStatus}");
            sb.AppendLine($"  64-bit OS: {Environment.Is64BitOperatingSystem}");
            sb.AppendLine($"  64-bit Process: {Environment.Is64BitProcess}");
            sb.AppendLine($"  .NET Version: {Environment.Version}");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();
            
            WriteLogRaw(sb.ToString());
            
            if (!isAdmin)
            {
                LogWarning("Application is NOT running as Administrator. Many tweaks require admin privileges and will fail.");
                LogWarning("Right-click the application and select 'Run as administrator' for full functionality.");
            }
        }
        
        /// <summary>
        /// Checks if the current process is running with administrator privileges.
        /// </summary>
        public static bool IsRunningAsAdmin()
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Gets the Windows version string.
        /// </summary>
        private static string GetWindowsVersion()
        {
            try
            {
                var version = Environment.OSVersion.Version;
                return version.Build >= 22000 ? $"Windows 11 (Build {version.Build})" :
                       version.Build >= 10240 ? $"Windows 10 (Build {version.Build})" :
                       $"Windows {version.Major}.{version.Minor} (Build {version.Build})";
            }
            catch
            {
                return "Unknown";
            }
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
