using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;
using WinWithWin.GUI.Models;
using WinWithWin.GUI.Services;

namespace WinWithWin.GUI
{
    public partial class MainWindow : Window
    {
        private readonly TweakService _tweakService;
        private readonly LocalizationService _localizationService;
        private readonly PowerShellService _powerShellService;
        private readonly FavoritesService _favoritesService;
        private readonly SettingsService _settingsService;
        private readonly ToastNotificationService _toastService;
        private readonly ProfileService _profileService;
        private readonly HistoryService _historyService;
        private readonly SchedulerService _schedulerService;
        private SystemTrayService? _systemTrayService;
        
        private ObservableCollection<TweakViewModel> _tweaks = new();
        private string _currentCategory = "All";
        private bool _isDarkTheme = true;
        private bool _isInitializing = true;

        public MainWindow()
        {
            try
            {
                InitializeComponent();

                _powerShellService = new PowerShellService();
                _localizationService = new LocalizationService();
                _tweakService = new TweakService(_powerShellService, _localizationService);
                _favoritesService = new FavoritesService();
                _settingsService = new SettingsService();
                _toastService = new ToastNotificationService();
                _profileService = new ProfileService();
                _historyService = new HistoryService();
                _schedulerService = new SchedulerService();

                // Subscribe to tweak service events for progress
                _tweakService.OperationStarted += OnOperationStarted;
                _tweakService.OperationEnded += OnOperationEnded;
                _tweakService.ProgressChanged += OnProgressChanged;

                // Subscribe to locale changes
                _localizationService.LocaleChanged += OnLocaleChanged;

                // Start logging session
                LoggingService.LogSessionStart();
                LoggingService.CleanupOldLogs();

                InitializeSystemTray();
                LoadSettings();
                LoadWindowsVersion();
                LoadTweaks();
                
                _isInitializing = false;
            }
            catch (Exception ex)
            {
                LoggingService.LogError("Failed to initialize application", ex);
                MessageBox.Show($"Failed to initialize application:\n\n{ex.Message}\n\n{ex.StackTrace}", 
                    "WinWithWin Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        private void OnLocaleChanged(object? sender, string locale)
        {
            // Update UI texts
            UpdateLocalizedTexts();
            
            // Reload tweaks to get localized names
            LoadTweaks();
            
            // Show confirmation
            _toastService?.ShowNotification(
                _localizationService.GetString("notifications.languageChanged.title"), 
                _localizationService.GetString("notifications.languageChanged.message"));
        }

        #region Progress Bar Handling

        private void OnOperationStarted(string operationName)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressContainer.Visibility = Visibility.Visible;
                OperationProgress.IsIndeterminate = true;
                ProgressText.Text = operationName;
            });
        }

        private void OnOperationEnded()
        {
            Dispatcher.Invoke(() =>
            {
                ProgressContainer.Visibility = Visibility.Collapsed;
                OperationProgress.IsIndeterminate = false;
                OperationProgress.Value = 0;
            });
        }

        private void OnProgressChanged(int percent, string message)
        {
            Dispatcher.Invoke(() =>
            {
                OperationProgress.IsIndeterminate = false;
                OperationProgress.Value = percent;
                ProgressText.Text = $"{percent}%";
            });
        }

        private void ShowProgress(string message, int percent = -1)
        {
            ProgressContainer.Visibility = Visibility.Visible;
            if (percent < 0)
            {
                OperationProgress.IsIndeterminate = true;
                ProgressText.Text = message;
            }
            else
            {
                OperationProgress.IsIndeterminate = false;
                OperationProgress.Value = percent;
                ProgressText.Text = $"{percent}%";
            }
        }

        private void HideProgress()
        {
            ProgressContainer.Visibility = Visibility.Collapsed;
            OperationProgress.IsIndeterminate = false;
            OperationProgress.Value = 0;
        }

        #endregion

