using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace WinWithWin.GUI.Services
{
    /// <summary>
    /// Service to check the current state of tweaks by reading Windows Registry directly.
    /// This works independently of PowerShell and detects changes made by any tool.
    /// </summary>
    public class TweakStateChecker
    {
        // Define registry checks for each tweak
        // Format: TweakId -> List of (RegistryPath, ValueName, ExpectedValue when enabled)
        private readonly Dictionary<string, List<RegistryCheck>> _tweakChecks;

        public TweakStateChecker()
        {
            _tweakChecks = InitializeTweakChecks();
        }

        /// <summary>
        /// Checks if a specific tweak is currently applied on the system.
        /// </summary>
        public bool IsTweakApplied(string tweakId)
        {
            if (!_tweakChecks.TryGetValue(tweakId, out var checks) || checks.Count == 0)
            {
                // No registry checks defined - assume not applied
                return false;
            }

            // All checks must pass for the tweak to be considered "applied"
            foreach (var check in checks)
            {
                if (!CheckRegistryValue(check))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks all tweaks and returns a dictionary with their current state.
        /// </summary>
        public Dictionary<string, bool> CheckAllTweaks()
        {
            var results = new Dictionary<string, bool>();

            foreach (var tweakId in _tweakChecks.Keys)
            {
                results[tweakId] = IsTweakApplied(tweakId);
            }

            return results;
        }

        private bool CheckRegistryValue(RegistryCheck check)
        {
            try
            {
                using var key = GetRegistryKey(check.RootKey, check.SubKey, false);
                if (key == null)
                {
                    // Key doesn't exist - check if that's the expected state
                    return check.ExpectedWhenApplied == null;
                }

                var value = key.GetValue(check.ValueName);

                // If we expect the value to not exist
                if (check.ExpectedWhenApplied == null)
                {
                    return value == null;
                }

                // If value doesn't exist but we expect one
                if (value == null)
                {
                    return false;
                }

                // Compare values based on type
                return check.ExpectedWhenApplied switch
                {
                    int expectedInt => value is int intVal && intVal == expectedInt,
                    string expectedStr => value?.ToString() == expectedStr,
                    _ => value.Equals(check.ExpectedWhenApplied)
                };
            }
            catch
            {
                // On any error, assume tweak is not applied
                return false;
            }
        }

        private static RegistryKey? GetRegistryKey(RegistryHive hive, string subKey, bool writable)
        {
            var rootKey = hive switch
            {
                RegistryHive.LocalMachine => Registry.LocalMachine,
                RegistryHive.CurrentUser => Registry.CurrentUser,
                RegistryHive.ClassesRoot => Registry.ClassesRoot,
                RegistryHive.Users => Registry.Users,
                _ => Registry.LocalMachine
            };

            return rootKey.OpenSubKey(subKey, writable);
        }

        private Dictionary<string, List<RegistryCheck>> InitializeTweakChecks()
        {
            return new Dictionary<string, List<RegistryCheck>>
            {
                // Privacy Tweaks
                ["disable_telemetry"] = new List<RegistryCheck>
                {
                    new(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0)
                },

                ["disable_advertising_id"] = new List<RegistryCheck>
                {
                    new(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", 0)
                },

                ["disable_activity_history"] = new List<RegistryCheck>
                {
                    new(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\System", "EnableActivityFeed", 0),
                    new(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\System", "PublishUserActivities", 0)
                },

                ["disable_location_tracking"] = new List<RegistryCheck>
                {
                    new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location", "Value", "Deny")
                },

                ["disable_feedback"] = new List<RegistryCheck>
                {
                    new(RegistryHive.CurrentUser, @"Software\Microsoft\Siuf\Rules", "NumberOfSIUFInPeriod", 0)
                },

                ["disable_tailored_experiences"] = new List<RegistryCheck>
                {
                    new(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Privacy", "TailoredExperiencesWithDiagnosticDataEnabled", 0)
                },

                ["disable_typing_data"] = new List<RegistryCheck>
                {
                    new(RegistryHive.CurrentUser, @"Software\Microsoft\Input\TIPC", "Enabled", 0)
                },

                ["disable_app_launch_tracking"] = new List<RegistryCheck>
                {
                    new(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Start_TrackProgs", 0)
                },

                ["disable_windows_spotlight"] = new List<RegistryCheck>
                {
                    new(RegistryHive.CurrentUser, @"Software\Policies\Microsoft\Windows\CloudContent", "DisableWindowsSpotlightFeatures", 1)
                },

                ["disable_cortana"] = new List<RegistryCheck>
                {
                    new(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 0)
                },

                // Performance Tweaks
                ["optimize_visual_effects"] = new List<RegistryCheck>
                {
                    new(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting", 2)
                },

                ["disable_background_apps"] = new List<RegistryCheck>
                {
                    new(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", "GlobalUserDisabled", 1)
                },

                ["disable_game_dvr"] = new List<RegistryCheck>
                {
                    new(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 0)
                },

                ["disable_xbox_game_bar"] = new List<RegistryCheck>
                {
                    new(RegistryHive.CurrentUser, @"Software\Microsoft\GameBar", "UseNexusForGameBarEnabled", 0)
                },

                ["optimize_network_settings"] = new List<RegistryCheck>
                {
                    new(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\LanmanServer\Parameters", "IRPStackSize", 20)
                },

                ["disable_prefetch"] = new List<RegistryCheck>
                {
                    new(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters", "EnablePrefetcher", 0)
                },

                // Security Tweaks
                ["disable_remote_assistance"] = new List<RegistryCheck>
                {
                    new(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Remote Assistance", "fAllowToGetHelp", 0)
                },

                ["disable_remote_desktop"] = new List<RegistryCheck>
                {
                    new(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Terminal Server", "fDenyTSConnections", 1)
                },

                ["enable_uac_high"] = new List<RegistryCheck>
                {
                    new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "ConsentPromptBehaviorAdmin", 2)
                },

                ["disable_autorun"] = new List<RegistryCheck>
                {
                    new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoDriveTypeAutoRun", 255)
                },

                ["enable_windows_firewall"] = new List<RegistryCheck>
                {
                    new(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\StandardProfile", "EnableFirewall", 1)
                },

                // Debloat Tweaks
                ["uninstall_onedrive"] = new List<RegistryCheck>
                {
                    // OneDrive is removed if the setup key doesn't exist
                    new(RegistryHive.ClassesRoot, @"CLSID\{018D5C66-4533-4307-9B53-224DE2ED1FE6}", null, null)
                },

                ["disable_consumer_features"] = new List<RegistryCheck>
                {
                    new(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableWindowsConsumerFeatures", 1)
                },

                ["disable_suggested_apps"] = new List<RegistryCheck>
                {
                    new(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SilentInstalledAppsEnabled", 0),
                    new(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SystemPaneSuggestionsEnabled", 0)
                },

                ["disable_tips_and_tricks"] = new List<RegistryCheck>
                {
                    new(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SoftLandingEnabled", 0)
                },

                ["remove_preinstalled_apps"] = new List<RegistryCheck>
                {
                    // This is harder to check - use ContentDeliveryManager as proxy
                    new(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "PreInstalledAppsEnabled", 0)
                },

                ["disable_widgets"] = new List<RegistryCheck>
                {
                    new(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Dsh", "AllowNewsAndInterests", 0)
                },

                ["disable_web_search"] = new List<RegistryCheck>
                {
                    new(RegistryHive.CurrentUser, @"Software\Policies\Microsoft\Windows\Explorer", "DisableSearchBoxSuggestions", 1)
                },

                ["classic_context_menu"] = new List<RegistryCheck>
                {
                    new(RegistryHive.CurrentUser, @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32", "", "")
                },

                // Network Tweaks - These check registry/system state
                ["dns_cloudflare"] = new List<RegistryCheck>
                {
                    // DNS settings are checked via netsh, not registry - use empty check
                },

                ["dns_google"] = new List<RegistryCheck>
                {
                    // DNS settings are checked via netsh
                },

                ["dns_quad9"] = new List<RegistryCheck>
                {
                    // DNS settings are checked via netsh
                },

                ["dns_auto"] = new List<RegistryCheck>
                {
                    // DHCP mode - checked via netsh
                },

                ["optimize_tcpip"] = new List<RegistryCheck>
                {
                    new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", 0)
                },

                ["reset_network"] = new List<RegistryCheck>
                {
                    // Network reset is a one-time action, cannot check state
                },

                // Power Tweaks
                ["power_high_performance"] = new List<RegistryCheck>
                {
                    // Power plan is checked via powercfg command
                },

                ["power_balanced"] = new List<RegistryCheck>
                {
                    // Power plan is checked via powercfg command
                },

                ["power_ultimate"] = new List<RegistryCheck>
                {
                    // Power plan is checked via powercfg command
                },

                ["disable_hibernate"] = new List<RegistryCheck>
                {
                    new(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "HibernateEnabled", 0)
                },

                ["disable_sleep"] = new List<RegistryCheck>
                {
                    // Sleep timeout is checked via powercfg
                },

                ["disable_usb_suspend"] = new List<RegistryCheck>
                {
                    // USB suspend is checked via power settings
                },

                ["disable_wake_timers"] = new List<RegistryCheck>
                {
                    // Wake timers checked via power settings
                },

                // Storage Tweaks
                ["clean_temp_files"] = new List<RegistryCheck>
                {
                    // One-time action, no persistent state
                },

                ["clean_windows_update_cache"] = new List<RegistryCheck>
                {
                    // One-time action, no persistent state
                },

                ["disable_prefetch"] = new List<RegistryCheck>
                {
                    new(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters", "EnablePrefetcher", 0)
                },

                ["disable_superfetch"] = new List<RegistryCheck>
                {
                    new(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters", "EnableSuperfetch", 0)
                },

                ["run_ssd_trim"] = new List<RegistryCheck>
                {
                    // One-time action, no persistent state
                },

                ["clean_recycle_bin"] = new List<RegistryCheck>
                {
                    // One-time action, no persistent state
                },

                ["clean_thumbnails_cache"] = new List<RegistryCheck>
                {
                    // One-time action, no persistent state
                }
            };
        }
    }

    /// <summary>
    /// Represents a single registry value check.
    /// </summary>
    public class RegistryCheck
    {
        public RegistryHive RootKey { get; }
        public string SubKey { get; }
        public string? ValueName { get; }
        public object? ExpectedWhenApplied { get; }

        public RegistryCheck(RegistryHive rootKey, string subKey, string? valueName, object? expectedWhenApplied)
        {
            RootKey = rootKey;
            SubKey = subKey;
            ValueName = valueName;
            ExpectedWhenApplied = expectedWhenApplied;
        }
    }
}
