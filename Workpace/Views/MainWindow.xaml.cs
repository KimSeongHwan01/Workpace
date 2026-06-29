using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Workpace.Models;
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

        // ───────────────────────────────────────
        // 사이드바 프로젝트 재클릭 감지
        // 같은 프로젝트를 다시 클릭하면 프로젝트 화면으로 돌아옴
        // ───────────────────────────────────────
        private void ProjectListBox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox?.SelectedItem is not Project project) return;

            var vm = DataContext as MainViewModel;
            if (vm == null) return;

            // 핵심 수정 — CurrentView가 이미 ProjectView인지 확인
            // 새 프로젝트 클릭 시: OnSelectedProjectChanged가 이미 처리했으므로 건드리지 않음
            // 같은 프로젝트 재클릭 시: CurrentView가 ProjectView가 아닐 수 있으므로 복귀 처리
            if (vm.CurrentView is not Workpace.Views.ProjectView)
            {
                vm.OnProjectReselected(project);
            }
        }
    }
}