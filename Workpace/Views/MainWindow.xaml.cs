using System.Windows;
using Workpace.ViewModels;

namespace Workpace.Views
{
    public partial class MainWindow : Window
    {
        // ProjectViewModel을 프로퍼티로 선언
        // XAML에서 이 프로퍼티에 접근해서 칸반 보드 영역 DataContext로 연결
        public ProjectViewModel ProjectVM { get; }

        public MainWindow()
        {
            InitializeComponent();

            // ProjectViewModel 먼저 생성 — 메시지 수신 대기 시작
            ProjectVM = new ProjectViewModel();

            // MainViewModel 생성 시 ProjectViewModel 전달
            DataContext = new MainViewModel(ProjectVM);
        }
    }
}