        private void UpdateLocalizedTexts()
        {
            // Update category titles - use OfType<> to safely filter only ListBoxItem objects
            var categories = CategoryListBox.Items.OfType<ListBoxItem>().ToList();
            if (categories.Count >= 9)
            {
                categories[0].Content = "🏠  " + _localizationService.GetString("categories.all");
                categories[1].Content = "⭐  " + _localizationService.GetString("favorites", new Dictionary<string, string>()) ?? "Favorites";
                categories[2].Content = "🔒  " + _localizationService.GetString("categories.privacy");
                categories[3].Content = "⚡  " + _localizationService.GetString("categories.performance");
                categories[4].Content = "🛡️  " + _localizationService.GetString("categories.security");
                categories[5].Content = "📦  " + _localizationService.GetString("categories.debloat");
                categories[6].Content = "🌐  " + _localizationService.GetString("categories.network");
                categories[7].Content = "🔋  " + _localizationService.GetString("categories.power");
                categories[8].Content = "💾  " + _localizationService.GetString("categories.storage");
            }

            // Update search placeholder
            SearchBox.Tag = _localizationService.GetString("buttons.search");

            // Update status bar buttons - find buttons in status bar safely
            try
            {
                if (Content is Grid mainGrid && mainGrid.Children.Count > 3 && 
                    mainGrid.Children[3] is Border statusBorder && 
                    statusBorder.Child is Grid statusBarGrid)
                {
                    var buttonPanel = statusBarGrid.Children.OfType<StackPanel>().FirstOrDefault();
                    if (buttonPanel != null)
                    {
                        var buttons = buttonPanel.Children.OfType<Button>().ToList();
                        if (buttons.Count >= 2)
                        {
                            buttons[0].Content = _localizationService.GetString("buttons.applySelected");
                            buttons[1].Content = _localizationService.GetString("buttons.undoAll");
                        }
                    }
                }
            }
            catch
            {
                // Ignore if UI structure is different
            }

            // Update status text
            StatusText.Text = _localizationService.GetString("status.ready");

            // Update category description
            UpdateCategoryDescription();
        }

        private void UpdateCategoryDescription()
        {
            CategoryDescription.Text = _currentCategory switch
            {
                "Favorites" => _localizationService.GetString("categoryDescriptions.favorites") ?? "Your favorite tweaks for quick access",
                "Privacy" => _localizationService.GetString("categoryDescriptions.privacy"),
                "Performance" => _localizationService.GetString("categoryDescriptions.performance"),
                "Security" => _localizationService.GetString("categoryDescriptions.security"),
                "Debloat" => _localizationService.GetString("categoryDescriptions.debloat"),
                "Network" => _localizationService.GetString("categoryDescriptions.network"),
                "Power" => _localizationService.GetString("categoryDescriptions.power"),
                "Storage" => _localizationService.GetString("categoryDescriptions.storage"),
                _ => _localizationService.GetString("categoryDescriptions.all")
            };
        }

        private void InitializeSystemTray()
        {
            try
            {
                _systemTrayService = new SystemTrayService(this);
                _systemTrayService.ShowRequested += (s, e) => RestoreFromTray();
                _systemTrayService.ExitRequested += (s, e) => ExitApplication();
            }
            catch (Exception)
            {
                // System tray initialization failed - continue without it
                _systemTrayService = null;
            }
        }

        private void LoadSettings()
        {
            _isDarkTheme = _settingsService.IsDarkTheme;
            ApplyTheme(_isDarkTheme);
            ThemeToggleButton.Content = _isDarkTheme ? "🌙" : "☀️";
            
            MinimizeToTrayCheckBox.IsChecked = _settingsService.MinimizeToTray;
            
            // Set language combo
            foreach (ComboBoxItem item in LanguageComboBox.Items)
            {
                if (item.Tag?.ToString() == _settingsService.Locale)
                {
                    LanguageComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_settingsService.MinimizeToTray)
            {
                e.Cancel = true;
                _systemTrayService?.MinimizeToTray();
            }
            else
            {
                _systemTrayService?.Dispose();
            }
            
            base.OnClosing(e);
        }

        private void RestoreFromTray()
        {
            _systemTrayService?.RestoreFromTray();
        }

        private void ExitApplication()
        {
            _settingsService.MinimizeToTray = false; // Prevent recursion
            _systemTrayService?.Dispose();
            Application.Current.Shutdown();
        }

        #region Window Controls

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                MaximizeButton_Click(sender, e);
            }
            else
            {
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                // Reset border radius when not maximized
                MainBorder.CornerRadius = new CornerRadius(10);
                UpdateMaximizeIcon(false);
            }
            else
            {
                WindowState = WindowState.Maximized;
                // Remove border radius when maximized for full screen coverage
                MainBorder.CornerRadius = new CornerRadius(0);
                UpdateMaximizeIcon(true);
            }
        }

