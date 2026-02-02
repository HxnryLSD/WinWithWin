using System;
using System.Windows;

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
            
            // Global exception handler
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                MessageBox.Show($"Fatal error: {ex?.Message}\n\n{ex?.StackTrace}", 
                    "WinWithWin Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            };
            
            DispatcherUnhandledException += (s, args) =>
            {
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
            base.OnExit(e);
            // Force process termination to ensure clean exit
            Environment.Exit(e.ApplicationExitCode);
        }
    }
}
