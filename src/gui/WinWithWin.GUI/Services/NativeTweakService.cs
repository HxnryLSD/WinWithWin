using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using Microsoft.Win32;

namespace WinWithWin.GUI.Services
{
    /// <summary>
    /// Native C# implementation of tweaks using Registry modifications.
    /// This allows tweaks to work without PowerShell modules.
    /// </summary>
    public class NativeTweakService
    {
        /// <summary>
        /// Applies a tweak by its ID using native C# Registry operations.
        /// </summary>
        public bool ApplyTweak(string tweakId)
        {
            try
            {
                return tweakId switch
                {
                    "disable_telemetry" => DisableTelemetry(),
                    "disable_advertising_id" => DisableAdvertisingId(),
                    "disable_activity_history" => DisableActivityHistory(),
                    "disable_location_tracking" => DisableLocationTracking(),
                    "disable_feedback" => DisableFeedback(),
                    "disable_tailored_experiences" => DisableTailoredExperiences(),
                    "disable_typing_data" => DisableTypingData(),
                    "disable_app_launch_tracking" => DisableAppLaunchTracking(),
                    "disable_windows_spotlight" => DisableWindowsSpotlight(),
                    "disable_cortana" => DisableCortana(),
                    "optimize_visual_effects" => OptimizeVisualEffects(),
                    "disable_background_apps" => DisableBackgroundApps(),
                    "disable_game_dvr" => DisableGameDvr(),
                    "disable_xbox_game_bar" => DisableXboxGameBar(),
                    "disable_consumer_features" => DisableConsumerFeatures(),
                    "disable_suggested_apps" => DisableSuggestedApps(),
                    "disable_tips_and_tricks" => DisableTipsAndTricks(),
                    "disable_web_search" => DisableWebSearch(),
                    "classic_context_menu" => EnableClassicContextMenu(),
                    // Network Tweaks
                    "dns_cloudflare" => SetDnsCloudflare(),
                    "dns_google" => SetDnsGoogle(),
                    "dns_quad9" => SetDnsQuad9(),
                    "dns_auto" => SetDnsAuto(),
                    "optimize_tcpip" => OptimizeTcpIp(),
                    "reset_network" => ResetNetwork(),
                    // Power Tweaks
                    "power_high_performance" => SetPowerHighPerformance(),
                    "power_balanced" => SetPowerBalanced(),
                    "power_ultimate" => SetPowerUltimate(),
                    "disable_hibernate" => DisableHibernate(),
                    "disable_sleep" => DisableSleep(),
                    "disable_usb_suspend" => DisableUsbSuspend(),
                    "disable_wake_timers" => DisableWakeTimers(),
                    // Storage Tweaks
                    "clean_temp_files" => CleanTempFiles(),
                    "clean_windows_update_cache" => CleanWindowsUpdateCache(),
                    "disable_prefetch" => DisablePrefetch(),
                    "disable_superfetch" => DisableSuperfetch(),
                    "run_ssd_trim" => RunSsdTrim(),
                    "clean_recycle_bin" => CleanRecycleBin(),
                    "clean_thumbnails_cache" => CleanThumbnailsCache(),
                    _ => false
                };
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Undoes a tweak by its ID using native C# Registry operations.
        /// </summary>
        public bool UndoTweak(string tweakId)
        {
            try
            {
                return tweakId switch
                {
                    "disable_telemetry" => EnableTelemetry(),
                    "disable_advertising_id" => EnableAdvertisingId(),
                    "disable_activity_history" => EnableActivityHistory(),
                    "disable_location_tracking" => EnableLocationTracking(),
                    "disable_feedback" => EnableFeedback(),
                    "disable_tailored_experiences" => EnableTailoredExperiences(),
                    "disable_typing_data" => EnableTypingData(),
                    "disable_app_launch_tracking" => EnableAppLaunchTracking(),
                    "disable_windows_spotlight" => EnableWindowsSpotlight(),
                    "disable_cortana" => EnableCortana(),
                    "optimize_visual_effects" => RestoreVisualEffects(),
                    "disable_background_apps" => EnableBackgroundApps(),
                    "disable_game_dvr" => EnableGameDvr(),
                    "disable_xbox_game_bar" => EnableXboxGameBar(),
                    "disable_consumer_features" => EnableConsumerFeatures(),
                    "disable_suggested_apps" => EnableSuggestedApps(),
                    "disable_tips_and_tricks" => EnableTipsAndTricks(),
                    "disable_web_search" => EnableWebSearch(),
                    "classic_context_menu" => DisableClassicContextMenu(),
                    // Network Tweaks - Undo sets to automatic
                    "dns_cloudflare" => SetDnsAuto(),
                    "dns_google" => SetDnsAuto(),
                    "dns_quad9" => SetDnsAuto(),
                    "dns_auto" => true, // Already auto, nothing to undo
                    "optimize_tcpip" => UndoOptimizeTcpIp(),
                    "reset_network" => true, // Cannot undo a reset
                    // Power Tweaks
                    "power_high_performance" => SetPowerBalanced(),
                    "power_balanced" => true, // Already balanced
                    "power_ultimate" => SetPowerBalanced(),
                    "disable_hibernate" => EnableHibernate(),
                    "disable_sleep" => EnableSleep(),
                    "disable_usb_suspend" => EnableUsbSuspend(),
                    "disable_wake_timers" => EnableWakeTimers(),
                    // Storage Tweaks - most are one-time actions, cannot undo
                    "clean_temp_files" => true,
                    "clean_windows_update_cache" => true,
                    "disable_prefetch" => EnablePrefetch(),
                    "disable_superfetch" => EnableSuperfetch(),
                    "run_ssd_trim" => true,
                    "clean_recycle_bin" => true,
                    "clean_thumbnails_cache" => true,
                    _ => false
                };
            }
            catch (Exception)
            {
                return false;
            }
        }

        #region Privacy Tweaks

        private bool DisableTelemetry()
        {
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0);
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowDeviceNameInTelemetry", 0);
            return true;
        }

        private bool EnableTelemetry()
        {
            DeleteRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry");
            DeleteRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowDeviceNameInTelemetry");
            return true;
        }

        private bool DisableAdvertisingId()
        {
            SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", 0);
            return true;
        }

        private bool EnableAdvertisingId()
        {
            SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", 1);
            return true;
        }

        private bool DisableActivityHistory()
        {
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\System", "EnableActivityFeed", 0);
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\System", "PublishUserActivities", 0);
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\System", "UploadUserActivities", 0);
            return true;
        }

        private bool EnableActivityHistory()
        {
            DeleteRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\System", "EnableActivityFeed");
            DeleteRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\System", "PublishUserActivities");
            DeleteRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\System", "UploadUserActivities");
            return true;
        }

        private bool DisableLocationTracking()
        {
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location", "Value", "Deny");
            return true;
        }

        private bool EnableLocationTracking()
        {
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location", "Value", "Allow");
            return true;
        }

        private bool DisableFeedback()
        {
            SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Siuf\Rules", "NumberOfSIUFInPeriod", 0);
            SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Siuf\Rules", "PeriodInNanoSeconds", 0);
            return true;
        }

        private bool EnableFeedback()
        {
            DeleteRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Siuf\Rules", "NumberOfSIUFInPeriod");
            DeleteRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Siuf\Rules", "PeriodInNanoSeconds");
            return true;
        }

        private bool DisableTailoredExperiences()
        {
            SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Privacy", "TailoredExperiencesWithDiagnosticDataEnabled", 0);
            return true;
        }

        private bool EnableTailoredExperiences()
        {
            SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Privacy", "TailoredExperiencesWithDiagnosticDataEnabled", 1);
            return true;
        }

        private bool DisableTypingData()
        {
            SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Input\TIPC", "Enabled", 0);
            return true;
        }

        private bool EnableTypingData()
        {
            SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Input\TIPC", "Enabled", 1);
            return true;
        }

        private bool DisableAppLaunchTracking()
        {
            SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Start_TrackProgs", 0);
            return true;
        }

        private bool EnableAppLaunchTracking()
        {
            SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "Start_TrackProgs", 1);
            return true;
        }

