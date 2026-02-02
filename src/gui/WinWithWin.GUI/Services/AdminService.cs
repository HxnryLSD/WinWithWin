using System;
using System.Diagnostics;
using System.Security.Principal;

namespace WinWithWin.GUI.Services
{
    public static class AdminService
    {
        public static bool IsRunningAsAdmin()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static void RestartAsAdmin()
        {
            var processInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? "WinWithWin.GUI.exe",
                Verb = "runas"
            };

            try
            {
                Process.Start(processInfo);
                Environment.Exit(0);
            }
            catch (Exception)
            {
                // User cancelled UAC prompt
            }
        }
    }
}
