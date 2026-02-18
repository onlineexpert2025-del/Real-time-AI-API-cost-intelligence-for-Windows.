using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using System.Linq;

namespace CostPulse
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // Drag move on Header only
            var header = this.FindName("HeaderGrid") as System.Windows.Controls.Grid;
            if (header != null)
            {
                header.MouseLeftButtonDown += (s, e) =>
                {
                    if (e.ButtonState == MouseButtonState.Pressed)
                        this.DragMove();
                };
            }

            // Hover effect on MainBorder
            var mainBorder = this.FindName("MainBorder") as System.Windows.Controls.Border;
            if (mainBorder != null)
            {
                mainBorder.MouseEnter += (s, e) => { mainBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)); };
                mainBorder.MouseLeave += (s, e) => { mainBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(51, 255, 255, 255)); }; // #33FFFFFF is approx 51 alpha
            }

            Loaded += MainWindow_Loaded;
            // Closing += MainWindow_Closing; // Removed
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!App.IsExiting)
            {
                e.Cancel = true;
                this.Hide();
            }
            base.OnClosing(e);
        }

        // Removed old event handler to avoid duplicate logic or confusion
        // private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e) ... replaced by override

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateUI();
            if (App.DataService != null)
            {
                App.DataService.DataChanged += UpdateUI;
            }
        }

        private decimal _lastTodayTotal = 0;
        private bool _isFirstLoad = true;

        private void UpdateUI()
        {
            if (App.DataService == null) return;

            decimal today = App.DataService.GetTodayTotal();
            decimal month = App.DataService.GetMonthTotal();
            int tokens = App.DataService.GetTokensToday();
            decimal budget = App.DataService.Data.Settings.DailyBudget;
            string currency = App.DataService.Data.Settings.Currency;

            Color targetColor = Colors.LightGreen;
            if (today >= budget) targetColor = Colors.Red;
            else if (today >= budget * 0.8m) targetColor = Colors.Yellow;

            if (_isFirstLoad)
            {
                _lastTodayTotal = today;
                _lastTodayTotal = today;
                TxtTodayTotal.Text = today < 1.0m ? $"{currency}{today:F4}" : $"{currency}{today:F2}";
                TxtTodayTotal.Foreground = new SolidColorBrush(targetColor);
                _isFirstLoad = false;
            }
            else
            {
                // Animate Value Update if changed
                if (today != _lastTodayTotal)
                {
                    AnimateValueChange(today, currency);
                    UpdateDelta(today, _lastTodayTotal);
                    _lastTodayTotal = today;
                }
                
                AnimateTextColor(targetColor);
            }

            TxtMonthTotal.Text = $"Month: {currency}{month:F2}";
            TxtTokens.Text = $"{tokens:N0} tokens";

            RenderSparkline();
        }

        private void AnimateValueChange(decimal newValue, string currency)
        {
            var fadeOut = new System.Windows.Media.Animation.DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150));
            fadeOut.Completed += (s, e) =>
            {
                TxtTodayTotal.Text = newValue < 1.0m ? $"{currency}{newValue:F4}" : $"{currency}{newValue:F2}";
                var fadeIn = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150));
                TxtTodayTotal.BeginAnimation(TextBlock.OpacityProperty, fadeIn);
            };
            TxtTodayTotal.BeginAnimation(TextBlock.OpacityProperty, fadeOut);
        }

        private void AnimateTextColor(Color targetColor)
        {
            var brush = TxtTodayTotal.Foreground as SolidColorBrush;
            if (brush == null || brush.IsFrozen)
            {
                brush = new SolidColorBrush(targetColor);
                TxtTodayTotal.Foreground = brush;
                return;
            }

            if (brush.Color == targetColor) return;

            var anim = new System.Windows.Media.Animation.ColorAnimation(targetColor, TimeSpan.FromMilliseconds(200));
            brush.BeginAnimation(SolidColorBrush.ColorProperty, anim);
        }

        private void UpdateDelta(decimal current, decimal previous)
        {
            if (current == previous)
            {
                TxtDelta.Visibility = Visibility.Collapsed;
                return;
            }

            TxtDelta.Visibility = Visibility.Visible;
            if (current > previous)
            {
                TxtDelta.Text = "▲";
                TxtDelta.Foreground = new SolidColorBrush(Colors.LightGreen);
            }
            else
            {
                TxtDelta.Text = "▼";
                TxtDelta.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        private void RenderSparkline()
        {
            SparkCanvas.Children.Clear();

            // Real Data
            var totals = App.DataService.GetLast7DaysTotals(); 

            if (totals == null || totals.Count < 2) return;

            double max = (double)totals.Max();
            double min = (double)totals.Min();
            
            double width = SparkCanvas.Width;
            double height = SparkCanvas.Height;
            if (double.IsNaN(width)) width = 210; // Fallback
            if (double.IsNaN(height)) height = 30; // Fallback

            var polyline = new System.Windows.Shapes.Polyline
            {
                Stroke = new SolidColorBrush(Color.FromRgb(102, 136, 255)), // #6688FF
                StrokeThickness = 1.5
            };

            var points = new PointCollection();
            double step = width / (totals.Count - 1);
            double range = max - min;
            double padding = 3; // 3-4px padding
            double effectiveHeight = height - (2 * padding);

            // 1. Draw Faint Baseline
            var baseline = new System.Windows.Shapes.Line
            {
                X1 = 0,
                Y1 = height - padding,
                X2 = width,
                Y2 = height - padding,
                Stroke = new SolidColorBrush(Colors.White),
                Opacity = 0.1,
                StrokeThickness = 1
            };
            SparkCanvas.Children.Add(baseline);

            // 2. Calculate Points & Add Markers
            for (int i = 0; i < totals.Count; i++)
            {
                double x = i * step;
                double val = (double)totals[i];
                double y;

                if (range == 0)
                {
                    y = height / 2;
                }
                else
                {
                    // Normalize (0..1)
                    double normalized = (val - min) / range;
                    // Invert Y (0 is top) and apply padding
                    y = (height - padding) - (normalized * effectiveHeight);
                }
                points.Add(new Point(x, y));

                // Add Marker (Circle)
                var marker = new System.Windows.Shapes.Ellipse
                {
                    Width = 4,
                    Height = 4,
                    Fill = new SolidColorBrush(Color.FromRgb(102, 136, 255)), // Same as line
                    Stroke = new SolidColorBrush(Colors.Black), // Contrast
                    StrokeThickness = 0.5
                };
                Canvas.SetLeft(marker, x - 2); // Center horizontally
                Canvas.SetTop(marker, y - 2);  // Center vertically
                SparkCanvas.Children.Add(marker);
            }

            // 3. Add Polyline (added last to be on top of baseline, but markers are added in loop so markers are on top of polyline if we add polyline first? No, explicit order matter)
            // Actually, usually line is below markers.
            // Let's add Polyline BEFORE the markers loop? No, implementation convenience suggests adding it to canvas index or just insert.
            // Simpler: Add baseline -> Polyline -> Markers.
            
            polyline.Points = points;
            
            // Insert Polyline at index 1 (after baseline, before markers)
            if (SparkCanvas.Children.Count > 0)
                SparkCanvas.Children.Insert(1, polyline);
            else
                SparkCanvas.Children.Add(polyline);
        }
    }
}