        private bool DisableWindowsSpotlight()
        {
            SetRegistryValue(Registry.CurrentUser, @"Software\Policies\Microsoft\Windows\CloudContent", "DisableWindowsSpotlightFeatures", 1);
            SetRegistryValue(Registry.CurrentUser, @"Software\Policies\Microsoft\Windows\CloudContent", "DisableWindowsSpotlightOnActionCenter", 1);
            SetRegistryValue(Registry.CurrentUser, @"Software\Policies\Microsoft\Windows\CloudContent", "DisableWindowsSpotlightWindowsWelcomeExperience", 1);
            return true;
        }

        private bool EnableWindowsSpotlight()
        {
            DeleteRegistryValue(Registry.CurrentUser, @"Software\Policies\Microsoft\Windows\CloudContent", "DisableWindowsSpotlightFeatures");
            DeleteRegistryValue(Registry.CurrentUser, @"Software\Policies\Microsoft\Windows\CloudContent", "DisableWindowsSpotlightOnActionCenter");
            DeleteRegistryValue(Registry.CurrentUser, @"Software\Policies\Microsoft\Windows\CloudContent", "DisableWindowsSpotlightWindowsWelcomeExperience");
            return true;
        }

        private bool DisableCortana()
        {
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 0);
            SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Search", "CortanaConsent", 0);
            return true;
        }

