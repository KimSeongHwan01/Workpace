using System.Windows;
using System.Windows.Threading;

namespace Workpace.Views
{
    public partial class CustomToast : Window
    {
        // 자동으로 닫히는 타이머
        private readonly DispatcherTimer _autoCloseTimer;

        public CustomToast(string icon, string title, string message, int displaySeconds = 10)
        {
            InitializeComponent();

            // App.xaml에 등록된 Geometry 리소스를 키 이름으로 꺼내옴
            // NotificationService에서 "IconFlame" 같은 키 문자열을 넘겨주면 여기서 실제 Path로 변환
            var geometry = Application.Current.TryFindResource(icon) as System.Windows.Media.Geometry
                           ?? Application.Current.TryFindResource("IconBell") as System.Windows.Media.Geometry;

            DataContext = new { IconGeometry = geometry, Title = title, Message = message };

            // 자동 닫힘 타이머
            _autoCloseTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(displaySeconds)
            };
            _autoCloseTimer.Tick += (s, e) =>
            {
                _autoCloseTimer.Stop();
                Close();
            };
            _autoCloseTimer.Start();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _autoCloseTimer.Stop();
            Close();
        }
    }
}