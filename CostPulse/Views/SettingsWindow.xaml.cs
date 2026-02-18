using System;
using System.Collections.Generic;
using System.Linq; // Added for ToList()
using System.Windows;
using CostPulse.Models;

namespace CostPulse.Views
{
    public partial class SettingsWindow : Window
    {
        private List<string> _tempLogPaths = new List<string>();

        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
            LoadCloudStatus();
        }

        private void LoadCloudStatus()
        {
            if (App.CloudSync != null)
            {
                ChkCloudSync.IsChecked = App.CloudSync.IsSyncEnabled;
                UpdateSyncLabel();
            }
        }

        private void UpdateSyncLabel()
        {
             if (App.CloudSync?.LastSyncTime != null)
             {
                 TxtLastSync.Text = $"Last Sync: {App.CloudSync.LastSyncTime.Value:g}";
             }
             else
             {
                 TxtLastSync.Text = "Last Sync: Never";
             }
        }

        private async void BtnSyncNow_Click(object sender, RoutedEventArgs e)
        {
            if (App.CloudSync == null) return;
            
            BtnSyncNow.IsEnabled = false;
            TxtLastSync.Text = "Syncing...";
            
            // Force enable for the manual click if checked
            App.CloudSync.IsSyncEnabled = ChkCloudSync.IsChecked == true;

            bool success = await App.CloudSync.SyncAsync();
            
            if (success)
            {
                UpdateSyncLabel();
            }
            else
            {
                TxtLastSync.Text = "Sync Failed (Simulated)";
            }
            BtnSyncNow.IsEnabled = true;
        }

        private void LoadSettings()
        {
            var settings = App.DataService.Data.Settings;
            
            if (settings.DeveloperMode)
            {
                PortfolioBanner.Visibility = Visibility.Visible;
                TxtLicense.IsEnabled = false;
                TxtLicense.Text = "Portfolio Mode - License Validation Bypassed";
            }
            else
            {
                 PortfolioBanner.Visibility = Visibility.Collapsed;
                 TxtLicense.Text = settings.LicenseKey;
            }

            TxtBudget.Text = settings.DailyBudget.ToString();
            
            // Pricing
            if (settings.PricingModels != null)
            {
               var list = new List<PricingModel>();
               foreach(var p in settings.PricingModels)
               {
                   list.Add(new PricingModel { ModelName = p.ModelName, InputPricePerMillion = p.InputPricePerMillion, OutputPricePerMillion = p.OutputPricePerMillion });
               }
               GridPricing.ItemsSource = list;
            }

            // Logs (Pro Feature)
            bool isPro = App.DataService.LicenseService != null && App.DataService.LicenseService.IsPro;
            if (isPro)
            {
                ProGateOverlay.Visibility = Visibility.Collapsed;
                if (settings.LogWatchPaths != null)
                {
                    _tempLogPaths = new List<string>(settings.LogWatchPaths);
                }
                ListLogPaths.ItemsSource = _tempLogPaths;
            }
            else
            {
                ProGateOverlay.Visibility = Visibility.Visible;
                BtnAddFolder.IsEnabled = false; // Just in case, though overlay blocks clicks usually
                BtnRemoveFolder.IsEnabled = false;
            }
        }

        private void BtnAddFolder_Click(object sender, RoutedEventArgs e)
        {
            // Use WinForms dialog
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Select folder to watch for logs";
                dialog.UseDescriptionForTitle = true;
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string path = dialog.SelectedPath;
                    if (!_tempLogPaths.Contains(path))
                    {
                        _tempLogPaths.Add(path);
                        ListLogPaths.Items.Refresh(); // Refresh UI
                    }
                }
            }
        }

        private void BtnRemoveFolder_Click(object sender, RoutedEventArgs e)
        {
            if (ListLogPaths.SelectedItem is string path)
            {
                _tempLogPaths.Remove(path);
                ListLogPaths.Items.Refresh();
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(TxtBudget.Text, out decimal budget))
            {
                var settings = App.DataService.Data.Settings; 
                
                settings.DailyBudget = budget;
                settings.Currency = TxtCurrency.Text;
                settings.AlwaysOnTop = ChkAlwaysOnTop.IsChecked == true;
                settings.RunOnStartup = ChkRunOnStartup.IsChecked == true;
                settings.LicenseKey = TxtLicense.Text?.Trim();
                
                if (GridPricing.ItemsSource is List<PricingModel> details)
                {
                    settings.PricingModels = details;
                }

                if (App.DataService.LicenseService.IsPro)
                {
                    settings.LogWatchPaths = new List<string>(_tempLogPaths);
                }

                App.DataService.UpdateSettings(settings); // Validates license too
                
                // Update Cloud Sync
                if (App.CloudSync != null)
                {
                    App.CloudSync.IsSyncEnabled = ChkCloudSync.IsChecked == true;
                }

                // Handle Run On Startup Registry
                try
                {
                    string appName = "CostPulse";
                    string runKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
                    using (Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(runKey, true))
                    {
                        if (settings.RunOnStartup)
                        {
                            string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                            // Verify extension is .exe (could be .dll if loose)
                            if (exePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                            {
                                key.SetValue(appName, $"\"{exePath}\"");
                            }
                        }
                        else
                        {
                            if (key.GetValue(appName) != null)
                                key.DeleteValue(appName);
                        }
                    }
                }
                catch (Exception ex) 
                {
                    MessageBox.Show($"Failed to update startup settings: {ex.Message}", "Settings Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                // If license status changed, UI won't reflect immediately until re-opened, which is fine.
                // But we should verify if LogImportService needs restart.
                // App.LogImporter.UpdateWatchers() handles checking current settings.
                // We should call it.
                if (App.LogImporter != null)
                {
                    App.LogImporter.UpdateWatchers();
                }

                // Update widget TopMost
                foreach (Window w in Application.Current.Windows)
                {
                    if (w is MainWindow mw)
                    {
                        mw.Topmost = settings.AlwaysOnTop;
                    }
                }

                Close();
            }
            else
            {
                MessageBox.Show("Invalid Budget Value");
            }
        }
    }
}
