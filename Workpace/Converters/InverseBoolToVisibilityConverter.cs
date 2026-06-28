using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Workpace.Converters
{
    // ───────────────────────────────────────
    // BoolToVisibilityConverter의 반전 버전
    // true → Collapsed, false → Visible
    // 보기 모드(IsEditMode=false)일 때 표시할 요소에 사용
    // ───────────────────────────────────────
    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? Visibility.Collapsed : Visibility.Visible;
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}