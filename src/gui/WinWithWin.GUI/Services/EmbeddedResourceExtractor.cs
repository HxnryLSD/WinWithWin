using System;
using System.IO;
using System.Reflection;

namespace WinWithWin.GUI.Services
{
    /// <summary>
    /// Extracts embedded resources to the file system for single-file deployment.
    /// </summary>
    public static class EmbeddedResourceExtractor
    {
        private static bool _extracted = false;
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets the base path where resources are extracted.
        /// Uses the executable directory for portable operation.
        /// </summary>
        public static string BasePath => PathHelper.ApplicationDirectory;

        /// <summary>
        /// Extracts all embedded config and locale files if they don't exist.
        /// </summary>
        public static void ExtractIfNeeded()
        {
            if (_extracted) return;

            lock (_lock)
            {
                if (_extracted) return;

                try
                {
                    ExtractEmbeddedResources();
                    _extracted = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to extract resources: {ex.Message}");
                    // Continue anyway - external files might exist
                }
            }
        }

        private static void ExtractEmbeddedResources()
        {
            var assembly = Assembly.GetExecutingAssembly();
            
            // Define resource mappings: LogicalName -> relative file path
            var resourceMappings = new (string ResourceName, string FilePath)[]
            {
                ("config.tweaks.json", Path.Combine("config", "tweaks.json")),
                ("config.apps.json", Path.Combine("config", "apps.json")),
                ("config.presets.Balanced.json", Path.Combine("config", "presets", "Balanced.json")),
                ("config.presets.Gaming.json", Path.Combine("config", "presets", "Gaming.json")),
                ("config.presets.Minimal.json", Path.Combine("config", "presets", "Minimal.json")),
                ("config.presets.Privacy.json", Path.Combine("config", "presets", "Privacy.json")),
                ("locales.en.json", Path.Combine("locales", "en.json")),
                ("locales.de.json", Path.Combine("locales", "de.json")),
            };

            foreach (var (resourceName, filePath) in resourceMappings)
            {
                var fullPath = Path.Combine(BasePath, filePath);
                
                // Only extract if file doesn't exist (allows user customization)
                if (!File.Exists(fullPath))
                {
                    ExtractResource(assembly, resourceName, fullPath);
                }
            }
        }

        private static void ExtractResource(Assembly assembly, string resourceName, string targetPath)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                System.Diagnostics.Debug.WriteLine($"Resource not found: {resourceName}");
                return;
            }

            // Create directory if needed
            var directory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Write resource to file
            using var fileStream = File.Create(targetPath);
            stream.CopyTo(fileStream);
            
            System.Diagnostics.Debug.WriteLine($"Extracted: {resourceName} -> {targetPath}");
        }

        /// <summary>
        /// Gets embedded resource content as string without extracting to disk.
        /// Useful for reading config when disk write fails.
        /// </summary>
        public static string? GetEmbeddedResourceContent(string resourceName)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null) return null;

                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
            catch
            {
                return null;
            }
        }
    }
}
