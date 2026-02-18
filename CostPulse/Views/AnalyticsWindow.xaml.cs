using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using CostPulse.ViewModels;

namespace CostPulse.Views
{
    public partial class AnalyticsWindow : Window
    {
        private AnalyticsViewModel _viewModel;

        public AnalyticsWindow()
        {
            InitializeComponent();
            _viewModel = new AnalyticsViewModel();
            this.DataContext = _viewModel;
            
            // Populate ComboBox
            ComboTimeRange.ItemsSource = Enum.GetValues(typeof(TimeRange));
            
            // Subscribe to VM changes
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            
            Loaded += AnalyticsWindow_Loaded;
            SizeChanged += AnalyticsWindow_SizeChanged;
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
             if (e.PropertyName == nameof(AnalyticsViewModel.MainChartData))
             {
                 DrawChart();
             }
        }

        private void AnalyticsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Initial draw
            DrawChart();
        }

        private void AnalyticsWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawChart();
        }

        private void DrawChart()
        {
            ChartCanvas.Children.Clear();
            
            if (_viewModel.MainChartData == null || !_viewModel.MainChartData.Any()) return;

            var data = _viewModel.MainChartData;
            double canvasWidth = ChartCanvas.ActualWidth;
            double canvasHeight = ChartCanvas.ActualHeight;

            if (canvasWidth == 0 || canvasHeight == 0) return;

            // Determine Max for scaling
            decimal maxVal = data.Max(d => d.Value);
            if (maxVal == 0) maxVal = 1;

            double barWidth = (canvasWidth / data.Count) * 0.6; 
            double gap = (canvasWidth / data.Count) * 0.4;

            for (int i = 0; i < data.Count; i++)
            {
                var point = data[i];
                double height = (double)(point.Value / maxVal) * canvasHeight;
                
                if (point.Value > 0 && height < 2) height = 2;

                var rect = new Rectangle
                {
                    Width = barWidth,
                    Height = height,
                    Fill = new SolidColorBrush(Color.FromRgb(68, 255, 68)), // #44FF44
                    Opacity = 0.8,
                    ToolTip = $"{point.Label}: ${point.Value:F4}"
                };

                double x = i * (barWidth + gap) + (gap / 2); // Center bar in slot
                double y = canvasHeight - height;

                Canvas.SetLeft(rect, x);
                Canvas.SetTop(rect, y);
                
                ChartCanvas.Children.Add(rect);
            }
        }
    }
}
