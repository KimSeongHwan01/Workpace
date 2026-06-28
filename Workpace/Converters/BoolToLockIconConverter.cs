using System.Globalization;
using System.Windows.Data;

namespace Workpace.Converters
{
    // IsCore(bool) → 자물쇠 아이콘 텍스트 변환
    // true  → 🔒 (핵심 기능으로 지정됨)
    // false → 🔓 (일반 기능)
    public class BoolToLockIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is true ? "🔒" : "🔓";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}