using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CostPulse.Models;
using Microsoft.Win32;

namespace CostPulse.Views
{
    public partial class HistoryWindow : Window
    {
        public HistoryWindow()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            // Simple load all. Filtering can be done in memory.
            string filter = TxtFilter.Text.ToLower();
            var entries = App.DataService.Data.Entries.OrderByDescending(e => e.Timestamp).ToList();

            if (!string.IsNullOrWhiteSpace(filter))
            {
                entries = entries.Where(e => 
                    e.ModelName.ToLower().Contains(filter) || 
                    e.Provider.ToLower().Contains(filter) ||
                    (e.Label != null && e.Label.ToLower().Contains(filter))
                ).ToList();
            }

            GridHistory.ItemsSource = entries;
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is UsageEntry entry)
            {
                if (MessageBox.Show("Delete this entry?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    App.DataService.DeleteEntry(entry);
                    LoadData();
                }
            }
        }

        private void BtnClearAll_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to delete ALL history?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                App.DataService.ClearHistory();
                LoadData();
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog { Filter = "CSV Files (*.csv)|*.csv", FileName = $"CostPulse_Export_{DateTime.Now:yyyyMMdd}.csv" };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    App.DataService.ExportData(dlg.FileName);
                    MessageBox.Show("Export Successful");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export Failed: {ex.Message}");
                }
            }
        }
    }
}
