using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Workpace.Converters
{
    // IsCore(bool) → 자물쇠 색상 변환
    // true  → 보라색 (핵심 기능 강조)
    // false → 회색 (일반 기능)
    public class BoolToLockColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is true
                ? new SolidColorBrush(Color.FromRgb(124, 58, 237))  // #7C3AED
                : new SolidColorBrush(Color.FromRgb(209, 213, 219)); // #D1D5DB

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}