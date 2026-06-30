using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Workpace.Converters
{
    // 문자열이 비어있지 않으면 Visible, 비어있으면 Collapsed
    // Task 설명이 없을 때 "설명" 섹션 자체를 안 보이게 하기 위해 사용
    public class StringNotEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value as string;
            return string.IsNullOrWhiteSpace(text) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}