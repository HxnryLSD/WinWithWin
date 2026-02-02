using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using WinWithWin.GUI.Models;

namespace WinWithWin.GUI.Services
{
    /// <summary>
    /// Service for exporting and importing tweak profiles
    /// </summary>
    public class ProfileService
    {
        private readonly string _profilesDirectory;

        public event EventHandler<string>? ProfileSaved;
        public event EventHandler<string>? ProfileLoaded;

        public ProfileService()
        {
            _profilesDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WinWithWin",
                "Profiles"
            );

            if (!Directory.Exists(_profilesDirectory))
            {
                Directory.CreateDirectory(_profilesDirectory);
            }
        }

        /// <summary>
        /// Gets the default profiles directory
        /// </summary>
        public string ProfilesDirectory => _profilesDirectory;

        /// <summary>
        /// Exports current tweak states to a profile file
        /// </summary>
        public bool ExportProfile(string profileName, IEnumerable<TweakViewModel> tweaks, string? description = null)
        {
            try
            {
                var profile = new TweakProfile
                {
                    Name = profileName,
                    Description = description ?? $"Profile created on {DateTime.Now:yyyy-MM-dd HH:mm}",
                    CreatedAt = DateTime.Now,
                    Version = "1.1.0",
                    Tweaks = tweaks
                        .Where(t => t.IsApplied)
                        .Select(t => new TweakProfileEntry
                        {
                            Id = t.Id,
                            Name = t.Name,
                            Category = t.Category
                        })
                        .ToList()
                };

                var fileName = SanitizeFileName(profileName) + ".json";
                var filePath = Path.Combine(_profilesDirectory, fileName);

                var json = JsonConvert.SerializeObject(profile, Formatting.Indented);
                File.WriteAllText(filePath, json);

                ProfileSaved?.Invoke(this, filePath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Exports profile to a custom file path
        /// </summary>
        public bool ExportProfileToFile(string filePath, IEnumerable<TweakViewModel> tweaks, string profileName, string? description = null)
        {
            try
            {
                var profile = new TweakProfile
                {
                    Name = profileName,
                    Description = description ?? $"Profile created on {DateTime.Now:yyyy-MM-dd HH:mm}",
                    CreatedAt = DateTime.Now,
                    Version = "1.1.0",
                    Tweaks = tweaks
                        .Where(t => t.IsApplied)
                        .Select(t => new TweakProfileEntry
                        {
                            Id = t.Id,
                            Name = t.Name,
                            Category = t.Category
                        })
                        .ToList()
                };

                var json = JsonConvert.SerializeObject(profile, Formatting.Indented);
                File.WriteAllText(filePath, json);

                ProfileSaved?.Invoke(this, filePath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Imports a profile from file and returns the tweak IDs to apply
        /// </summary>
        public TweakProfile? ImportProfile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return null;

                var json = File.ReadAllText(filePath);
                var profile = JsonConvert.DeserializeObject<TweakProfile>(json);

                if (profile != null)
                {
                    ProfileLoaded?.Invoke(this, filePath);
                }

                return profile;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets all saved profiles
        /// </summary>
        public List<TweakProfile> GetSavedProfiles()
        {
            var profiles = new List<TweakProfile>();

            try
            {
                var files = Directory.GetFiles(_profilesDirectory, "*.json");
                foreach (var file in files)
                {
                    var profile = ImportProfile(file);
                    if (profile != null)
                    {
                        profile.FilePath = file;
                        profiles.Add(profile);
                    }
                }
            }
            catch
            {
                // Ignore errors
            }

            return profiles.OrderByDescending(p => p.CreatedAt).ToList();
        }

        /// <summary>
        /// Deletes a saved profile
        /// </summary>
        public bool DeleteProfile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
            }
            catch
            {
                // Ignore
            }
            return false;
        }

        private static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
        }
    }

    public class TweakProfile
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Version { get; set; } = "1.0.0";
        public List<TweakProfileEntry> Tweaks { get; set; } = new();

        [JsonIgnore]
        public string? FilePath { get; set; }

        [JsonIgnore]
        public int TweakCount => Tweaks.Count;
    }

    public class TweakProfileEntry
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }
}
