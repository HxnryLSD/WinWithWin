using System;
using System.Windows;
using WinWithWin.GUI.Services;

namespace WinWithWin.GUI
{
    public partial class App : Application
    {
        public App()
        {
            // Ensure the application exits completely when the main window closes
            ShutdownMode = ShutdownMode.OnMainWindowClose;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Extract embedded resources (config, locales) for single-file deployment
            try
            {
                EmbeddedResourceExtractor.ExtractIfNeeded();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Resource extraction warning: {ex.Message}");
                // Continue anyway - files might already exist
            }
            
            // Global exception handler
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                // Ignore known harmless WPF tooltip/popup errors
                if (IsKnownHarmlessException(ex)) return;
                
                MessageBox.Show($"Fatal error: {ex?.Message}\n\n{ex?.StackTrace}", 
                    "WinWithWin Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            };
            
            DispatcherUnhandledException += (s, args) =>
            {
                // Ignore known harmless WPF tooltip/popup errors
                if (IsKnownHarmlessException(args.Exception))
                {
                    args.Handled = true;
                    return;
                }
                
                MessageBox.Show($"Error: {args.Exception.Message}\n\n{args.Exception.StackTrace}", 
                    "WinWithWin Error", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };
            
            // Check for admin rights
            if (!Services.AdminService.IsRunningAsAdmin())
            {
                var result = MessageBox.Show(
                    "WinWithWin requires administrator privileges to apply tweaks.\n\nDo you want to restart as administrator?",
                    "Administrator Required",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                
                if (result == MessageBoxResult.Yes)
                {
                    Services.AdminService.RestartAsAdmin();
                }
                else
                {
                    Environment.Exit(0);
                }
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Suppress any shutdown exceptions
            try
            {
                base.OnExit(e);
            }
            catch
            {
                // Ignore shutdown errors
            }
            
            // Force process termination to ensure clean exit
            Environment.Exit(e.ApplicationExitCode);
        }

        /// <summary>
        /// Checks if the exception is a known harmless WPF issue that can be safely ignored.
        /// </summary>
        private static bool IsKnownHarmlessException(Exception? ex)
        {
            if (ex == null) return false;

            var stackTrace = ex.StackTrace ?? "";
            var message = ex.Message ?? "";

            // Known WPF tooltip/popup accessibility bridge issues
            // These occur on some Windows configurations and are harmless
            if (stackTrace.Contains("PopupSecurityHelper") ||
                stackTrace.Contains("ForceMsaaToUiaBridge") ||
                stackTrace.Contains("ToolTip.OnIsOpenChanged") ||
                stackTrace.Contains("PopupControlService.ShowToolTip"))
            {
                System.Diagnostics.Debug.WriteLine($"Ignored harmless WPF exception: {message}");
                return true;
            }

            // Known WPF shutdown/telemetry issues
            // These occur during application close and are harmless
            if (stackTrace.Contains("ControlsTraceLogger") ||
                stackTrace.Contains("LogUsedControlsDetails") ||
                stackTrace.Contains("CriticalShutdown") ||
                stackTrace.Contains("UpdateWindowListsOnClose") ||
                stackTrace.Contains("InternalDispose") ||
                stackTrace.Contains("WmDestroy"))
            {
                System.Diagnostics.Debug.WriteLine($"Ignored harmless WPF shutdown exception: {message}");
                return true;
            }

            return false;
        }
    }
}
