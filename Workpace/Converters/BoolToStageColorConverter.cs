using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Workpace.Converters
{
    // 단계 완료 여부 → 원 색상 변환
    // true(완료) → 보라색, false(미완료) → 회색
    public class BoolToStageColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value
                ? new SolidColorBrush(Color.FromRgb(124, 58, 237))  // #7C3AED 보라
                : new SolidColorBrush(Color.FromRgb(229, 231, 235)); // #E5E7EB 회색
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    // 단계 완료 여부 → 텍스트 색상 변환
    // true(완료) → 진한 색, false(미완료) → 회색
    public class BoolToStageForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value
                ? new SolidColorBrush(Color.FromRgb(31, 31, 31))    // #1F1F1F 진한색
                : new SolidColorBrush(Color.FromRgb(156, 163, 175)); // #9CA3AF 회색
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}