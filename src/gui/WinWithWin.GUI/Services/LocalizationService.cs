using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WinWithWin.GUI.Services
{
    public class LocalizationService
    {
        private readonly string _localesPath;
        private string _currentLocale = "en";
        private JObject? _strings;

        public event EventHandler<string>? LocaleChanged;
        
        public string CurrentLocale => _currentLocale;

        public LocalizationService()
        {
            _localesPath = PathHelper.LocalesPath;
            LoadLocale(_currentLocale);
        }

        public void SetLocale(string locale)
        {
            if (_currentLocale == locale) return;
            
            _currentLocale = locale;
            LoadLocale(locale);
            LocaleChanged?.Invoke(this, locale);
        }

        public string GetString(string key, Dictionary<string, string>? variables = null)
        {
            if (_strings == null) return key;

            var keys = key.Split('.');
            JToken? current = _strings;

            foreach (var k in keys)
            {
                if (current is JObject obj && obj.TryGetValue(k, out var value))
                {
                    current = value;
                }
                else
                {
                    return key;
                }
            }

            var result = current?.ToString() ?? key;

            if (variables != null)
            {
                foreach (var variable in variables)
                {
                    result = result.Replace($"{{{variable.Key}}}", variable.Value);
                }
            }

            return result;
        }

        /// <summary>
        /// Gets a string with a fallback if localization fails
        /// </summary>
        public string GetStringOrFallback(string key, string fallback, Dictionary<string, string>? variables = null)
        {
            var result = GetString(key, variables);
            return result == key ? fallback : result;
        }

        private void LoadLocale(string locale)
        {
            try
            {
                var localeFile = Path.Combine(_localesPath, $"{locale}.json");

                if (!File.Exists(localeFile))
                {
                    localeFile = Path.Combine(_localesPath, "en.json");
                }

                if (File.Exists(localeFile))
                {
                    var json = File.ReadAllText(localeFile);
                    _strings = JsonConvert.DeserializeObject<JObject>(json);
                }
                else
                {
                    // Initialize with minimal default strings for critical UI elements
                    _strings = JObject.Parse(@"{
                        ""dialogs"": {
                            ""loadError"": {
                                ""title"": ""Error"",
                                ""message"": ""Failed to load tweaks: {message}""
                            },
                            ""initError"": {
                                ""title"": ""WinWithWin Startup Error"",
                                ""message"": ""Failed to initialize application:\n\n{message}""
                            }
                        },
                        ""status"": {
                            ""ready"": ""Ready"",
                            ""loading"": ""Loading tweaks..."",
                            ""loadError"": ""Error loading tweaks: {message}""
                        }
                    }");
                }
            }
            catch
            {
                // Fallback to minimal defaults
                _strings = JObject.Parse(@"{""status"":{""ready"":""Ready""}}");
            }
        }
    }
}
