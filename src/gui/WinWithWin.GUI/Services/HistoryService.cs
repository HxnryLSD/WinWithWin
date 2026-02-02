using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace WinWithWin.GUI.Services
{
    /// <summary>
    /// Service for tracking tweak change history
    /// </summary>
    public class HistoryService
    {
        private readonly string _historyFilePath;
        private List<TweakHistoryEntry> _history;
        private const int MaxHistoryEntries = 500;

        public event EventHandler<TweakHistoryEntry>? EntryAdded;

        public HistoryService()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WinWithWin"
            );

            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            _historyFilePath = Path.Combine(appDataPath, "history.json");
            _history = LoadHistory();
        }

        /// <summary>
        /// Records a tweak change in history
        /// </summary>
        public void RecordChange(string tweakId, string tweakName, HistoryAction action, bool success, string? details = null)
        {
            var entry = new TweakHistoryEntry
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.Now,
                TweakId = tweakId,
                TweakName = tweakName,
                Action = action,
                Success = success,
                Details = details,
                UserName = Environment.UserName,
                MachineName = Environment.MachineName
            };

            _history.Insert(0, entry);

            // Trim old entries
            if (_history.Count > MaxHistoryEntries)
            {
                _history = _history.Take(MaxHistoryEntries).ToList();
            }

            SaveHistory();
            EntryAdded?.Invoke(this, entry);
        }

        /// <summary>
        /// Records applying a tweak
        /// </summary>
        public void RecordApply(string tweakId, string tweakName, bool success, string? details = null)
        {
            RecordChange(tweakId, tweakName, HistoryAction.Applied, success, details);
        }

        /// <summary>
        /// Records undoing a tweak
        /// </summary>
        public void RecordUndo(string tweakId, string tweakName, bool success, string? details = null)
        {
            RecordChange(tweakId, tweakName, HistoryAction.Undone, success, details);
        }

        /// <summary>
        /// Records applying a preset
        /// </summary>
        public void RecordPreset(string presetName, int tweakCount, bool success)
        {
            RecordChange("preset:" + presetName, presetName, HistoryAction.PresetApplied, success, $"{tweakCount} tweaks");
        }

        /// <summary>
        /// Gets all history entries
        /// </summary>
        public IReadOnlyList<TweakHistoryEntry> GetHistory()
        {
            return _history.AsReadOnly();
        }

        /// <summary>
        /// Gets history for a specific tweak
        /// </summary>
        public IReadOnlyList<TweakHistoryEntry> GetHistoryForTweak(string tweakId)
        {
            return _history.Where(h => h.TweakId == tweakId).ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets recent history entries
        /// </summary>
        public IReadOnlyList<TweakHistoryEntry> GetRecentHistory(int count = 20)
        {
            return _history.Take(count).ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets history entries for today
        /// </summary>
        public IReadOnlyList<TweakHistoryEntry> GetTodayHistory()
        {
            var today = DateTime.Today;
            return _history.Where(h => h.Timestamp.Date == today).ToList().AsReadOnly();
        }

        /// <summary>
        /// Clears all history
        /// </summary>
        public void ClearHistory()
        {
            _history.Clear();
            SaveHistory();
        }

        /// <summary>
        /// Exports history to a file
        /// </summary>
        public bool ExportHistory(string filePath)
        {
            try
            {
                var json = JsonConvert.SerializeObject(_history, Formatting.Indented);
                File.WriteAllText(filePath, json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private List<TweakHistoryEntry> LoadHistory()
        {
            try
            {
                if (File.Exists(_historyFilePath))
                {
                    var json = File.ReadAllText(_historyFilePath);
                    return JsonConvert.DeserializeObject<List<TweakHistoryEntry>>(json) ?? new List<TweakHistoryEntry>();
                }
            }
            catch
            {
                // Ignore errors
            }

            return new List<TweakHistoryEntry>();
        }

        private void SaveHistory()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_history, Formatting.Indented);
                File.WriteAllText(_historyFilePath, json);
            }
            catch
            {
                // Ignore errors
            }
        }
    }

    public class TweakHistoryEntry
    {
        public string Id { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string TweakId { get; set; } = string.Empty;
        public string TweakName { get; set; } = string.Empty;
        public HistoryAction Action { get; set; }
        public bool Success { get; set; }
        public string? Details { get; set; }
        public string? UserName { get; set; }
        public string? MachineName { get; set; }

        public string ActionText => Action switch
        {
            HistoryAction.Applied => "Applied",
            HistoryAction.Undone => "Undone",
            HistoryAction.PresetApplied => "Preset Applied",
            HistoryAction.Scheduled => "Scheduled",
            HistoryAction.BatchApplied => "Batch Applied",
            HistoryAction.BatchUndone => "Batch Undone",
            _ => "Unknown"
        };

        public string StatusIcon => Success ? "✅" : "❌";

        public string FormattedTimestamp => Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public enum HistoryAction
    {
        Applied,
        Undone,
        PresetApplied,
        Scheduled,
        BatchApplied,
        BatchUndone
    }
}
