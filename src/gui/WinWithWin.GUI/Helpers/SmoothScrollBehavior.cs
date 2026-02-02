using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace WinWithWin.GUI.Helpers
{
    /// <summary>
    /// Provides smooth scrolling behavior for ScrollViewer controls.
    /// Dynamically adapts to the monitor's refresh rate for optimal smoothness.
    /// </summary>
    public static class SmoothScrollBehavior
    {
        // Win32 API for getting monitor refresh rate
        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool EnumDisplaySettingsW(string? lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

        private const int ENUM_CURRENT_SETTINGS = -1;
        private const int VREFRESH = 116; // GetDeviceCaps index for vertical refresh rate

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DEVMODE
        {
            private const int CCHDEVICENAME = 32;
            private const int CCHFORMNAME = 32;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public int dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
            public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        private static int _cachedRefreshRate = 0;
        private static DateTime _lastRefreshRateCheck = DateTime.MinValue;

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(SmoothScrollBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
        public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

        private static readonly DependencyProperty TargetVerticalOffsetProperty =
            DependencyProperty.RegisterAttached(
                "TargetVerticalOffset",
                typeof(double),
                typeof(SmoothScrollBehavior),
                new PropertyMetadata(0.0));

        private static double GetTargetVerticalOffset(DependencyObject obj) => (double)obj.GetValue(TargetVerticalOffsetProperty);
        private static void SetTargetVerticalOffset(DependencyObject obj, double value) => obj.SetValue(TargetVerticalOffsetProperty, value);

        /// <summary>
        /// Gets the current monitor's refresh rate in Hz using multiple methods.
        /// </summary>
        private static int GetMonitorRefreshRate(Visual visual)
        {
            // Cache the refresh rate for 10 seconds to avoid excessive calls
            if (_cachedRefreshRate > 0 && (DateTime.Now - _lastRefreshRateCheck).TotalSeconds < 10)
            {
                return _cachedRefreshRate;
            }

            int refreshRate = 60; // Default fallback

            // Method 1: Try using EnumDisplaySettings (most reliable for current display mode)
            try
            {
                var devMode = new DEVMODE();
                devMode.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
                
                if (EnumDisplaySettingsW(null, ENUM_CURRENT_SETTINGS, ref devMode))
                {
                    if (devMode.dmDisplayFrequency > 0 && devMode.dmDisplayFrequency < 500)
                    {
                        refreshRate = devMode.dmDisplayFrequency;
                        _cachedRefreshRate = refreshRate;
                        _lastRefreshRateCheck = DateTime.Now;
                        return refreshRate;
                    }
                }
            }
            catch
            {
                // Continue to fallback methods
            }

            // Method 2: Try using GetDeviceCaps
            try
            {
                IntPtr hdc = GetDC(IntPtr.Zero);
                if (hdc != IntPtr.Zero)
                {
                    int vRefresh = GetDeviceCaps(hdc, VREFRESH);
                    ReleaseDC(IntPtr.Zero, hdc);
                    
                    if (vRefresh > 0 && vRefresh < 500)
                    {
                        refreshRate = vRefresh;
                        _cachedRefreshRate = refreshRate;
                        _lastRefreshRateCheck = DateTime.Now;
                        return refreshRate;
                    }
                }
            }
            catch
            {
                // Continue to fallback
            }

            // Method 3: Try to get from window's HwndSource
            try
            {
                if (visual != null)
                {
                    var hwndSource = PresentationSource.FromVisual(visual) as HwndSource;
                    if (hwndSource?.CompositionTarget != null)
                    {
                        // Use the rendering tier to estimate - high-end systems usually have high refresh
                        var tier = RenderCapability.Tier >> 16;
                        if (tier >= 2)
                        {
                            // Try EnumDisplaySettings again with specific window context
                            IntPtr hdc = GetDC(hwndSource.Handle);
                            if (hdc != IntPtr.Zero)
                            {
                                int vRefresh = GetDeviceCaps(hdc, VREFRESH);
                                ReleaseDC(hwndSource.Handle, hdc);
                                
                                if (vRefresh > 0 && vRefresh < 500)
                                {
                                    refreshRate = vRefresh;
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // Use default
            }

            _cachedRefreshRate = refreshRate;
            _lastRefreshRateCheck = DateTime.Now;
            return refreshRate;
        }

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewer scrollViewer)
            {
                if ((bool)e.NewValue)
                {
                    scrollViewer.PreviewMouseWheel += ScrollViewer_PreviewMouseWheel;
                    SetTargetVerticalOffset(scrollViewer, scrollViewer.VerticalOffset);
                }
                else
                {
                    scrollViewer.PreviewMouseWheel -= ScrollViewer_PreviewMouseWheel;
                }
            }
        }

        private static void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                e.Handled = true;

                double scrollAmount = 100; // Pixels to scroll per wheel notch
                double delta = -e.Delta / 120.0 * scrollAmount;

                double currentTarget = GetTargetVerticalOffset(scrollViewer);
                double newTarget = Math.Max(0, Math.Min(scrollViewer.ScrollableHeight, currentTarget + delta));
                
                SetTargetVerticalOffset(scrollViewer, newTarget);

                // Get monitor refresh rate and calculate optimal interval
                int refreshRate = GetMonitorRefreshRate(scrollViewer);
                double intervalMs = 1000.0 / refreshRate;

                AnimateScroll(scrollViewer, scrollViewer.VerticalOffset, newTarget, 180, intervalMs);
            }
        }

        private static void AnimateScroll(ScrollViewer scrollViewer, double from, double to, int durationMs, double intervalMs)
        {
            var startTime = DateTime.Now;
            var duration = TimeSpan.FromMilliseconds(durationMs);

            System.Windows.Threading.DispatcherTimer? timer = null;
            timer = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(intervalMs)
            };

            timer.Tick += (s, e) =>
            {
                var elapsed = DateTime.Now - startTime;
                if (elapsed >= duration)
                {
                    scrollViewer.ScrollToVerticalOffset(to);
                    timer?.Stop();
                    return;
                }

                // Cubic ease out for smoother deceleration
                double t = elapsed.TotalMilliseconds / durationMs;
                t = 1 - Math.Pow(1 - t, 3); // Ease out cubic

                double currentOffset = from + (to - from) * t;
                scrollViewer.ScrollToVerticalOffset(currentOffset);
            };

            timer.Start();
        }
    }
}
