using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;

namespace WinWithWin.GUI.Services
{
    /// <summary>
    /// Service for System Tray integration
    /// </summary>
    public class SystemTrayService : IDisposable
    {
        private NotifyIcon? _notifyIcon;
        private readonly Window _mainWindow;
        private bool _isDisposed;
        private bool _isInitialized;

        public event EventHandler? ShowRequested;
        public event EventHandler? ExitRequested;

        public SystemTrayService(Window mainWindow)
        {
            _mainWindow = mainWindow;
            try
            {
                InitializeTrayIcon();
                _isInitialized = true;
            }
            catch (Exception)
            {
                // System tray not available - continue without it
                _isInitialized = false;
            }
        }

        private void InitializeTrayIcon()
        {
            _notifyIcon = new NotifyIcon
            {
                Text = "WinWithWin",
                Visible = false
            };

            // Create icon from embedded resource or generate one
            _notifyIcon.Icon = CreateDefaultIcon();

            // Create context menu
            var contextMenu = new ContextMenuStrip();
            
            var showItem = new ToolStripMenuItem("Show WinWithWin");
            showItem.Click += (s, e) => ShowRequested?.Invoke(this, EventArgs.Empty);
            showItem.Font = new Font(showItem.Font, System.Drawing.FontStyle.Bold);
            contextMenu.Items.Add(showItem);
            
            contextMenu.Items.Add(new ToolStripSeparator());
            
            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => ExitRequested?.Invoke(this, EventArgs.Empty);
            contextMenu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.DoubleClick += (s, e) => ShowRequested?.Invoke(this, EventArgs.Empty);
        }

        private Icon CreateDefaultIcon()
        {
            // Try to load from file first
            try
            {
                var iconPath = System.IO.Path.Combine(PathHelper.AssetsPath, "icon.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    return new Icon(iconPath);
                }
            }
            catch { }

            // Create a simple default icon
            using var bitmap = new Bitmap(32, 32);
            using var graphics = Graphics.FromImage(bitmap);
            
            // Draw a simple "W" icon
            graphics.Clear(Color.FromArgb(0, 120, 212)); // Primary color
            using var font = new Font("Segoe UI", 16, System.Drawing.FontStyle.Bold);
            using var brush = new SolidBrush(Color.White);
            graphics.DrawString("W", font, brush, 4, 4);
            
            return Icon.FromHandle(bitmap.GetHicon());
        }

        /// <summary>
        /// Shows the tray icon and hides the main window
        /// </summary>
        public void MinimizeToTray()
        {
            if (!_isInitialized || _notifyIcon == null)
            {
                // Fallback: just minimize normally
                _mainWindow.WindowState = WindowState.Minimized;
                return;
            }
            
            _notifyIcon.Visible = true;
            _mainWindow.Hide();
            
            _notifyIcon.ShowBalloonTip(
                2000,
                "WinWithWin",
                "Application minimized to system tray",
                ToolTipIcon.Info
            );
        }

        /// <summary>
        /// Hides the tray icon and shows the main window
        /// </summary>
        public void RestoreFromTray()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
            }
            
            _mainWindow.Show();
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Activate();
        }

        /// <summary>
        /// Shows a balloon notification in the system tray
        /// </summary>
        public void ShowBalloon(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
        {
            _notifyIcon?.ShowBalloonTip(3000, title, message, icon);
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _notifyIcon?.Dispose();
                _isDisposed = true;
            }
        }
    }
}
