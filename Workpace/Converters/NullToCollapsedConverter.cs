using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Workpace.Converters
{
    // null이면 Collapsed, 값 있으면 Visible
    // DueDate(DateTime?)처럼 nullable 타입의 Visibility 제어용
    public class NullToCollapsedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value == null ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}