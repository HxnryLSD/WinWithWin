using System;
using System.IO;
using Newtonsoft.Json;

namespace WinWithWin.GUI.Services
{
    /// <summary>
    /// Service for managing application settings including theme preference
    /// </summary>
    public class SettingsService
    {
        private readonly string _settingsFilePath;
        private AppSettings _settings;

        public event EventHandler<AppSettings>? SettingsChanged;

        public SettingsService()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WinWithWin"
            );
            
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            _settingsFilePath = Path.Combine(appDataPath, "settings.json");
            _settings = LoadSettings();
        }

        /// <summary>
        /// Gets the current settings
        /// </summary>
        public AppSettings Settings => _settings;

        /// <summary>
        /// Gets or sets whether dark theme is enabled
        /// </summary>
        public bool IsDarkTheme
        {
            get => _settings.IsDarkTheme;
            set
            {
                if (_settings.IsDarkTheme != value)
                {
                    _settings.IsDarkTheme = value;
                    SaveSettings();
                    SettingsChanged?.Invoke(this, _settings);
                }
            }
        }

        /// <summary>
        /// Gets or sets the current locale
        /// </summary>
        public string Locale
        {
            get => _settings.Locale;
            set
            {
                if (_settings.Locale != value)
                {
                    _settings.Locale = value;
                    SaveSettings();
                    SettingsChanged?.Invoke(this, _settings);
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to minimize to tray on close
        /// </summary>
        public bool MinimizeToTray
        {
            get => _settings.MinimizeToTray;
            set
            {
                if (_settings.MinimizeToTray != value)
                {
                    _settings.MinimizeToTray = value;
                    SaveSettings();
                    SettingsChanged?.Invoke(this, _settings);
                }
            }
        }

        /// <summary>
        /// Gets or sets whether notifications are enabled
        /// </summary>
        public bool NotificationsEnabled
        {
            get => _settings.NotificationsEnabled;
            set
            {
                if (_settings.NotificationsEnabled != value)
                {
                    _settings.NotificationsEnabled = value;
                    SaveSettings();
                    SettingsChanged?.Invoke(this, _settings);
                }
            }
        }

        private AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch
            {
                // Ignore errors, return defaults
            }

            return new AppSettings();
        }

        private void SaveSettings()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_settings, Formatting.Indented);
                File.WriteAllText(_settingsFilePath, json);
            }
            catch
            {
                // Ignore save errors
            }
        }
    }

    public class AppSettings
    {
        public bool IsDarkTheme { get; set; } = true;
        public string Locale { get; set; } = "en";
        public bool MinimizeToTray { get; set; } = false;
        public bool NotificationsEnabled { get; set; } = true;
        public bool ShowAdvancedTweaks { get; set; } = false;
    }
}
