using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Workpace.Converters
{
    // bool(HasActivity) → 히트맵 색상
    public class BoolToHeatmapColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is true
                ? Color.FromRgb(124, 58, 237)   // #7C3AED 보라색
                : Color.FromRgb(237, 233, 254);  // #EDE9FE 연한 보라

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    // Col(int) → X 픽셀 위치 (14px 간격)
    public class ColToPixelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is int col ? col * 14.0 : 0.0;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    // Row(int) → Y 픽셀 위치 (14px 간격)
    public class RowToPixelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is int row ? row * 14.0 : 0.0;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    // double → Thickness (왼쪽 마진만 설정)
    public class DoubleToLeftMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is double d ? new Thickness(d, 0, 0, 4) : new Thickness(0);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}