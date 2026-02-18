using System.Windows;
using CostPulse.Services;
using CostPulse.Views; // For windows if we create them here
using System;

namespace CostPulse.ViewModels
{
    public class MainViewModel
    {
        private readonly DataService _dataService;

        public RelayCommand ShowWidgetCommand { get; }
        public RelayCommand ExitCommand { get; }
        public RelayCommand AddEntryCommand { get; }
        public RelayCommand ShowHistoryCommand { get; }
        public RelayCommand ExportCommand { get; }

        public RelayCommand OpenSettingsCommand { get; }

        public MainViewModel()
        {
            _dataService = App.DataService;

            ShowWidgetCommand = new RelayCommand(o => ToggleWidget());
            ExitCommand = new RelayCommand(o => Shutdown());
            AddEntryCommand = new RelayCommand(o => OpenAddEntry());
            ShowHistoryCommand = new RelayCommand(o => OpenHistory());
            // ExportCommand
            ExportCommand = new RelayCommand(o => ExportData());
            OpenSettingsCommand = new RelayCommand(o => OpenSettings());
            OpenAnalyticsCommand = new RelayCommand(o => OpenAnalytics());
        }

        public RelayCommand OpenAnalyticsCommand { get; }

        private void OpenAnalytics()
        {
            if (_dataService.LicenseService == null || !_dataService.LicenseService.IsPro)
            {
                MessageBox.Show("Analytics Dashboard is a Pro feature.\nPlease enter a valid license key in Settings.", "Pro Feature", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var w = new Views.AnalyticsWindow();
            w.Show();
        }

        private void ToggleWidget()
        {
            if (Application.Current.MainWindow != null)
            {
                if (Application.Current.MainWindow.Visibility == Visibility.Visible)
                    Application.Current.MainWindow.Hide();
                else
                {
                    Application.Current.MainWindow.Show();
                    Application.Current.MainWindow.Activate();
                }
            }
        }

        private void Shutdown()
        {
            App.IsExiting = true;
            Application.Current.Shutdown();
        }

        private void OpenAddEntry()
        {
            var w = new AddEntryWindow();
            w.Show();
        }

        private void ExportData()
        {
            try
            {
                // We can reuse the logic from HistoryWindow or move it to ViewModel
                // Since this is a simple app, let's just use SaveFileDialog here
                var dlg = new Microsoft.Win32.SaveFileDialog 
                { 
                    Filter = "CSV Files (*.csv)|*.csv", 
                    FileName = $"CostPulse_Export_{DateTime.Now:yyyyMMdd}.csv" 
                };
                
                if (dlg.ShowDialog() == true)
                {
                    _dataService.ExportData(dlg.FileName);
                    MessageBox.Show("Export successful!", "CostPulse", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export CSV: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // Ensure we don't crash
            }
        }

        // ... methods ...

        private void OpenHistory()
        {
             var w = new HistoryWindow();
             w.Show(); // Non-modal or Modal? Let's keep it non-modal so they can see widget updates
        }

        private void OpenSettings()
        {
            var w = new SettingsWindow();
            w.ShowDialog();
        }
    }
}
