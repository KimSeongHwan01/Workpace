using System.Windows;

namespace Workpace.Views
{
    public partial class RetrospectDialog : Window
    {
        // 저장된 회고 내용 — 외부에서 읽어감
        public string ResultLearn { get; private set; } = "";
        public string ResultRegret { get; private set; } = "";
        public string ResultImprove { get; private set; } = "";

        public RetrospectDialog()
        {
            InitializeComponent();
        }

        // 저장 버튼 — 입력값 저장 후 닫기
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // 3개 필드 중 하나라도 비어있으면 저장 막기
            if (string.IsNullOrWhiteSpace(LearnBox.Text) ||
                string.IsNullOrWhiteSpace(RegretBox.Text) ||
                string.IsNullOrWhiteSpace(ImproveBox.Text))
            {
                MessageBox.Show("배운 점, 아쉬운 점, 개선할 점을 모두 입력해주세요.",
                    "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                return; // 저장 중단
            }

            ResultLearn = LearnBox.Text.Trim();
            ResultRegret = RegretBox.Text.Trim();
            ResultImprove = ImproveBox.Text.Trim();
            DialogResult = true;
            Close();
        }

        // 나중에 버튼 — 그냥 닫기
        private void LaterButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}