        private void UpdateMaximizeIcon(bool isMaximized)
        {
            // Update the maximize icon based on window state
            // When maximized, show restore icon (two rectangles)
            // When normal, show maximize icon (single rectangle)
            if (MaximizeIcon != null)
            {
                MaximizeIcon.Data = isMaximized 
                    ? Geometry.Parse("M2,0 H10 V8 H2 Z M0,2 H8 V10 H0 Z") // Restore icon
                    : Geometry.Parse("M0,0 H10 V10 H0 Z"); // Maximize icon
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            LoggingService.LogSessionEnd();
            Close();
        }

        #endregion

        private void MinimizeToTrayCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing || _settingsService == null) return;
            _settingsService.MinimizeToTray = MinimizeToTrayCheckBox.IsChecked == true;
        }

        private void LoadWindowsVersion()
        {
            var version = Environment.OSVersion.Version;
            var isWin11 = version.Build >= 22000;
            WindowsVersionText.Text = $"Windows {(isWin11 ? "11" : "10")} - Build {version.Build}";
        }

        private async void LoadTweaks()
        {
            try
            {
                StatusText.Text = _localizationService.GetString("status.checkingState");
                
                var tweaks = await _tweakService.LoadTweaksAsync();
                _tweaks = new ObservableCollection<TweakViewModel>(tweaks);
                
                // Load favorites
                foreach (var tweak in _tweaks)
                {
                    tweak.IsFavorite = _favoritesService.IsFavorite(tweak.Id);
                }
                
                FilterTweaks();
                
                // Count applied tweaks
                int appliedCount = _tweaks.Count(t => t.IsApplied);
                StatusText.Text = _localizationService.GetString("status.loaded", new Dictionary<string, string> {
                    { "count", _tweaks.Count.ToString() },
                    { "applied", appliedCount.ToString() }
                });
            }
            catch (Exception ex)
            {
                StatusText.Text = _localizationService.GetString("status.loadError", new Dictionary<string, string> { { "message", ex.Message } });
                MessageBox.Show(
                    _localizationService.GetString("dialogs.loadError.message", new Dictionary<string, string> { { "message", ex.Message } }), 
                    _localizationService.GetString("dialogs.loadError.title"), 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterTweaks()
        {
            if (_tweaks == null) return;

            var filtered = _tweaks.AsEnumerable();
            
            // Filter by category
            if (_currentCategory == "Favorites")
            {
                filtered = filtered.Where(t => t.IsFavorite);
            }
            else if (_currentCategory != "All")
            {
                filtered = filtered.Where(t => t.Category == _currentCategory);
            }

            // Filter by search
            var searchText = SearchBox.Text?.ToLower() ?? "";
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                filtered = filtered.Where(t => 
                    t.Name.ToLower().Contains(searchText) || 
                    t.Description.ToLower().Contains(searchText) ||
                    t.Category.ToLower().Contains(searchText) ||
                    t.Id.ToLower().Contains(searchText));
            }

            TweaksItemsControl.ItemsSource = filtered.ToList();
        }

        private void CategoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;
            
            if (CategoryListBox.SelectedItem is ListBoxItem item)
            {
                _currentCategory = item.Tag?.ToString() ?? "All";
                CategoryTitle.Text = item.Content?.ToString()?.Substring(4) ?? "All Tweaks";
                
                CategoryDescription.Text = _currentCategory switch
                {
                    "Favorites" => "Your favorite tweaks for quick access",
                    "Privacy" => "Control telemetry, tracking, and data collection",
                    "Performance" => "Optimize system speed and responsiveness",
                    "Security" => "Harden system security settings",
                    "Debloat" => "Remove preinstalled apps and bloatware",
                    "Network" => "DNS, TCP/IP, and network optimizations",
                    "Power" => "Power plans, sleep, and energy settings",
                    "Storage" => "Disk cleanup, temp files, and SSD optimization",
                    _ => "Browse and apply all available tweaks"
                };

                FilterTweaks();
            }
        }

        private void FavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tweakId)
            {
                var tweak = _tweaks.FirstOrDefault(t => t.Id == tweakId);
                if (tweak != null)
                {
                    tweak.IsFavorite = _favoritesService.ToggleFavorite(tweakId);
                    
                    // If we're viewing favorites and unfavorited, refresh the list
                    if (_currentCategory == "Favorites")
                    {
                        FilterTweaks();
                    }
                }
            }
        }

        private void ExpandTweakInfo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TweakViewModel tweak)
            {
                tweak.IsExpanded = !tweak.IsExpanded;
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterTweaks();
        }

        private async void TweakToggle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton toggle && toggle.Tag is string tweakId)
            {
                var tweak = _tweaks.FirstOrDefault(t => t.Id == tweakId);
                if (tweak == null) return;

                // Store original state to revert if needed
                var originalState = tweak.IsApplied;

                // Native tweak implementation is always available - no PowerShell check needed

                var isApplying = toggle.IsChecked == true;
                var action = isApplying ? "apply" : "undo";

                try
                {
                    StatusText.Text = isApplying 
                        ? _localizationService.GetString("status.applying", new Dictionary<string, string> { { "name", tweak.Name } })
                        : _localizationService.GetString("status.undoing", new Dictionary<string, string> { { "name", tweak.Name } });
                    
                    bool success;
                    if (isApplying)
                    {
                        success = await _tweakService.ApplyTweakAsync(tweakId);
                    }
                    else
                    {
                        success = await _tweakService.UndoTweakAsync(tweakId);
                    }

                    if (success)
                    {
                        tweak.IsApplied = isApplying;
                        StatusText.Text = isApplying
                            ? _localizationService.GetString("status.applySuccess", new Dictionary<string, string> { { "name", tweak.Name } })
                            : _localizationService.GetString("status.undoSuccess", new Dictionary<string, string> { { "name", tweak.Name } });
                        
                        // Show toast notification
                        if (_settingsService.NotificationsEnabled)
                        {
                            _toastService.ShowSuccess(
                                isApplying 
                                    ? _localizationService.GetString("notifications.tweakApplied.title")
                                    : _localizationService.GetString("notifications.tweakUndone.title"),
                                tweak.Name
                            );
                        }
                    }
                    else
                    {
                        toggle.IsChecked = !isApplying;
                        StatusText.Text = isApplying
                            ? _localizationService.GetString("status.applyFailed", new Dictionary<string, string> { { "name", tweak.Name } })
                            : _localizationService.GetString("status.undoFailed", new Dictionary<string, string> { { "name", tweak.Name } });
                        
                        if (_settingsService.NotificationsEnabled)
                        {
                            _toastService.ShowError(_localizationService.GetString("notifications.tweakFailed.title"), tweak.Name);
                        }
                    }
                }
                catch (Exception ex)
                {
                    toggle.IsChecked = !isApplying;
                    StatusText.Text = $"Error: {ex.Message}";
                }
            }
        }

        private async void PresetButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string presetName)
            {
                var result = MessageBox.Show(
                    _localizationService.GetString("dialogs.applyPreset.message", new Dictionary<string, string> { { "preset", presetName } }),
                    _localizationService.GetString("dialogs.applyPreset.title"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        StatusText.Text = _localizationService.GetString("status.restorePointCreating");
                        await _tweakService.CreateRestorePointAsync($"WinWithWin - {presetName} Preset");
                        
                        StatusText.Text = _localizationService.GetString("status.presetApplying", new Dictionary<string, string> { { "preset", presetName } });
                        var success = await _tweakService.ApplyPresetAsync(presetName);
                        
                        if (success)
                        {
                            StatusText.Text = _localizationService.GetString("status.presetSuccess", new Dictionary<string, string> { { "preset", presetName } });
                            
                            if (_settingsService.NotificationsEnabled)
                            {
                                _toastService.ShowSuccess(
                                    _localizationService.GetString("notifications.presetApplied.title"), 
                                    _localizationService.GetString("notifications.presetApplied.message", new Dictionary<string, string> { { "preset", presetName } }));
                            }
                            
                            LoadTweaks(); // Refresh states
                        }
                        else
                        {
                            StatusText.Text = _localizationService.GetString("status.presetPartialFail");
                            
                            if (_settingsService.NotificationsEnabled)
                            {
                                _toastService.ShowWarning(
                                    _localizationService.GetString("notifications.presetApplied.title"), 
                                    _localizationService.GetString("status.presetPartialFail"));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        StatusText.Text = _localizationService.GetString("status.error", new Dictionary<string, string> { { "message", ex.Message } });
                        MessageBox.Show(
                            _localizationService.GetString("dialogs.presetError.message", new Dictionary<string, string> { { "message", ex.Message } }), 
                            _localizationService.GetString("dialogs.presetError.title"),
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void CreateRestorePointButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusText.Text = _localizationService.GetString("status.restorePointCreating");
                var success = await _tweakService.CreateRestorePointAsync("WinWithWin Manual Backup");
                
                StatusText.Text = success 
                    ? _localizationService.GetString("status.restorePointSuccess") 
                    : _localizationService.GetString("status.restorePointFailed");
            }
            catch (Exception ex)
            {
                StatusText.Text = _localizationService.GetString("status.error", new Dictionary<string, string> { { "message", ex.Message } });
            }
        }

        private async void ApplySelectedButton_Click(object sender, RoutedEventArgs e)
        {
            // Get tweaks that are selected via checkbox AND not already applied
            var selectedTweaks = _tweaks.Where(t => t.IsSelected && !t.IsApplied).ToList();
            
            if (!selectedTweaks.Any())
            {
                // Check if any are selected at all
                var anySelected = _tweaks.Any(t => t.IsSelected);
                if (anySelected)
                {
                    MessageBox.Show(
                        _localizationService.GetString("dialogs.allAlreadyApplied.message"), 
                        _localizationService.GetString("dialogs.allAlreadyApplied.title"), 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(
                        _localizationService.GetString("dialogs.selectTweaksFirst.message"), 
                        _localizationService.GetString("dialogs.selectTweaksFirst.title"), 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                return;
            }

            var result = MessageBox.Show(
                _localizationService.GetString("dialogs.applySelected.message", new Dictionary<string, string> { { "count", selectedTweaks.Count.ToString() } }),
                _localizationService.GetString("dialogs.applySelected.title"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    StatusText.Text = _localizationService.GetString("status.restorePointCreating");
                    await _tweakService.CreateRestorePointAsync("WinWithWin Batch Apply");

                    var successCount = 0;
                    foreach (var tweak in selectedTweaks)
                    {
                        StatusText.Text = _localizationService.GetString("status.applying", new Dictionary<string, string> { { "name", tweak.Name } });
                        if (await _tweakService.ApplyTweakAsync(tweak.Id))
                        {
                            tweak.IsApplied = true;
                            tweak.IsSelected = false; // Clear selection after successful apply
                            successCount++;
                        }
                    }

                    StatusText.Text = _localizationService.GetString("status.appliedCount", new Dictionary<string, string> {
                        { "success", successCount.ToString() },
                        { "total", selectedTweaks.Count.ToString() }
                    });
                    
                    // Show notification
                    if (_settingsService.NotificationsEnabled && successCount > 0)
                    {
                        _toastService.ShowSuccess(
                            _localizationService.GetString("notifications.tweaksApplied.title"), 
                            _localizationService.GetString("notifications.tweaksApplied.message", new Dictionary<string, string> { { "count", successCount.ToString() } }));
                    }
                }
                catch (Exception ex)
                {
                    StatusText.Text = _localizationService.GetString("status.error", new Dictionary<string, string> { { "message", ex.Message } });
                }
            }
        }

        private async void UndoAllButton_Click(object sender, RoutedEventArgs e)
        {
            var appliedTweaks = _tweaks.Where(t => t.IsApplied).ToList();
            
            if (!appliedTweaks.Any())
            {
                MessageBox.Show(
                    _localizationService.GetString("dialogs.noAppliedTweaks.message"), 
                    _localizationService.GetString("dialogs.noAppliedTweaks.title"), 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                _localizationService.GetString("dialogs.undoAll.message", new Dictionary<string, string> { { "count", appliedTweaks.Count.ToString() } }),
                _localizationService.GetString("dialogs.undoAll.title"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var successCount = 0;
                    foreach (var tweak in appliedTweaks)
                    {
                        StatusText.Text = _localizationService.GetString("status.undoing", new Dictionary<string, string> { { "name", tweak.Name } });
                        if (await _tweakService.UndoTweakAsync(tweak.Id))
                        {
                            tweak.IsApplied = false;
                            successCount++;
                        }
                    }

                    StatusText.Text = _localizationService.GetString("status.undoneCount", new Dictionary<string, string> {
                        { "success", successCount.ToString() },
                        { "total", appliedTweaks.Count.ToString() }
                    });
                    FilterTweaks();
                }
                catch (Exception ex)
                {
                    StatusText.Text = _localizationService.GetString("status.error", new Dictionary<string, string> { { "message", ex.Message } });
                }
            }
        }

        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _isDarkTheme = !_isDarkTheme;
            _settingsService.IsDarkTheme = _isDarkTheme;
            ThemeToggleButton.Content = _isDarkTheme ? "🌙" : "☀️";
            ApplyTheme(_isDarkTheme);
        }

        private void ApplyTheme(bool isDark)
        {
            var themeName = isDark ? "DarkTheme" : "LightTheme";
            
            try
            {
                var themeDict = new ResourceDictionary
                {
                    Source = new Uri($"Themes/{themeName}.xaml", UriKind.Relative)
                };
                
                var stylesDict = new ResourceDictionary
                {
                    Source = new Uri("Themes/Styles.xaml", UriKind.Relative)
                };
                
                Application.Current.Resources.MergedDictionaries.Clear();
                Application.Current.Resources.MergedDictionaries.Add(themeDict);
                Application.Current.Resources.MergedDictionaries.Add(stylesDict);
            }
            catch (Exception ex)
            {
                StatusText.Text = _localizationService.GetString("status.themeFailed", new Dictionary<string, string> { { "message", ex.Message } });
            }
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || _localizationService == null || _settingsService == null) return;
            
            if (LanguageComboBox.SelectedItem is ComboBoxItem item)
            {
                var locale = item.Tag?.ToString() ?? "en";
                _localizationService.SetLocale(locale);
                _settingsService.Locale = locale;
            }
        }

        #region Profile Export/Import

        private void ExportProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var appliedTweaks = _tweaks.Where(t => t.IsApplied).ToList();
            
            if (!appliedTweaks.Any())
            {
                MessageBox.Show(
                    _localizationService.GetString("dialogs.noAppliedToExport.message"), 
                    _localizationService.GetString("dialogs.noAppliedToExport.title"),
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "WinWithWin Profile (*.json)|*.json",
                DefaultExt = ".json",
                FileName = $"WinWithWin_Profile_{DateTime.Now:yyyyMMdd}"
            };

            if (dialog.ShowDialog() == true)
            {
                var profileName = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);
                var success = _profileService.ExportProfileToFile(dialog.FileName, _tweaks, profileName);
                
                if (success)
                {
                    StatusText.Text = _localizationService.GetString("status.profileExported", new Dictionary<string, string> { { "count", appliedTweaks.Count.ToString() } });
                    _toastService.ShowSuccess(
                        _localizationService.GetString("notifications.profileExported.title"), 
                        _localizationService.GetString("notifications.profileExported.message", new Dictionary<string, string> { { "count", appliedTweaks.Count.ToString() } }));
                }
                else
                {
                    StatusText.Text = _localizationService.GetString("status.exportFailed");
                    MessageBox.Show(
                        _localizationService.GetString("dialogs.profileError.message"), 
                        _localizationService.GetString("dialogs.profileError.title"),
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ImportProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "WinWithWin Profile (*.json)|*.json",
                DefaultExt = ".json"
            };

            if (dialog.ShowDialog() == true)
            {
                var profile = _profileService.ImportProfile(dialog.FileName);
                
                if (profile == null)
                {
                    MessageBox.Show(
                        _localizationService.GetString("dialogs.invalidProfile.message"), 
                        _localizationService.GetString("dialogs.invalidProfile.title"),
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var result = MessageBox.Show(
                    _localizationService.GetString("dialogs.importProfile.message", new Dictionary<string, string> { 
                        { "count", profile.TweakCount.ToString() }, 
                        { "name", profile.Name } 
                    }),
                    _localizationService.GetString("dialogs.importProfile.title"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        StatusText.Text = _localizationService.GetString("status.restorePointCreating");
                        await _tweakService.CreateRestorePointAsync($"WinWithWin - {profile.Name}");

                        var successCount = 0;
                        foreach (var tweakEntry in profile.Tweaks)
                        {
                            var tweak = _tweaks.FirstOrDefault(t => t.Id == tweakEntry.Id);
                            if (tweak != null && !tweak.IsApplied)
                            {
                                StatusText.Text = _localizationService.GetString("status.applying", new Dictionary<string, string> { { "name", tweak.Name } });
                                if (await _tweakService.ApplyTweakAsync(tweak.Id))
                                {
                                    tweak.IsApplied = true;
                                    _historyService.RecordApply(tweak.Id, tweak.Name, true);
                                    successCount++;
                                }
                            }
                        }

                        StatusText.Text = _localizationService.GetString("status.profileApplied", new Dictionary<string, string> {
                            { "success", successCount.ToString() },
                            { "total", profile.TweakCount.ToString() }
                        });
                        _toastService.ShowSuccess(
                            _localizationService.GetString("notifications.profileImported.title"), 
                            _localizationService.GetString("notifications.profileImported.message", new Dictionary<string, string> { { "count", successCount.ToString() } }));
                        FilterTweaks();
                    }
                    catch (Exception ex)
                    {
                        StatusText.Text = _localizationService.GetString("status.error", new Dictionary<string, string> { { "message", ex.Message } });
                    }
                }
            }
        }

        #endregion

        #region Batch Operations

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            var currentItems = TweaksItemsControl.ItemsSource as IEnumerable<TweakViewModel>;
            if (currentItems == null) return;

            var allSelected = currentItems.All(t => t.IsSelected);
            
            foreach (var tweak in currentItems)
            {
                tweak.IsSelected = !allSelected;
            }
        }

        private async void BatchUndoButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedTweaks = _tweaks.Where(t => t.IsSelected && t.IsApplied).ToList();

            if (!selectedTweaks.Any())
            {
                MessageBox.Show(
                    _localizationService.GetString("dialogs.noSelectedApplied.message"), 
                    _localizationService.GetString("dialogs.noSelectedApplied.title"), 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                _localizationService.GetString("dialogs.undoSelected.message", new Dictionary<string, string> { { "count", selectedTweaks.Count.ToString() } }),
                _localizationService.GetString("dialogs.undoSelected.title"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var successCount = 0;
                    foreach (var tweak in selectedTweaks)
                    {
                        StatusText.Text = _localizationService.GetString("status.undoing", new Dictionary<string, string> { { "name", tweak.Name } });
                        if (await _tweakService.UndoTweakAsync(tweak.Id))
                        {
                            tweak.IsApplied = false;
                            tweak.IsSelected = false;
                            _historyService.RecordUndo(tweak.Id, tweak.Name, true);
                            successCount++;
                        }
                    }

                    StatusText.Text = _localizationService.GetString("status.undoneCount", new Dictionary<string, string> {
                        { "success", successCount.ToString() },
                        { "total", selectedTweaks.Count.ToString() }
                    });
                    _historyService.RecordChange("batch", "Batch Undo", HistoryAction.BatchUndone, true, $"{successCount} tweaks");
                    _toastService.ShowSuccess(
                        _localizationService.GetString("notifications.tweaksApplied.title"), 
                        _localizationService.GetString("status.undoneCount", new Dictionary<string, string> {
                            { "success", successCount.ToString() },
                            { "total", selectedTweaks.Count.ToString() }
                        }));
                    FilterTweaks();
                }
                catch (Exception ex)
                {
                    StatusText.Text = _localizationService.GetString("status.error", new Dictionary<string, string> { { "message", ex.Message } });
                }
            }
        }

        #endregion

        #region History & Scheduler

        private void ViewHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            var history = _historyService.GetRecentHistory(50);
            
            if (!history.Any())
            {
                MessageBox.Show(
                    _localizationService.GetString("history.noEntries"), 
                    _localizationService.GetString("history.title"), 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var historyText = string.Join("\n", history.Select(h => 
                $"{h.FormattedTimestamp} | {h.StatusIcon} {h.ActionText}: {h.TweakName}"
            ));

            var historyWindow = new Window
            {
                Title = _localizationService.GetString("history.title"),
                Width = 600,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = (Brush)Application.Current.Resources["BackgroundBrush"]
            };

            var scrollViewer = new ScrollViewer { Margin = new Thickness(15) };
            var textBlock = new TextBlock
            {
                Text = historyText,
                Foreground = (Brush)Application.Current.Resources["PrimaryTextBrush"],
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12
            };
            scrollViewer.Content = textBlock;
            historyWindow.Content = scrollViewer;
            historyWindow.ShowDialog();
        }

        private void ScheduledTweaksButton_Click(object sender, RoutedEventArgs e)
        {
            var schedules = _schedulerService.GetSchedules();
            
            var message = schedules.Any()
                ? _localizationService.GetString("scheduler.header") + "\n\n" + string.Join("\n", schedules.Select(s => 
                    $"{s.StatusIcon} {s.TweakName} - {s.ActionText} ({s.RecurrenceText}) at {s.ScheduledTime:HH:mm}"))
                : _localizationService.GetString("scheduler.noSchedules");

            MessageBox.Show(message, _localizationService.GetString("scheduler.title"), MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion
    }
}
