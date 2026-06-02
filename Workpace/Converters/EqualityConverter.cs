using System.Globalization;
using System.Windows.Data;

namespace Workpace.Converters
{
    // RadioButton의 IsChecked와 SelectedType을 연결해주는 변환기
    // ConverterParameter와 현재 값이 같으면 true(선택됨) 반환
    public class EqualityConverter : IValueConverter
    {
        // View → ViewModel: 현재 값이 파라미터와 같은지 확인
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() == parameter?.ToString();
        }

        // ViewModel → View: 선택됐으면 파라미터 값을 SelectedType에 저장
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? parameter : Binding.DoNothing;
        }
    }
}