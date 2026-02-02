using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace WinWithWin.GUI.Services
{
    /// <summary>
    /// Service for scheduling tweaks to run at specific times
    /// </summary>
    public class SchedulerService
    {
        private readonly string _schedulesFilePath;
        private readonly string _schedulerScriptPath;
        private List<ScheduledTweak> _schedules;

        public event EventHandler<ScheduledTweak>? ScheduleAdded;
        public event EventHandler<ScheduledTweak>? ScheduleRemoved;

        public SchedulerService()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WinWithWin"
            );

            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            _schedulesFilePath = Path.Combine(appDataPath, "schedules.json");
            _schedulerScriptPath = Path.Combine(appDataPath, "ScheduledTweak.ps1");
            _schedules = LoadSchedules();

            EnsureSchedulerScriptExists();
        }

        /// <summary>
        /// Creates a scheduled task for a tweak
        /// </summary>
        public bool ScheduleTweak(string tweakId, string tweakName, ScheduleType type, DateTime scheduledTime, 
            bool applyTweak = true, bool recurring = false, RecurrencePattern? pattern = null)
        {
            try
            {
                var schedule = new ScheduledTweak
                {
                    Id = Guid.NewGuid().ToString(),
                    TweakId = tweakId,
                    TweakName = tweakName,
                    ScheduleType = type,
                    ScheduledTime = scheduledTime,
                    ApplyTweak = applyTweak,
                    IsRecurring = recurring,
                    Recurrence = pattern,
                    CreatedAt = DateTime.Now,
                    IsEnabled = true
                };

                // Create Windows Task Scheduler task
                var taskName = $"WinWithWin_{schedule.Id}";
                var success = CreateWindowsTask(taskName, schedule);

                if (success)
                {
                    schedule.TaskName = taskName;
                    _schedules.Add(schedule);
                    SaveSchedules();
                    ScheduleAdded?.Invoke(this, schedule);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Removes a scheduled tweak
        /// </summary>
        public bool RemoveSchedule(string scheduleId)
        {
            try
            {
                var schedule = _schedules.FirstOrDefault(s => s.Id == scheduleId);
                if (schedule == null) return false;

                // Remove Windows Task
                if (!string.IsNullOrEmpty(schedule.TaskName))
                {
                    RemoveWindowsTask(schedule.TaskName);
                }

                _schedules.Remove(schedule);
                SaveSchedules();
                ScheduleRemoved?.Invoke(this, schedule);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Enables or disables a schedule
        /// </summary>
        public bool SetScheduleEnabled(string scheduleId, bool enabled)
        {
            var schedule = _schedules.FirstOrDefault(s => s.Id == scheduleId);
            if (schedule == null) return false;

            schedule.IsEnabled = enabled;
            
            if (!string.IsNullOrEmpty(schedule.TaskName))
            {
                SetWindowsTaskEnabled(schedule.TaskName, enabled);
            }

            SaveSchedules();
            return true;
        }

        /// <summary>
        /// Gets all schedules
        /// </summary>
        public IReadOnlyList<ScheduledTweak> GetSchedules()
        {
            return _schedules.AsReadOnly();
        }

        /// <summary>
        /// Gets schedules for a specific tweak
        /// </summary>
        public IReadOnlyList<ScheduledTweak> GetSchedulesForTweak(string tweakId)
        {
            return _schedules.Where(s => s.TweakId == tweakId).ToList().AsReadOnly();
        }

        private bool CreateWindowsTask(string taskName, ScheduledTweak schedule)
        {
            try
            {
                var action = schedule.ApplyTweak ? "apply" : "undo";
                var triggerTime = schedule.ScheduledTime.ToString("HH:mm");
                var triggerDate = schedule.ScheduledTime.ToString("yyyy-MM-dd");

                var frequency = schedule.IsRecurring && schedule.Recurrence != null
                    ? GetFrequencyParameter(schedule.Recurrence.Value)
                    : "ONCE";

                var script = $@"
$ErrorActionPreference = 'SilentlyContinue'
$taskAction = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument '-NoProfile -ExecutionPolicy Bypass -File ""{_schedulerScriptPath}"" -TweakId ""{schedule.TweakId}"" -Action ""{action}""'
$taskTrigger = New-ScheduledTaskTrigger -{frequency} -At '{triggerTime}'
$taskSettings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable
$taskPrincipal = New-ScheduledTaskPrincipal -UserId 'SYSTEM' -LogonType ServiceAccount -RunLevel Highest

Register-ScheduledTask -TaskName '{taskName}' -Action $taskAction -Trigger $taskTrigger -Settings $taskSettings -Principal $taskPrincipal -Force
";

                return ExecutePowerShell(script);
            }
            catch
            {
                return false;
            }
        }

        private bool RemoveWindowsTask(string taskName)
        {
            try
            {
                var script = $"Unregister-ScheduledTask -TaskName '{taskName}' -Confirm:$false -ErrorAction SilentlyContinue";
                return ExecutePowerShell(script);
            }
            catch
            {
                return false;
            }
        }

        private bool SetWindowsTaskEnabled(string taskName, bool enabled)
        {
            try
            {
                var action = enabled ? "Enable" : "Disable";
                var script = $"{action}-ScheduledTask -TaskName '{taskName}' -ErrorAction SilentlyContinue";
                return ExecutePowerShell(script);
            }
            catch
            {
                return false;
            }
        }

        private string GetFrequencyParameter(RecurrencePattern pattern)
        {
            return pattern switch
            {
                RecurrencePattern.Daily => "Daily",
                RecurrencePattern.Weekly => "Weekly",
                RecurrencePattern.Monthly => "Monthly",
                RecurrencePattern.AtStartup => "AtStartup",
                RecurrencePattern.AtLogon => "AtLogon",
                _ => "Once"
            };
        }

        private void EnsureSchedulerScriptExists()
        {
            if (!File.Exists(_schedulerScriptPath))
            {
                var script = @"
param(
    [Parameter(Mandatory=$true)]
    [string]$TweakId,
    
    [Parameter(Mandatory=$true)]
    [ValidateSet('apply', 'undo')]
    [string]$Action
)

# WinWithWin Scheduled Tweak Runner
$ErrorActionPreference = 'Stop'

try {
    $modulePath = Join-Path $PSScriptRoot '..\..\WinWithWin\src\core\WinWithWin.psm1'
    
    if (Test-Path $modulePath) {
        Import-Module $modulePath -Force
        
        if ($Action -eq 'apply') {
            Invoke-Tweak -TweakId $TweakId -Apply
        } else {
            Invoke-Tweak -TweakId $TweakId -Undo
        }
        
        # Log success
        $logPath = Join-Path $env:APPDATA 'WinWithWin\scheduler.log'
        Add-Content -Path $logPath -Value ""[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] SUCCESS: $Action tweak $TweakId""
    }
} catch {
    $logPath = Join-Path $env:APPDATA 'WinWithWin\scheduler.log'
    Add-Content -Path $logPath -Value ""[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] ERROR: $Action tweak $TweakId - $($_.Exception.Message)""
}
";
                File.WriteAllText(_schedulerScriptPath, script);
            }
        }

        private bool ExecutePowerShell(string script)
        {
            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script.Replace("\"", "\\\"")}\"",
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };

                using var process = System.Diagnostics.Process.Start(startInfo);
                process?.WaitForExit(10000);
                return process?.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private List<ScheduledTweak> LoadSchedules()
        {
            try
            {
                if (File.Exists(_schedulesFilePath))
                {
                    var json = File.ReadAllText(_schedulesFilePath);
                    return JsonConvert.DeserializeObject<List<ScheduledTweak>>(json) ?? new List<ScheduledTweak>();
                }
            }
            catch
            {
                // Ignore
            }
            return new List<ScheduledTweak>();
        }

        private void SaveSchedules()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_schedules, Formatting.Indented);
                File.WriteAllText(_schedulesFilePath, json);
            }
            catch
            {
                // Ignore
            }
        }
    }

    public class ScheduledTweak
    {
        public string Id { get; set; } = string.Empty;
        public string TweakId { get; set; } = string.Empty;
        public string TweakName { get; set; } = string.Empty;
        public ScheduleType ScheduleType { get; set; }
        public DateTime ScheduledTime { get; set; }
        public bool ApplyTweak { get; set; } = true;
        public bool IsRecurring { get; set; }
        public RecurrencePattern? Recurrence { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsEnabled { get; set; } = true;
        public string? TaskName { get; set; }
        public DateTime? LastRun { get; set; }

        public string ActionText => ApplyTweak ? "Apply" : "Undo";
        public string StatusIcon => IsEnabled ? "▶️" : "⏸️";
        public string RecurrenceText => IsRecurring && Recurrence.HasValue 
            ? Recurrence.Value.ToString() 
            : "One-time";
    }

    public enum ScheduleType
    {
        OneTime,
        Recurring,
        AtStartup,
        AtLogon
    }

    public enum RecurrencePattern
    {
        Daily,
        Weekly,
        Monthly,
        AtStartup,
        AtLogon
    }
}
