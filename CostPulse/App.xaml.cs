using System.Windows;
using CostPulse.Services;
using Hardcodet.Wpf.TaskbarNotification;
using System;

namespace CostPulse
{
    public partial class App : Application
    {
        public static ServiceContainer Services { get; private set; }
        
        // Backward compatibility proxies for existing code that uses App.Logger, App.DataService
        // Ideally we refactor all usage, but for now we map them.
        public static LoggingService Logger => Services?.Logger;
        public static DataService DataService => Services?.Data;
        public static ICloudSyncService CloudSync => Services?.CloudSync;
        public static ClipboardMonitorService ClipboardMonitor => Services?.ClipboardMonitor;
        public static LogImportService LogImporter => Services?.LogImporter;
        public static bool IsExiting { get; set; } = false;

        private System.Windows.Forms.NotifyIcon? _tray;
        private ViewModels.MainViewModel _mainViewModel;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Global exception handling
            DispatcherUnhandledException += (s, args) =>
            {
                // We access Logger safely
                Services?.Logger?.LogError("Unhandled UI Exception", args.Exception);
                args.Handled = true; 
                MessageBox.Show($"An error occurred: {args.Exception.Message}", "CostPulse Error", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            try
            {
                Services = new ServiceContainer();
                Services.StartServices();
                
                _mainViewModel = new ViewModels.MainViewModel(); // MainViewModel now uses App.DataService proxy
                
                SetupTrayIcon();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Startup failed: {ex.Message}");
                Shutdown();
            }
        }

        private void SetupTrayIcon()
        {
            // Load resource icon using Pack URI (works with single-file publish)
            System.Drawing.Icon? icon = null;
            try
            {
                var uri = new Uri("pack://application:,,,/app.ico");
                var streamInfo = System.Windows.Application.GetResourceStream(uri);
                if (streamInfo != null)
                {
                    using var stream = streamInfo.Stream;
                    icon = new System.Drawing.Icon(stream);
                }
            }
            catch (Exception ex)
            {
                // Logger.LogError("Failed to load tray icon", ex); // User requested to silence this
            }

            // Fallback if failed
            if (icon == null) icon = System.Drawing.SystemIcons.Information;

            _tray = new System.Windows.Forms.NotifyIcon
            {
                Icon = icon,
                Visible = true,
                Text = GetDynamicTooltip()
            };

            // Context Menu
            var menu = new System.Windows.Forms.ContextMenuStrip();
            
            // Version Header
            var version = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "1.0.0";
            var headerItem = new System.Windows.Forms.ToolStripMenuItem($"CostPulse v{version}");
            headerItem.Enabled = false; 
            menu.Items.Add(headerItem);
            menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

            menu.Items.Add("Open Widget", null, (s, e) => _mainViewModel.ShowWidgetCommand.Execute(null));
            menu.Items.Add("Add Entry", null, (s, e) => _mainViewModel.AddEntryCommand.Execute(null));
            menu.Items.Add("History", null, (s, e) => _mainViewModel.ShowHistoryCommand.Execute(null));
            menu.Items.Add("Analytics", null, (s, e) => _mainViewModel.OpenAnalyticsCommand.Execute(null));
            menu.Items.Add("Settings", null, (s, e) => _mainViewModel.OpenSettingsCommand.Execute(null));
            menu.Items.Add("Export CSV", null, (s, e) => ExportCsvSafe());
            menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            menu.Items.Add("Exit", null, (s, e) => ExitApp());

            _tray.ContextMenuStrip = menu;

            // Update Tooltip on Data Change
            DataService.DataChanged += () =>
            {
                Dispatcher.Invoke(() => 
                {
                    if (_tray != null) _tray.Text = GetDynamicTooltip();
                });
            };
            
            // 2. Basic Update Check (Fire and forget)
            System.Threading.Tasks.Task.Run(() => CheckForUpdates(version));
        }

        private async System.Threading.Tasks.Task CheckForUpdates(string currentVersion)
        {
            try
            {
                using var client = new System.Net.Http.HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                // Placeholder URL
                string json = await client.GetStringAsync("https://raw.githubusercontent.com/costpulse/app/main/version.json");
                
                // Simple parse - expect {"version": "1.2.1"}
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("version", out var vElement))
                {
                    string remoteVersion = vElement.GetString();
                    if (string.Compare(remoteVersion, currentVersion) > 0)
                    {
                        Dispatcher.Invoke(() => 
                        {
                            if (_tray != null)
                            {
                                _tray.ShowBalloonTip(5000, "Update Available", $"CostPulse v{remoteVersion} is available.", System.Windows.Forms.ToolTipIcon.Info);
                            }
                        });
                    }
                }
            }
            catch 
            {
                // Silent failure if offline or invalid URL
            }
        }

        private string GetDynamicTooltip()
        {
            if (DataService?.Data?.Settings == null) return "CostPulse";
            decimal today = DataService.GetTodayTotal();
            int tokens = DataService.GetTokensToday();
            string currency = DataService.Data.Settings.Currency;
            string text = $"CostPulse - Today: {currency}{today:F2} ({tokens:N0})";
            if (text.Length >= 64) text = text.Substring(0, 63);
            return text;
        }

        private void ExportCsvSafe()
        {
            try
            {
                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Export CostPulse CSV",
                    Filter = "CSV files (*.csv)|*.csv",
                    FileName = $"CostPulse_Export_{DateTime.Now:yyyyMMdd}.csv",
                    OverwritePrompt = true
                };

                if (dlg.ShowDialog() == true)
                {
                    DataService.ExportData(dlg.FileName);
                    MessageBox.Show("Export successful!", "CostPulse", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed:\n{ex.Message}", "CostPulse", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExitApp()
        {
            IsExiting = true;
            try { _tray?.Dispose(); } catch { }
            Services?.StopServices();
            _tray = null;
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Services?.Logger?.Log("App exiting.");
            try { _tray?.Dispose(); } catch { }
            Services?.StopServices();
            base.OnExit(e);
        }
    }
}
