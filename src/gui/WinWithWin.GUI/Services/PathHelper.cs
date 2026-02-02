using System;
using System.Diagnostics;
using System.IO;

namespace WinWithWin.GUI.Services
{
    /// <summary>
    /// Helper class for resolving paths in single-file and normal deployments
    /// </summary>
    public static class PathHelper
    {
        private static string? _applicationDirectory;

        /// <summary>
        /// Gets the directory where the application executable is located.
        /// Works correctly for single-file deployments.
        /// </summary>
        public static string ApplicationDirectory
        {
            get
            {
                if (_applicationDirectory != null)
                    return _applicationDirectory;

                // For single-file apps, Environment.ProcessPath gives the actual exe location
                var exePath = Environment.ProcessPath;
                
                if (string.IsNullOrEmpty(exePath))
                {
                    // Fallback to process module
                    exePath = Process.GetCurrentProcess().MainModule?.FileName;
                }

                if (!string.IsNullOrEmpty(exePath))
                {
                    _applicationDirectory = Path.GetDirectoryName(exePath) ?? AppContext.BaseDirectory;
                }
                else
                {
                    _applicationDirectory = AppContext.BaseDirectory;
                }

                return _applicationDirectory;
            }
        }

        /// <summary>
        /// Gets the path to the Config directory
        /// </summary>
        public static string ConfigPath => Path.Combine(ApplicationDirectory, "Config");

        /// <summary>
        /// Gets the path to the Locales directory
        /// </summary>
        public static string LocalesPath => Path.Combine(ApplicationDirectory, "Locales");

        /// <summary>
        /// Gets the path to the Assets directory
        /// </summary>
        public static string AssetsPath => Path.Combine(ApplicationDirectory, "Assets");
    }
}
