using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using Microsoft.Win32;

namespace WinWithWin.GUI.Services
{
    /// <summary>
    /// Result of a tweak operation with detailed information.
    /// </summary>
    public class TweakResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ErrorDetails { get; set; }
        public Exception? Exception { get; set; }

        public static TweakResult Ok() => new() { Success = true };
        
        public static TweakResult Fail(string message, string? details = null, Exception? ex = null) => new()
        {
            Success = false,
            ErrorMessage = message,
            ErrorDetails = details,
            Exception = ex
        };

        public static TweakResult NotSupported(string tweakId) => new()
        {
            Success = false,
            ErrorMessage = $"Tweak '{tweakId}' is not implemented in native mode"
        };
    }

    /// <summary>
    /// Native C# implementation of tweaks using Registry modifications.
    /// This allows tweaks to work without PowerShell modules.
    /// </summary>
    public class NativeTweakService
    {
        /// <summary>
        /// Applies a tweak by its ID using native C# Registry operations.
        /// Returns detailed result with error information.
        /// </summary>
        public TweakResult ApplyTweakWithDetails(string tweakId)
        {
            try
            {
                LoggingService.LogInfo($"Attempting to apply tweak: {tweakId}");
                
                var result = tweakId switch
                {
                    "disable_telemetry" => ExecuteWithLogging(nameof(DisableTelemetry), DisableTelemetry),
                    "disable_advertising_id" => ExecuteWithLogging(nameof(DisableAdvertisingId), DisableAdvertisingId),
                    "disable_activity_history" => ExecuteWithLogging(nameof(DisableActivityHistory), DisableActivityHistory),
                    "disable_location_tracking" => ExecuteWithLogging(nameof(DisableLocationTracking), DisableLocationTracking),
                    "disable_feedback" => ExecuteWithLogging(nameof(DisableFeedback), DisableFeedback),
                    "disable_tailored_experiences" => ExecuteWithLogging(nameof(DisableTailoredExperiences), DisableTailoredExperiences),
                    "disable_typing_data" => ExecuteWithLogging(nameof(DisableTypingData), DisableTypingData),
                    "disable_app_launch_tracking" => ExecuteWithLogging(nameof(DisableAppLaunchTracking), DisableAppLaunchTracking),
                    "disable_windows_spotlight" => ExecuteWithLogging(nameof(DisableWindowsSpotlight), DisableWindowsSpotlight),
                    "disable_cortana" => ExecuteWithLogging(nameof(DisableCortana), DisableCortana),
                    "optimize_visual_effects" => ExecuteWithLogging(nameof(OptimizeVisualEffects), OptimizeVisualEffects),
                    "disable_background_apps" => ExecuteWithLogging(nameof(DisableBackgroundApps), DisableBackgroundApps),
                    "disable_game_dvr" => ExecuteWithLogging(nameof(DisableGameDvr), DisableGameDvr),
                    "disable_xbox_game_bar" => ExecuteWithLogging(nameof(DisableXboxGameBar), DisableXboxGameBar),
                    "disable_consumer_features" => ExecuteWithLogging(nameof(DisableConsumerFeatures), DisableConsumerFeatures),
                    "disable_suggested_apps" => ExecuteWithLogging(nameof(DisableSuggestedApps), DisableSuggestedApps),
                    "disable_tips_and_tricks" => ExecuteWithLogging(nameof(DisableTipsAndTricks), DisableTipsAndTricks),
                    "disable_web_search" => ExecuteWithLogging(nameof(DisableWebSearch), DisableWebSearch),
                    "classic_context_menu" => ExecuteWithLogging(nameof(EnableClassicContextMenu), EnableClassicContextMenu),
                    // Network Tweaks
                    "dns_cloudflare" => ExecuteWithLogging(nameof(SetDnsCloudflare), SetDnsCloudflare),
                    "dns_google" => ExecuteWithLogging(nameof(SetDnsGoogle), SetDnsGoogle),
                    "dns_quad9" => ExecuteWithLogging(nameof(SetDnsQuad9), SetDnsQuad9),
                    "dns_auto" => ExecuteWithLogging(nameof(SetDnsAuto), SetDnsAuto),
                    "optimize_tcpip" => ExecuteWithLogging(nameof(OptimizeTcpIp), OptimizeTcpIp),
                    "reset_network" => ExecuteWithLogging(nameof(ResetNetwork), ResetNetwork),
                    // Power Tweaks
                    "power_high_performance" => ExecuteWithLogging(nameof(SetPowerHighPerformance), SetPowerHighPerformance),
                    "power_balanced" => ExecuteWithLogging(nameof(SetPowerBalanced), SetPowerBalanced),
                    "power_ultimate" => ExecuteWithLogging(nameof(SetPowerUltimate), SetPowerUltimate),
                    "disable_hibernate" => ExecuteWithLogging(nameof(DisableHibernate), DisableHibernate),
                    "disable_sleep" => ExecuteWithLogging(nameof(DisableSleep), DisableSleep),
                    "disable_usb_suspend" => ExecuteWithLogging(nameof(DisableUsbSuspend), DisableUsbSuspend),
                    "disable_wake_timers" => ExecuteWithLogging(nameof(DisableWakeTimers), DisableWakeTimers),
                    // Storage Tweaks
                    "clean_temp_files" => ExecuteWithLogging(nameof(CleanTempFiles), CleanTempFiles),
                    "clean_windows_update_cache" => ExecuteWithLogging(nameof(CleanWindowsUpdateCache), CleanWindowsUpdateCache),
                    "disable_prefetch" => ExecuteWithLogging(nameof(DisablePrefetch), DisablePrefetch),
                    "disable_superfetch" => ExecuteWithLogging(nameof(DisableSuperfetch), DisableSuperfetch),
                    "run_ssd_trim" => ExecuteWithLogging(nameof(RunSsdTrim), RunSsdTrim),
                    "clean_recycle_bin" => ExecuteWithLogging(nameof(CleanRecycleBin), CleanRecycleBin),
                    "clean_thumbnails_cache" => ExecuteWithLogging(nameof(CleanThumbnailsCache), CleanThumbnailsCache),
                    // Additional Performance Tweaks
                    "disable_fast_startup" => ExecuteWithLogging(nameof(DisableFastStartup), DisableFastStartup),
                    "ultimate_performance_plan" => ExecuteWithLogging(nameof(EnableUltimatePerformance), EnableUltimatePerformance),
                    "disable_unnecessary_services" => ExecuteWithLogging(nameof(DisableUnnecessaryServices), DisableUnnecessaryServices),
                    "optimize_gaming" => ExecuteWithLogging(nameof(OptimizeGaming), OptimizeGaming),
                    "disable_scheduled_tasks" => ExecuteWithLogging(nameof(DisableScheduledTasks), DisableScheduledTasks),
                    "optimize_memory" => ExecuteWithLogging(nameof(OptimizeMemory), OptimizeMemory),
                    // Security Tweaks
                    "disable_remote_desktop" => ExecuteWithLogging(nameof(DisableRemoteDesktop), DisableRemoteDesktop),
                    "configure_uac" => ExecuteWithLogging(nameof(ConfigureUac), ConfigureUac),
                    "configure_smartscreen" => ExecuteWithLogging(nameof(ConfigureSmartScreen), ConfigureSmartScreen),
                    "ensure_defender_enabled" => ExecuteWithLogging(nameof(EnsureDefenderEnabled), EnsureDefenderEnabled),
                    "configure_windows_update" => ExecuteWithLogging(nameof(ConfigureWindowsUpdate), ConfigureWindowsUpdate),
                    // Debloat Tweaks
                    "disable_widgets" => ExecuteWithLogging(nameof(DisableWidgets), DisableWidgets),
                    "disable_edge_features" => ExecuteWithLogging(nameof(DisableEdgeFeatures), DisableEdgeFeatures),
                    "restore_classic_context_menu" => ExecuteWithLogging(nameof(EnableClassicContextMenu), EnableClassicContextMenu),
                    "remove_bloatware" => ExecuteWithLogging(nameof(RemoveBloatware), RemoveBloatware),
                    "remove_onedrive" => ExecuteWithLogging(nameof(RemoveOneDrive), RemoveOneDrive),
                    _ => TweakResult.NotSupported(tweakId)
                };

                if (result.Success)
                {
                    LoggingService.LogInfo($"Tweak {tweakId} applied successfully");
                }
                else
                {
                    LoggingService.LogError($"Tweak {tweakId} failed: {result.ErrorMessage}", result.Exception);
                }

                return result;
            }
            catch (Exception ex)
            {
                var result = TweakResult.Fail($"Unexpected error applying tweak {tweakId}", ex.ToString(), ex);
                LoggingService.LogError($"Unexpected error in ApplyTweakWithDetails for {tweakId}", ex);
                return result;
            }
        }

        /// <summary>
        /// Undoes a tweak by its ID using native C# Registry operations.
        /// Returns detailed result with error information.
        /// </summary>
        public TweakResult UndoTweakWithDetails(string tweakId)
        {
            try
            {
                LoggingService.LogInfo($"Attempting to undo tweak: {tweakId}");
                
                var result = tweakId switch
                {
                    "disable_telemetry" => ExecuteWithLogging(nameof(EnableTelemetry), EnableTelemetry),
                    "disable_advertising_id" => ExecuteWithLogging(nameof(EnableAdvertisingId), EnableAdvertisingId),
                    "disable_activity_history" => ExecuteWithLogging(nameof(EnableActivityHistory), EnableActivityHistory),
                    "disable_location_tracking" => ExecuteWithLogging(nameof(EnableLocationTracking), EnableLocationTracking),
                    "disable_feedback" => ExecuteWithLogging(nameof(EnableFeedback), EnableFeedback),
                    "disable_tailored_experiences" => ExecuteWithLogging(nameof(EnableTailoredExperiences), EnableTailoredExperiences),
                    "disable_typing_data" => ExecuteWithLogging(nameof(EnableTypingData), EnableTypingData),
                    "disable_app_launch_tracking" => ExecuteWithLogging(nameof(EnableAppLaunchTracking), EnableAppLaunchTracking),
                    "disable_windows_spotlight" => ExecuteWithLogging(nameof(EnableWindowsSpotlight), EnableWindowsSpotlight),
                    "disable_cortana" => ExecuteWithLogging(nameof(EnableCortana), EnableCortana),
                    "optimize_visual_effects" => ExecuteWithLogging(nameof(RestoreVisualEffects), RestoreVisualEffects),
                    "disable_background_apps" => ExecuteWithLogging(nameof(EnableBackgroundApps), EnableBackgroundApps),
                    "disable_game_dvr" => ExecuteWithLogging(nameof(EnableGameDvr), EnableGameDvr),
                    "disable_xbox_game_bar" => ExecuteWithLogging(nameof(EnableXboxGameBar), EnableXboxGameBar),
                    "disable_consumer_features" => ExecuteWithLogging(nameof(EnableConsumerFeatures), EnableConsumerFeatures),
                    "disable_suggested_apps" => ExecuteWithLogging(nameof(EnableSuggestedApps), EnableSuggestedApps),
                    "disable_tips_and_tricks" => ExecuteWithLogging(nameof(EnableTipsAndTricks), EnableTipsAndTricks),
                    "disable_web_search" => ExecuteWithLogging(nameof(EnableWebSearch), EnableWebSearch),
                    "classic_context_menu" => ExecuteWithLogging(nameof(DisableClassicContextMenu), DisableClassicContextMenu),
                    // Network Tweaks - these are toggle/one-shot operations
                    "dns_cloudflare" => ExecuteWithLogging(nameof(SetDnsAuto), SetDnsAuto),
                    "dns_google" => ExecuteWithLogging(nameof(SetDnsAuto), SetDnsAuto),
                    "dns_quad9" => ExecuteWithLogging(nameof(SetDnsAuto), SetDnsAuto),
                    "dns_auto" => TweakResult.Ok(), // Already auto
                    "optimize_tcpip" => ExecuteWithLogging(nameof(ResetTcpIp), ResetTcpIp),
                    "reset_network" => TweakResult.Ok(), // One-shot operation
                    // Power Tweaks
                    "power_high_performance" => ExecuteWithLogging(nameof(SetPowerBalanced), SetPowerBalanced),
                    "power_balanced" => TweakResult.Ok(), // Already balanced
                    "power_ultimate" => ExecuteWithLogging(nameof(SetPowerBalanced), SetPowerBalanced),
                    "disable_hibernate" => ExecuteWithLogging(nameof(EnableHibernate), EnableHibernate),
                    "disable_sleep" => ExecuteWithLogging(nameof(EnableSleep), EnableSleep),
                    "disable_usb_suspend" => ExecuteWithLogging(nameof(EnableUsbSuspend), EnableUsbSuspend),
                    "disable_wake_timers" => ExecuteWithLogging(nameof(EnableWakeTimers), EnableWakeTimers),
                    // Storage Tweaks - most are one-shot operations
                    "clean_temp_files" => TweakResult.Ok(),
                    "clean_windows_update_cache" => TweakResult.Ok(),
                    "disable_prefetch" => ExecuteWithLogging(nameof(EnablePrefetch), EnablePrefetch),
                    "disable_superfetch" => ExecuteWithLogging(nameof(EnableSuperfetch), EnableSuperfetch),
                    "run_ssd_trim" => TweakResult.Ok(),
                    "clean_recycle_bin" => TweakResult.Ok(),
                    "clean_thumbnails_cache" => TweakResult.Ok(),
                    // Additional Performance Tweaks
                    "disable_fast_startup" => ExecuteWithLogging(nameof(EnableFastStartup), EnableFastStartup),
                    "ultimate_performance_plan" => ExecuteWithLogging(nameof(SetPowerBalanced), SetPowerBalanced),
                    "disable_unnecessary_services" => ExecuteWithLogging(nameof(EnableUnnecessaryServices), EnableUnnecessaryServices),
                    "optimize_gaming" => ExecuteWithLogging(nameof(UndoOptimizeGaming), UndoOptimizeGaming),
                    "disable_scheduled_tasks" => ExecuteWithLogging(nameof(EnableScheduledTasks), EnableScheduledTasks),
                    "optimize_memory" => ExecuteWithLogging(nameof(UndoOptimizeMemory), UndoOptimizeMemory),
                    // Security Tweaks
                    "disable_remote_desktop" => ExecuteWithLogging(nameof(EnableRemoteDesktop), EnableRemoteDesktop),
                    "configure_uac" => ExecuteWithLogging(nameof(ResetUac), ResetUac),
                    "configure_smartscreen" => ExecuteWithLogging(nameof(ResetSmartScreen), ResetSmartScreen),
                    "ensure_defender_enabled" => TweakResult.Ok(), // Already enabled, nothing to undo
                    "configure_windows_update" => ExecuteWithLogging(nameof(ResetWindowsUpdate), ResetWindowsUpdate),
                    // Debloat Tweaks
                    "disable_widgets" => ExecuteWithLogging(nameof(EnableWidgets), EnableWidgets),
                    "disable_edge_features" => ExecuteWithLogging(nameof(EnableEdgeFeatures), EnableEdgeFeatures),
                    "restore_classic_context_menu" => ExecuteWithLogging(nameof(DisableClassicContextMenu), DisableClassicContextMenu),
                    "remove_bloatware" => TweakResult.Ok(), // Cannot restore removed apps easily
                    "remove_onedrive" => TweakResult.Ok(), // Cannot restore OneDrive easily
                    _ => TweakResult.NotSupported(tweakId)
                };

                if (result.Success)
                {
                    LoggingService.LogInfo($"Tweak {tweakId} undone successfully");
                }
                else
                {
                    LoggingService.LogError($"Undo tweak {tweakId} failed: {result.ErrorMessage}", result.Exception);
                }

                return result;
            }
            catch (Exception ex)
            {
                var result = TweakResult.Fail($"Unexpected error undoing tweak {tweakId}", ex.ToString(), ex);
                LoggingService.LogError($"Unexpected error in UndoTweakWithDetails for {tweakId}", ex);
                return result;
            }
        }

        /// <summary>
        /// Executes a tweak function with detailed logging and error capture.
        /// </summary>
        private TweakResult ExecuteWithLogging(string functionName, Func<bool> action)
        {
            try
            {
                LoggingService.LogInfo($"  Executing: {functionName}");
                var success = action();
                
                if (success)
                {
                    LoggingService.LogInfo($"  {functionName}: Success");
                    return TweakResult.Ok();
                }
                else
                {
                    var msg = $"{functionName} returned false (operation may have partially failed or been skipped)";
                    LoggingService.LogWarning($"  {functionName}: {msg}");
                    return TweakResult.Fail(msg);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                var msg = $"Access denied: {ex.Message}";
                var details = $"Function: {functionName}\nThis usually means the application needs to run as Administrator, or the registry key is protected by Windows.\nPath: {ex.Message}";
                LoggingService.LogError($"  {functionName}: Access Denied - {ex.Message}");
                return TweakResult.Fail(msg, details, ex);
            }
            catch (System.Security.SecurityException ex)
            {
                var msg = $"Security error: {ex.Message}";
                var details = $"Function: {functionName}\nWindows security prevented this operation.\nDetails: {ex.Message}";
                LoggingService.LogError($"  {functionName}: Security Error - {ex.Message}");
                return TweakResult.Fail(msg, details, ex);
            }
            catch (IOException ex)
            {
                var msg = $"I/O error: {ex.Message}";
                var details = $"Function: {functionName}\nFailed to read/write files or registry.\nDetails: {ex.Message}";
                LoggingService.LogError($"  {functionName}: I/O Error - {ex.Message}");
                return TweakResult.Fail(msg, details, ex);
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                var msg = $"Windows error ({ex.NativeErrorCode}): {ex.Message}";
                var details = $"Function: {functionName}\nWindows API call failed.\nError Code: {ex.NativeErrorCode}\nDetails: {ex.Message}";
                LoggingService.LogError($"  {functionName}: Win32 Error {ex.NativeErrorCode} - {ex.Message}");
                return TweakResult.Fail(msg, details, ex);
            }
            catch (InvalidOperationException ex)
            {
                var msg = $"Invalid operation: {ex.Message}";
                var details = $"Function: {functionName}\nThe operation could not be performed in the current state.\nDetails: {ex.Message}";
                LoggingService.LogError($"  {functionName}: Invalid Operation - {ex.Message}");
                return TweakResult.Fail(msg, details, ex);
            }
            catch (Exception ex)
            {
                var msg = $"{ex.GetType().Name}: {ex.Message}";
                var details = $"Function: {functionName}\nException Type: {ex.GetType().FullName}\nMessage: {ex.Message}\nStack Trace:\n{ex.StackTrace}";
                LoggingService.LogError($"  {functionName}: Exception - {ex.GetType().Name}: {ex.Message}");
                return TweakResult.Fail(msg, details, ex);
            }
        }

        /// <summary>
        /// Legacy method for backward compatibility. Use ApplyTweakWithDetails for detailed results.
        /// </summary>
        public bool ApplyTweak(string tweakId)
        {
            return ApplyTweakWithDetails(tweakId).Success;
        }

        /// <summary>
        /// Legacy method for backward compatibility. Use UndoTweakWithDetails for detailed results.
        /// </summary>
        public bool UndoTweak(string tweakId)
        {
            return UndoTweakWithDetails(tweakId).Success;
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

        /// <summary>
        /// Alias for UndoOptimizeTcpIp for clearer naming in undo operations.
        /// </summary>
        private bool ResetTcpIp() => UndoOptimizeTcpIp();

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

        #region Additional Performance Tweaks

        private bool DisableFastStartup()
        {
            // Disable Fast Startup (Hybrid Boot)
            SetRegistryValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HiberbootEnabled", 0);
            return true;
        }

        private bool EnableFastStartup()
        {
            SetRegistryValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HiberbootEnabled", 1);
            return true;
        }

        private bool EnableUltimatePerformance()
        {
            // Try to enable Ultimate Performance power plan
            // First, duplicate it from the hidden plan
            var psi = new ProcessStartInfo
            {
                FileName = "powercfg",
                Arguments = "-duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };
            
            try
            {
                using var process = Process.Start(psi);
                process?.WaitForExit(10000);
                
                // Now set Ultimate Performance as active
                psi.Arguments = "-setactive e9a42b02-d5df-448d-aa00-03f14749eb61";
                using var setProcess = Process.Start(psi);
                setProcess?.WaitForExit(5000);
                return true;
            }
            catch
            {
                // Fallback: just set High Performance
                return SetPowerHighPerformance();
            }
        }

        private bool DisableUnnecessaryServices()
        {
            // Disable services that are typically not needed
            var servicesToDisable = new[]
            {
                "DiagTrack",           // Connected User Experiences and Telemetry
                "dmwappushservice",    // Device Management Wireless Application Protocol
                "MapsBroker",          // Downloaded Maps Manager
                "lfsvc",               // Geolocation Service
                "SharedAccess",        // Internet Connection Sharing (ICS)
                "RetailDemo",          // Retail Demo Service
                "WMPNetworkSvc",       // Windows Media Player Network Sharing Service
            };

            foreach (var service in servicesToDisable)
            {
                SetServiceStartType(service, "Disabled");
            }
            return true;
        }

        private bool EnableUnnecessaryServices()
        {
            var servicesToEnable = new[]
            {
                "DiagTrack",
                "dmwappushservice",
                "MapsBroker",
                "lfsvc",
                "SharedAccess",
                "RetailDemo",
                "WMPNetworkSvc",
            };

            foreach (var service in servicesToEnable)
            {
                SetServiceStartType(service, "Manual");
            }
            return true;
        }

        private void SetServiceStartType(string serviceName, string startType)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = $"config \"{serviceName}\" start= {startType.ToLower()}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var process = Process.Start(psi);
                process?.WaitForExit(5000);
            }
            catch
            {
                // Service might not exist, which is fine
            }
        }

        private bool OptimizeGaming()
        {
            // Disable fullscreen optimizations globally
            SetRegistryValue(Registry.CurrentUser, @"System\GameConfigStore", "GameDVR_FSEBehaviorMode", 2);
            SetRegistryValue(Registry.CurrentUser, @"System\GameConfigStore", "GameDVR_HonorUserFSEBehaviorMode", 1);
            SetRegistryValue(Registry.CurrentUser, @"System\GameConfigStore", "GameDVR_FSEBehavior", 2);
            
            // Disable Game Bar features
            SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\GameBar", "AllowAutoGameMode", 1);
            SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\GameBar", "AutoGameModeEnabled", 1);
            
            // Disable hardware-accelerated GPU scheduling nagle (for lower latency)
            SetRegistryValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 2);
            
            // Increase process priority for games
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Priority", 6);
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Scheduling Category", "High");
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "SFIO Priority", "High");
            
            return true;
        }

        private bool UndoOptimizeGaming()
        {
            DeleteRegistryValue(Registry.CurrentUser, @"System\GameConfigStore", "GameDVR_FSEBehaviorMode");
            DeleteRegistryValue(Registry.CurrentUser, @"System\GameConfigStore", "GameDVR_HonorUserFSEBehaviorMode");
            DeleteRegistryValue(Registry.CurrentUser, @"System\GameConfigStore", "GameDVR_FSEBehavior");
            DeleteRegistryValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode");
            return true;
        }

        private bool DisableScheduledTasks()
        {
            var tasksToDisable = new[]
            {
                @"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser",
                @"\Microsoft\Windows\Application Experience\ProgramDataUpdater",
                @"\Microsoft\Windows\Autochk\Proxy",
                @"\Microsoft\Windows\Customer Experience Improvement Program\Consolidator",
                @"\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip",
                @"\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector",
                @"\Microsoft\Windows\Feedback\Siuf\DmClient",
                @"\Microsoft\Windows\Feedback\Siuf\DmClientOnScenarioDownload",
            };

            foreach (var task in tasksToDisable)
            {
                DisableScheduledTask(task);
            }
            return true;
        }

        private bool EnableScheduledTasks()
        {
            var tasksToEnable = new[]
            {
                @"\Microsoft\Windows\Application Experience\Microsoft Compatibility Appraiser",
                @"\Microsoft\Windows\Application Experience\ProgramDataUpdater",
                @"\Microsoft\Windows\Autochk\Proxy",
                @"\Microsoft\Windows\Customer Experience Improvement Program\Consolidator",
                @"\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip",
                @"\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector",
                @"\Microsoft\Windows\Feedback\Siuf\DmClient",
                @"\Microsoft\Windows\Feedback\Siuf\DmClientOnScenarioDownload",
            };

            foreach (var task in tasksToEnable)
            {
                EnableScheduledTask(task);
            }
            return true;
        }

        private void DisableScheduledTask(string taskPath)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = $"/Change /TN \"{taskPath}\" /Disable",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var process = Process.Start(psi);
                process?.WaitForExit(5000);
            }
            catch
            {
                // Task might not exist
            }
        }

        private void EnableScheduledTask(string taskPath)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = $"/Change /TN \"{taskPath}\" /Enable",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var process = Process.Start(psi);
                process?.WaitForExit(5000);
            }
            catch
            {
                // Task might not exist
            }
        }

        private bool OptimizeMemory()
        {
            // Disable memory compression (can improve performance on systems with enough RAM)
            SetRegistryValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "DisablePagingExecutive", 1);
            
            // Increase system responsiveness
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", 0);
            
            // Clear page file at shutdown (security + fresh start)
            SetRegistryValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "ClearPageFileAtShutdown", 1);
            
            // Large system cache
            SetRegistryValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "LargeSystemCache", 0);
            
            return true;
        }

        private bool UndoOptimizeMemory()
        {
            SetRegistryValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "DisablePagingExecutive", 0);
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", 20);
            SetRegistryValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", "ClearPageFileAtShutdown", 0);
            return true;
        }

        #endregion

        #region Security Tweaks

        private bool DisableRemoteDesktop()
        {
            SetRegistryValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Terminal Server", "fDenyTSConnections", 1);
            
            // Disable Remote Desktop through firewall
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "advfirewall firewall set rule group=\"remote desktop\" new enable=No",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var process = Process.Start(psi);
                process?.WaitForExit(5000);
            }
            catch { }
            
            return true;
        }

        private bool EnableRemoteDesktop()
        {
            SetRegistryValue(Registry.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Terminal Server", "fDenyTSConnections", 0);
            
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "advfirewall firewall set rule group=\"remote desktop\" new enable=Yes",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var process = Process.Start(psi);
                process?.WaitForExit(5000);
            }
            catch { }
            
            return true;
        }

        private bool ConfigureUac()
        {
            // Set UAC to secure level (prompt on secure desktop)
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "ConsentPromptBehaviorAdmin", 2);
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "ConsentPromptBehaviorUser", 3);
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "EnableInstallerDetection", 1);
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "EnableLUA", 1);
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "EnableSecureUIAPaths", 1);
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "PromptOnSecureDesktop", 1);
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "ValidateAdminCodeSignatures", 0);
            return true;
        }

        private bool ResetUac()
        {
            // Reset to Windows defaults
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "ConsentPromptBehaviorAdmin", 5);
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "ConsentPromptBehaviorUser", 3);
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "PromptOnSecureDesktop", 1);
            return true;
        }

        private bool ConfigureSmartScreen()
        {
            // Enable SmartScreen for apps
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer", "SmartScreenEnabled", "On");
            
            // Enable SmartScreen for Edge
            SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Edge\SmartScreenEnabled", "", 1);
            
            // Block potentially unwanted apps
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender", "PUAProtection", 1);
            
            return true;
        }

        private bool ResetSmartScreen()
        {
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer", "SmartScreenEnabled", "Warn");
            DeleteRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender", "PUAProtection");
            return true;
        }

        private bool EnsureDefenderEnabled()
        {
            // Remove any policies that disable Defender
            DeleteRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender", "DisableAntiSpyware");
            DeleteRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection", "DisableRealtimeMonitoring");
            DeleteRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection", "DisableBehaviorMonitoring");
            
            // Enable cloud-delivered protection
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender\Spynet", "SpynetReporting", 2);
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows Defender\Spynet", "SubmitSamplesConsent", 1);
            
            return true;
        }

        private bool ConfigureWindowsUpdate()
        {
            // Set to notify before download
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoUpdate", 0);
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "AUOptions", 2); // Notify before download
            
            // Disable auto-restart with logged on users
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoRebootWithLoggedOnUsers", 1);
            
            // Defer feature updates by 30 days
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "DeferFeatureUpdates", 1);
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "DeferFeatureUpdatesPeriodInDays", 30);
            
            return true;
        }

        private bool ResetWindowsUpdate()
        {
            DeleteRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoUpdate");
            DeleteRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "AUOptions");
            DeleteRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoRebootWithLoggedOnUsers");
            DeleteRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "DeferFeatureUpdates");
            DeleteRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "DeferFeatureUpdatesPeriodInDays");
            return true;
        }

        #endregion

        #region Debloat Tweaks

        private bool DisableWidgets()
        {
            // Disable Windows Widgets (Windows 11)
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Dsh", "AllowNewsAndInterests", 0);
            SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarDa", 0);
            
            // Kill widgets process
            try
            {
                foreach (var process in Process.GetProcessesByName("Widgets"))
                {
                    process.Kill();
                }
            }
            catch { }
            
            return true;
        }

        private bool EnableWidgets()
        {
            DeleteRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Dsh", "AllowNewsAndInterests");
            SetRegistryValue(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "TaskbarDa", 1);
            return true;
        }

        private bool DisableEdgeFeatures()
        {
            // Disable Edge first run experience
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", "HideFirstRunExperience", 1);
            
            // Disable Edge sidebar
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", "HubsSidebarEnabled", 0);
            
            // Disable Edge collections
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", "EdgeCollectionsEnabled", 0);
            
            // Disable Edge shopping assistant
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", "EdgeShoppingAssistantEnabled", 0);
            
            // Disable Edge PDF viewer taking over
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", "AlwaysOpenPdfExternally", 1);
            
            // Disable startup boost
            SetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", "StartupBoostEnabled", 0);
            
            return true;
        }

        private bool EnableEdgeFeatures()
        {
            DeleteRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", "HideFirstRunExperience");
            DeleteRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", "HubsSidebarEnabled");
            DeleteRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", "EdgeCollectionsEnabled");
            DeleteRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", "EdgeShoppingAssistantEnabled");
            DeleteRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", "AlwaysOpenPdfExternally");
            DeleteRegistryValue(Registry.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", "StartupBoostEnabled");
            return true;
        }

        private bool RemoveBloatware()
        {
            // List of bloatware apps to remove
            var appsToRemove = new[]
            {
                "Microsoft.BingWeather",
                "Microsoft.BingNews",
                "Microsoft.GetHelp",
                "Microsoft.Getstarted",
                "Microsoft.MicrosoftOfficeHub",
                "Microsoft.MicrosoftSolitaireCollection",
                "Microsoft.People",
                "Microsoft.PowerAutomateDesktop",
                "Microsoft.Todos",
                "Microsoft.WindowsAlarms",
                "Microsoft.WindowsFeedbackHub",
                "Microsoft.WindowsMaps",
                "Microsoft.WindowsSoundRecorder",
                "Microsoft.YourPhone",
                "Microsoft.ZuneMusic",
                "Microsoft.ZuneVideo",
                "Clipchamp.Clipchamp",
                "Microsoft.GamingApp",
                "Microsoft.549981C3F5F10", // Cortana
                "MicrosoftTeams",
            };

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            foreach (var app in appsToRemove)
            {
                psi.Arguments = $"-NoProfile -Command \"Get-AppxPackage -Name '{app}' -AllUsers | Remove-AppxPackage -AllUsers -ErrorAction SilentlyContinue\"";
                try
                {
                    using var process = Process.Start(psi);
                    process?.WaitForExit(30000);
                }
                catch { }
            }

            return true;
        }

        private bool RemoveOneDrive()
        {
            // Kill OneDrive process
            try
            {
                foreach (var process in Process.GetProcessesByName("OneDrive"))
                {
                    process.Kill();
                }
            }
            catch { }

            System.Threading.Thread.Sleep(1000);

            // Find and run OneDrive uninstaller
            var possiblePaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\OneDrive\OneDriveSetup.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"System32\OneDriveSetup.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"SysWOW64\OneDriveSetup.exe"),
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = path,
                        Arguments = "/uninstall",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    try
                    {
                        using var process = Process.Start(psi);
                        process?.WaitForExit(60000);
                        break;
                    }
                    catch { }
                }
            }

            // Remove OneDrive from Explorer sidebar
            try
            {
                using var key = Registry.ClassesRoot.OpenSubKey(@"CLSID\{018D5C66-4533-4307-9B53-224DE2ED1FE6}", true);
                if (key != null)
                {
                    key.SetValue("System.IsPinnedToNameSpaceTree", 0);
                }
            }
            catch { }

            // Remove scheduled tasks
            DisableScheduledTask(@"\OneDrive Reporting Task");
            DisableScheduledTask(@"\OneDrive Standalone Update Task");

            return true;
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
