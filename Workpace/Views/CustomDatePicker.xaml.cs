using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Workpace.Views
{
    public partial class CustomDatePicker : UserControl
    {
        // SelectedDate DependencyProperty — 외부 ViewModel과 TwoWay 바인딩 지원
        public static readonly DependencyProperty SelectedDateProperty =
            DependencyProperty.Register(
                nameof(SelectedDate),
                typeof(DateTime?),
                typeof(CustomDatePicker),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSelectedDateChanged));

        public DateTime? SelectedDate
        {
            get => (DateTime?)GetValue(SelectedDateProperty);
            set => SetValue(SelectedDateProperty, value);
        }

        private static void OnSelectedDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (CustomDatePicker)d;
            ctrl.UpdateDisplay();
        }

        public CustomDatePicker()
        {
            InitializeComponent();
        }

        // Border 클릭 시 팝업 토글
        private void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            CalendarPopup.IsOpen = !CalendarPopup.IsOpen;
        }

        // 달력에서 날짜 선택 시
        private void Cal_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Cal.SelectedDate.HasValue)
            {
                SelectedDate = Cal.SelectedDate.Value;
                CalendarPopup.IsOpen = false;
            }
        }

        // 날짜 표시 갱신
        private void UpdateDisplay()
        {
            if (SelectedDate.HasValue)
            {
                DateDisplay.Text = SelectedDate.Value.ToString("yyyy-MM-dd");
                DateDisplay.Visibility = Visibility.Visible;
                Placeholder.Visibility = Visibility.Collapsed;

                // 달력도 같은 날짜로 동기화
                Cal.SelectedDate = SelectedDate.Value;
                Cal.DisplayDate = SelectedDate.Value;
            }
            else
            {
                DateDisplay.Visibility = Visibility.Collapsed;
                Placeholder.Visibility = Visibility.Visible;
                Cal.SelectedDate = null;
            }
        }
    }
}