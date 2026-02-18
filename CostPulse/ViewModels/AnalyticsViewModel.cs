using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using CostPulse.Models;
using CostPulse.Services;

namespace CostPulse.ViewModels
{
    public class AnalyticsViewModel : INotifyPropertyChanged
    {
        private readonly DataService _dataService;
        private TimeRange _selectedTimeRange = TimeRange.Last30Days;
        
        // Summary
        private decimal _totalCost;
        private int _totalTokens;
        private int _totalRequests;

        // Charts
        private List<ChartDataPoint> _mainChartData;
        private List<ModelCostPoint> _topModels;
        private List<ProviderCostPoint> _topProviders;

        public event PropertyChangedEventHandler PropertyChanged;

        public AnalyticsViewModel()
        {
            _dataService = App.DataService;
            LoadData();
        }

        public TimeRange SelectedTimeRange
        {
            get => _selectedTimeRange;
            set
            {
                if (_selectedTimeRange != value)
                {
                    _selectedTimeRange = value;
                    OnPropertyChanged();
                    LoadData();
                }
            }
        }

        public decimal TotalCost
        {
            get => _totalCost;
            set { _totalCost = value; OnPropertyChanged(); }
        }

        public int TotalTokens
        {
            get => _totalTokens;
            set { _totalTokens = value; OnPropertyChanged(); }
        }

        public int TotalRequests
        {
            get => _totalRequests;
            set { _totalRequests = value; OnPropertyChanged(); }
        }

        public List<ChartDataPoint> MainChartData
        {
            get => _mainChartData;
            set { _mainChartData = value; OnPropertyChanged(); }
        }

        public List<ModelCostPoint> TopModels
        {
            get => _topModels;
            set { _topModels = value; OnPropertyChanged(); }
        }

        public List<ProviderCostPoint> TopProviders
        {
            get => _topProviders;
            set { _topProviders = value; OnPropertyChanged(); }
        }

        public void Refresh()
        {
            LoadData();
        }

        private void LoadData()
        {
            if (_dataService?.Data?.Entries == null) return;

            var allEntries = _dataService.Data.Entries;
            List<UsageEntry> filteredEntries;
            DateTime now = DateTime.Now;

            // 1. Filter
            switch (SelectedTimeRange)
            {
                case TimeRange.Today:
                    filteredEntries = allEntries.Where(e => e.Timestamp.Date == now.Date).ToList();
                    break;
                case TimeRange.Last7Days:
                    filteredEntries = allEntries.Where(e => e.Timestamp >= now.AddDays(-7)).ToList();
                    break;
                case TimeRange.Last30Days:
                    filteredEntries = allEntries.Where(e => e.Timestamp >= now.AddDays(-30)).ToList();
                    break;
                case TimeRange.AllTime:
                default:
                    filteredEntries = allEntries.ToList();
                    break;
            }

            // 2. Summary Stats
            TotalCost = filteredEntries.Sum(e => e.Cost);
            TotalTokens = filteredEntries.Sum(e => e.InputTokens + e.OutputTokens);
            TotalRequests = filteredEntries.Count;

            // 3. Breakdowns
            TopModels = filteredEntries
                .GroupBy(e => e.ModelName)
                .Select(g => new ModelCostPoint { Name = g.Key, Cost = g.Sum(e => e.Cost), Count = g.Count() })
                .OrderByDescending(x => x.Cost)
                .Take(5)
                .ToList();

            TopProviders = filteredEntries
                .GroupBy(e => e.Provider)
                .Select(g => new ProviderCostPoint { Name = g.Key, Cost = g.Sum(e => e.Cost) })
                .OrderByDescending(x => x.Cost)
                .ToList();

            // 4. Main Chart Data
            // Strategy: 
            // - Today: Hourly
            // - 7d/30d: Daily
            // - AllTime: Monthly
            MainChartData = GenerateChartData(filteredEntries, SelectedTimeRange);
        }

        private List<ChartDataPoint> GenerateChartData(List<UsageEntry> entries, TimeRange range)
        {
            var points = new List<ChartDataPoint>();
            DateTime now = DateTime.Now;

            if (range == TimeRange.Today)
            {
                // Hourly (0-23)
                for (int i = 0; i < 24; i++)
                {
                    // Only show up to current hour? Or full day? Full day is fine (mostly empty)
                    // Let's filter to only hours that have data or up to now?
                    // Fixed 24h buckets for today
                    var hourStart = now.Date.AddHours(i);
                    decimal cost = entries.Where(e => e.Timestamp.Date == now.Date && e.Timestamp.Hour == i).Sum(e => e.Cost);
                    points.Add(new ChartDataPoint { Label = $"{i}:00", Value = cost, SortDate = hourStart });
                }
            }
            else if (range == TimeRange.AllTime)
            {
                // Monthly
                // Find min date
                if (!entries.Any()) return points;
                var minDate = entries.Min(e => e.Timestamp);
                var maxDate = now;
                
                var current = new DateTime(minDate.Year, minDate.Month, 1);
                while (current <= maxDate)
                {
                    decimal cost = entries.Where(e => e.Timestamp.Year == current.Year && e.Timestamp.Month == current.Month).Sum(e => e.Cost);
                    points.Add(new ChartDataPoint { Label = current.ToString("MMM yy"), Value = cost, SortDate = current });
                    current = current.AddMonths(1);
                }
            }
            else
            {
                // Daily (7 or 30)
                int days = range == TimeRange.Last7Days ? 7 : 30;
                for (int i = days - 1; i >= 0; i--)
                {
                    var date = now.Date.AddDays(-i);
                    decimal cost = entries.Where(e => e.Timestamp.Date == date).Sum(e => e.Cost);
                    points.Add(new ChartDataPoint { Label = date.ToString("MMM dd"), Value = cost, SortDate = date });
                }
            }

            return points;
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public enum TimeRange
    {
        Today,
        Last7Days,
        Last30Days,
        AllTime
    }

    public class ChartDataPoint
    {
        public string Label { get; set; }
        public decimal Value { get; set; }
        public DateTime SortDate { get; set; }
    }

    public class ModelCostPoint
    {
        public string Name { get; set; }
        public decimal Cost { get; set; }
        public int Count { get; set; }
    }

    public class ProviderCostPoint
    {
        public string Name { get; set; }
        public decimal Cost { get; set; }
    }
}
