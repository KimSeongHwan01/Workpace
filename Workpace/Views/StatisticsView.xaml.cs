using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using Workpace.ViewModels;

namespace Workpace.Views
{
    public partial class StatisticsView : UserControl
    {
        public StatisticsView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is StatisticsViewModel vm)
            {
                // PropertyChanged 기다리지 않고 바로 그림
                vm.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName == nameof(vm.MonthLabels)
                        || args.PropertyName == nameof(vm.HeatmapWidth))
                    {
                        Dispatcher.Invoke(() => DrawMonthHeader(vm));
                    }
                };

                // DataContext 세팅 시점에 이미 데이터가 있을 수 있으니 바로 호출
                if (vm.MonthLabels.Count > 0)
                    DrawMonthHeader(vm);
            }
        }

        // ───────────────────────────────────────
        // 월 헤더를 코드비하인드에서 직접 그림
        // Canvas 바인딩 문제를 우회하는 방법
        // ───────────────────────────────────────
        private void DrawMonthHeader(StatisticsViewModel vm)
        {
            MonthHeaderCanvas.Children.Clear();
            MonthHeaderCanvas.Width = vm.HeatmapWidth;

            foreach (var label in vm.MonthLabels)
            {
                var tb = new TextBlock
                {
                    Text = label.Text,
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175))
                };
                Canvas.SetLeft(tb, label.ColIndex * 14.0);
                Canvas.SetTop(tb, 0);
                MonthHeaderCanvas.Children.Add(tb);
            }
        }
    }
}