        private bool EnableCortana()
        {
            DeleteRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana");
            DeleteRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Search", "CortanaConsent");
            return true;
        }

        #endregion

        #region Performance Tweaks

        private bool OptimizeVisualEffects()
        {
            SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting", 2);
            SetRegistryValue(Registry.CurrentUser, @"Control Panel\Desktop", "UserPreferencesMask", new byte[] { 0x90, 0x12, 0x03, 0x80, 0x10, 0x00, 0x00, 0x00 });
            return true;
        }

        private bool RestoreVisualEffects()
        {
            SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting", 0);
            return true;
        }

        private bool DisableBackgroundApps()
        {
            SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", "GlobalUserDisabled", 1);
            SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Search", "BackgroundAppGlobalToggle", 0);
            return true;
        }

        private bool EnableBackgroundApps()
        {
            SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", "GlobalUserDisabled", 0);
            SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Search", "BackgroundAppGlobalToggle", 1);
            return true;
        }

        private bool DisableGameDvr()
        {
            SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 0);
            SetRegistryValue(Registry.CurrentUser, @"System\GameConfigStore", "GameDVR_Enabled", 0);
            return true;
        }

        private bool EnableGameDvr()
        {
            SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 1);
            SetRegistryValue(Registry.CurrentUser, @"System\GameConfigStore", "GameDVR_Enabled", 1);
            return true;
        }

        private bool DisableXboxGameBar()
        {
            SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\GameBar", "UseNexusForGameBarEnabled", 0);
            SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\GameBar", "ShowStartupPanel", 0);
            return true;
        }

        private bool EnableXboxGameBar()
        {
            SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\GameBar", "UseNexusForGameBarEnabled", 1);
            DeleteRegistryValue(Registry.CurrentUser, @"Software\Microsoft\GameBar", "ShowStartupPanel");
            return true;
        }

        #endregion

        #region Debloat Tweaks

        private bool DisableConsumerFeatures()
        {
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableWindowsConsumerFeatures", 1);
            return true;
        }

        private bool EnableConsumerFeatures()
        {
            DeleteRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableWindowsConsumerFeatures");
            return true;
        }

        private bool DisableSuggestedApps()
        {
            SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SilentInstalledAppsEnabled", 0);
            SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SystemPaneSuggestionsEnabled", 0);
            SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338388Enabled", 0);
            SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-338389Enabled", 0);
            return true;
        }

        private bool EnableSuggestedApps()
        {
            SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SilentInstalledAppsEnabled", 1);
            SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SystemPaneSuggestionsEnabled", 1);
            return true;
        }

        private bool DisableTipsAndTricks()
        {
            SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SoftLandingEnabled", 0);
            SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SubscribedContent-310093Enabled", 0);
            return true;
        }

        private bool EnableTipsAndTricks()
        {
            SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "SoftLandingEnabled", 1);
            return true;
        }

        private bool DisableWebSearch()
        {
            SetRegistryValue(Registry.CurrentUser, @"Software\Policies\Microsoft\Windows\Explorer", "DisableSearchBoxSuggestions", 1);
            SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled", 0);
            return true;
        }

        private bool EnableWebSearch()
        {
            DeleteRegistryValue(Registry.CurrentUser, @"Software\Policies\Microsoft\Windows\Explorer", "DisableSearchBoxSuggestions");
            SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled", 1);
            return true;
        }

        private bool EnableClassicContextMenu()
        {
            // Create the key that disables the Windows 11 context menu
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32");
            key?.SetValue("", "");
            return true;
        }

        private bool DisableClassicContextMenu()
        {
            try
            {
                Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}", false);
                return true;
            }
            catch
            {
                return true; // Key doesn't exist, which is fine
            }
        }

        #endregion

        #region Network Tweaks

        private string? GetActiveNetworkAdapterName()
        {
            try
            {
                var activeAdapter = NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault(n => n.OperationalStatus == OperationalStatus.Up 
                        && n.NetworkInterfaceType != NetworkInterfaceType.Loopback
                        && n.NetworkInterfaceType != NetworkInterfaceType.Tunnel);
                return activeAdapter?.Name;
            }
            catch
            {
                return null;
            }
        }

        private bool RunNetshCommand(string arguments)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using var process = Process.Start(psi);
                process?.WaitForExit(10000);
                return process?.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private bool RunPowerCfgCommand(string arguments)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powercfg",
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using var process = Process.Start(psi);
                process?.WaitForExit(10000);
                return process?.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private bool SetDnsServers(string primary, string secondary)
        {
            var adapterName = GetActiveNetworkAdapterName();
            if (string.IsNullOrEmpty(adapterName)) return false;

            var result1 = RunNetshCommand($"interface ip set dns name=\"{adapterName}\" static {primary}");
            var result2 = RunNetshCommand($"interface ip add dns name=\"{adapterName}\" {secondary} index=2");
            
            // Also set IPv6 DNS if applicable
            RunNetshCommand($"interface ipv6 set dns name=\"{adapterName}\" static {primary.Replace(".", "::")}");
            
            return result1;
        }

        private bool SetDnsCloudflare()
        {
            return SetDnsServers("1.1.1.1", "1.0.0.1");
        }

        private bool SetDnsGoogle()
        {
            return SetDnsServers("8.8.8.8", "8.8.4.4");
        }

        private bool SetDnsQuad9()
        {
            return SetDnsServers("9.9.9.9", "149.112.112.112");
        }

        private bool SetDnsAuto()
        {
            var adapterName = GetActiveNetworkAdapterName();
            if (string.IsNullOrEmpty(adapterName)) return false;

            RunNetshCommand($"interface ip set dns name=\"{adapterName}\" dhcp");
            RunNetshCommand($"interface ipv6 set dns name=\"{adapterName}\" dhcp");
            return true;
        }

        private bool OptimizeTcpIp()
        {
            // Enable TCP Auto-Tuning
            RunNetshCommand("int tcp set global autotuninglevel=normal");
            
            // Disable Nagle's Algorithm for lower latency
            SetRegistryValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces", "TcpAckFrequency", 1);
            SetRegistryValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces", "TCPNoDelay", 1);
            
            // Optimize network throttling
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", unchecked((int)0xFFFFFFFF));
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", 0);
            
            // Disable Large Send Offload
            RunNetshCommand("int tcp set global chimney=disabled");
            RunNetshCommand("int tcp set global rss=enabled");
            
            return true;
        }

        private bool UndoOptimizeTcpIp()
        {
            RunNetshCommand("int tcp set global autotuninglevel=normal");
            DeleteRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex");
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", 20);
            return true;
        }

        private bool ResetNetwork()
        {
            // Flush DNS cache
            RunNetshCommand("interface ip delete arpcache");
            
            var psi = new ProcessStartInfo
            {
                FileName = "ipconfig",
                Arguments = "/flushdns",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process.Start(psi)?.WaitForExit(5000);

            // Reset Winsock
            RunNetshCommand("winsock reset");
            
            // Reset TCP/IP stack
            RunNetshCommand("int ip reset");
            RunNetshCommand("int ipv6 reset");
            
            // Release and renew IP
            psi.Arguments = "/release";
            Process.Start(psi)?.WaitForExit(5000);
            
            psi.Arguments = "/renew";
            Process.Start(psi)?.WaitForExit(10000);
            
            return true;
        }

        #endregion

        #region Power Tweaks

        private bool SetPowerHighPerformance()
        {
            // GUID for High Performance power plan
            return RunPowerCfgCommand("/setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
        }

        private bool SetPowerBalanced()
        {
            // GUID for Balanced power plan
            return RunPowerCfgCommand("/setactive 381b4222-f694-41f0-9685-ff5bb260df2e");
        }

        private bool SetPowerUltimate()
        {
            // First, duplicate and create Ultimate Performance if it doesn't exist
            RunPowerCfgCommand("/duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61");
            // Then activate it
            return RunPowerCfgCommand("/setactive e9a42b02-d5df-448d-aa00-03f14749eb61");
        }

        private bool DisableHibernate()
        {
            return RunPowerCfgCommand("/hibernate off");
        }

        private bool EnableHibernate()
        {
            return RunPowerCfgCommand("/hibernate on");
        }

        private bool DisableSleep()
        {
            // Set sleep timeout to 0 (never) on AC power
            RunPowerCfgCommand("/change standby-timeout-ac 0");
            RunPowerCfgCommand("/change monitor-timeout-ac 0");
            return true;
        }

        private bool EnableSleep()
        {
            // Restore default sleep timeout (30 minutes)
            RunPowerCfgCommand("/change standby-timeout-ac 30");
            RunPowerCfgCommand("/change monitor-timeout-ac 15");
            return true;
        }

        private bool DisableUsbSuspend()
        {
            // Disable USB selective suspend in current power plan
            // Setting index 2 = Disabled
            RunPowerCfgCommand("/setacvalueindex scheme_current 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4782-a5d4-53bb8f07e226 0");
            RunPowerCfgCommand("/setdcvalueindex scheme_current 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4782-a5d4-53bb8f07e226 0");
            RunPowerCfgCommand("/setactive scheme_current");
            return true;
        }

        private bool EnableUsbSuspend()
        {
            // Enable USB selective suspend
            RunPowerCfgCommand("/setacvalueindex scheme_current 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4782-a5d4-53bb8f07e226 1");
            RunPowerCfgCommand("/setdcvalueindex scheme_current 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4782-a5d4-53bb8f07e226 1");
            RunPowerCfgCommand("/setactive scheme_current");
            return true;
        }

        private bool DisableWakeTimers()
        {
            // Disable wake timers in current power plan
            RunPowerCfgCommand("/setacvalueindex scheme_current 238c9fa8-0aad-41ed-83f4-97be242c8f20 bd3b718a-0680-4d9d-8ab2-e1d2b4ac806d 0");
            RunPowerCfgCommand("/setdcvalueindex scheme_current 238c9fa8-0aad-41ed-83f4-97be242c8f20 bd3b718a-0680-4d9d-8ab2-e1d2b4ac806d 0");
            RunPowerCfgCommand("/setactive scheme_current");
            return true;
        }

        private bool EnableWakeTimers()
        {
            // Enable wake timers
            RunPowerCfgCommand("/setacvalueindex scheme_current 238c9fa8-0aad-41ed-83f4-97be242c8f20 bd3b718a-0680-4d9d-8ab2-e1d2b4ac806d 1");
            RunPowerCfgCommand("/setdcvalueindex scheme_current 238c9fa8-0aad-41ed-83f4-97be242c8f20 bd3b718a-0680-4d9d-8ab2-e1d2b4ac806d 1");
            RunPowerCfgCommand("/setactive scheme_current");
            return true;
        }

        #endregion

        #region Storage Tweaks

        private bool CleanTempFiles()
        {
            try
            {
                var tempPaths = new[]
                {
                    Environment.GetEnvironmentVariable("TEMP") ?? Path.GetTempPath(),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp")
                };

                foreach (var tempPath in tempPaths)
                {
                    if (Directory.Exists(tempPath))
                    {
                        foreach (var file in Directory.GetFiles(tempPath, "*", SearchOption.AllDirectories))
                        {
                            try { File.Delete(file); } catch { }
                        }
                        foreach (var dir in Directory.GetDirectories(tempPath))
                        {
                            try { Directory.Delete(dir, true); } catch { }
                        }
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool CleanWindowsUpdateCache()
        {
            try
            {
                // Stop Windows Update service
                RunCommand("net", "stop wuauserv /y");
                RunCommand("net", "stop bits /y");

                // Clear the SoftwareDistribution folder
                var softwareDistPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SoftwareDistribution", "Download");
                if (Directory.Exists(softwareDistPath))
                {
                    foreach (var file in Directory.GetFiles(softwareDistPath, "*", SearchOption.AllDirectories))
                    {
                        try { File.Delete(file); } catch { }
                    }
                    foreach (var dir in Directory.GetDirectories(softwareDistPath))
                    {
                        try { Directory.Delete(dir, true); } catch { }
                    }
                }

                // Restart Windows Update service
                RunCommand("net", "start wuauserv");
                RunCommand("net", "start bits");

                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool DisablePrefetch()
        {
            SetRegistryValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters", "EnablePrefetcher", 0);
            SetRegistryValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters", "EnableSuperfetch", 0);
            return true;
        }

        private bool EnablePrefetch()
        {
            SetRegistryValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters", "EnablePrefetcher", 3);
            return true;
        }

        private bool DisableSuperfetch()
        {
            // Disable via registry
            SetRegistryValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters", "EnableSuperfetch", 0);
            // Stop and disable SysMain service
            RunCommand("sc", "stop SysMain");
            RunCommand("sc", "config SysMain start=disabled");
            return true;
        }

        private bool EnableSuperfetch()
        {
            SetRegistryValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters", "EnableSuperfetch", 3);
            RunCommand("sc", "config SysMain start=auto");
            RunCommand("sc", "start SysMain");
            return true;
        }

        private bool RunSsdTrim()
        {
            // Run defrag /O which performs TRIM on SSDs
            RunCommand("defrag", "C: /O");
            return true;
        }

        private bool CleanRecycleBin()
        {
            try
            {
                // Use rd command to clear recycle bin for all drives
                RunCommand("cmd", "/c rd /s /q C:\\$Recycle.Bin");
                
                // Also try the PowerShell Clear-RecycleBin approach for current user
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = "-Command \"Clear-RecycleBin -Force -ErrorAction SilentlyContinue\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process.Start(psi)?.WaitForExit(10000);
                
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool CleanThumbnailsCache()
        {
            try
            {
                var thumbCachePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Microsoft", "Windows", "Explorer");

                if (Directory.Exists(thumbCachePath))
                {
                    foreach (var file in Directory.GetFiles(thumbCachePath, "thumbcache*.db"))
                    {
                        try { File.Delete(file); } catch { }
                    }
                    foreach (var file in Directory.GetFiles(thumbCachePath, "iconcache*.db"))
                    {
                        try { File.Delete(file); } catch { }
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool RunCommand(string fileName, string arguments)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using var process = Process.Start(psi);
                process?.WaitForExit(30000);
                return process?.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Helper Methods

        private void SetRegistryValue(RegistryKey rootKey, string subKeyPath, string valueName, object value)
        {
            using var key = rootKey.CreateSubKey(subKeyPath, true);
            if (key != null)
            {
                if (value is int intValue)
                    key.SetValue(valueName, intValue, RegistryValueKind.DWord);
                else if (value is string strValue)
                    key.SetValue(valueName, strValue, RegistryValueKind.String);
                else if (value is byte[] byteValue)
                    key.SetValue(valueName, byteValue, RegistryValueKind.Binary);
            }
        }

        private void DeleteRegistryValue(RegistryKey rootKey, string subKeyPath, string valueName)
        {
            try
            {
                using var key = rootKey.OpenSubKey(subKeyPath, true);
                key?.DeleteValue(valueName, false);
            }
            catch
            {
                // Value doesn't exist, which is fine
            }
        }

        #endregion
    }
}
