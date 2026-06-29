using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Workpace.Converters
{
    // 우선순위 텍스트 → 배지 배경색 변환
    // 높음: 빨강, 보통: 주황, 낮음: 초록
    public class PriorityToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() switch
            {
                "높음" => new SolidColorBrush(Color.FromRgb(254, 226, 226)), // 연한 빨강
                "보통" => new SolidColorBrush(Color.FromRgb(254, 243, 199)), // 연한 노랑
                "낮음" => new SolidColorBrush(Color.FromRgb(209, 250, 229)), // 연한 초록
                _ => new SolidColorBrush(Color.FromRgb(243, 244, 246))
            };
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    // 우선순위 텍스트 → 배지 글자색 변환
    public class PriorityToForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() switch
            {
                "높음" => new SolidColorBrush(Color.FromRgb(185, 28, 28)),  // 진한 빨강
                "보통" => new SolidColorBrush(Color.FromRgb(146, 64, 14)),  // 진한 주황
                "낮음" => new SolidColorBrush(Color.FromRgb(6, 95, 70)),    // 진한 초록
                _ => new SolidColorBrush(Color.FromRgb(107, 114, 128))
            };
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}