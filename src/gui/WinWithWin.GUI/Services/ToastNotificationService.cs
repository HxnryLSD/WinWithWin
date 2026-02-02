using System;
using System.Runtime.InteropServices;

namespace WinWithWin.GUI.Services
{
    /// <summary>
    /// Service for displaying Windows Toast Notifications
    /// </summary>
    public class ToastNotificationService
    {
        private const string AppId = "WinWithWin.GUI";

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        /// <summary>
        /// Shows a toast notification with the specified title and message
        /// </summary>
        public void ShowNotification(string title, string message, NotificationType type = NotificationType.Info)
        {
            try
            {
                // Use Windows 10/11 native toast notifications via PowerShell
                var icon = type switch
                {
                    NotificationType.Success => "✅",
                    NotificationType.Warning => "⚠️",
                    NotificationType.Error => "❌",
                    _ => "ℹ️"
                };

                var script = $@"
                    [Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
                    [Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] | Out-Null

                    $template = @""
                    <toast>
                        <visual>
                            <binding template=""ToastGeneric"">
                                <text>{icon} {EscapeXml(title)}</text>
                                <text>{EscapeXml(message)}</text>
                            </binding>
                        </visual>
                        <audio silent=""true""/>
                    </toast>
""@

                    $xml = New-Object Windows.Data.Xml.Dom.XmlDocument
                    $xml.LoadXml($template)
                    
                    $notifier = [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('{AppId}')
                    $toast = New-Object Windows.UI.Notifications.ToastNotification $xml
                    $notifier.Show($toast)
                ";

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script.Replace("\"", "\\\"")}\"",
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                });
            }
            catch
            {
                // Fallback: Show nothing if toast fails
            }
        }

        /// <summary>
        /// Shows a success notification
        /// </summary>
        public void ShowSuccess(string title, string message)
        {
            ShowNotification(title, message, NotificationType.Success);
        }

        /// <summary>
        /// Shows a warning notification
        /// </summary>
        public void ShowWarning(string title, string message)
        {
            ShowNotification(title, message, NotificationType.Warning);
        }

        /// <summary>
        /// Shows an error notification
        /// </summary>
        public void ShowError(string title, string message)
        {
            ShowNotification(title, message, NotificationType.Error);
        }

        private static string EscapeXml(string text)
        {
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }
    }

    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }
}
