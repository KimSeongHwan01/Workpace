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

            DataContext = new { Icon = icon, Title = title, Message = message };

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