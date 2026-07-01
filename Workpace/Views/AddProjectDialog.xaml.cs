using System.Windows;
using System.Windows.Controls;
using Workpace.Models;
using Workpace.ViewModels;

namespace Workpace.Views
{
    public partial class AddProjectDialog : Window
    {
        public AddProjectDialogViewModel ViewModel { get; }

        // 새 프로젝트 추가 — 빈 다이얼로그
        public AddProjectDialog()
        {
            InitializeComponent();

            // ViewModel 생성 시 this(현재 창)를 넘겨줌
            // ViewModel에서 창을 닫을 때 필요해서
            ViewModel = new AddProjectDialogViewModel(this);
            DataContext = ViewModel;
        }

        // 프로젝트 수정 — 기존 값이 채워진 다이얼로그
        // (편집 버튼 클릭 시 MainViewModel.EditProject에서 호출)
        public AddProjectDialog(Project projectToEdit)
        {
            InitializeComponent();
            ViewModel = new AddProjectDialogViewModel(this, projectToEdit);
            DataContext = ViewModel;

            // 수정 모드 — 창 제목 + 화면 안 문구들도 같이 바꿔줌
            Title = "프로젝트 수정";
            HeaderTitleText.Text = "프로젝트 수정";
            HeaderSubtitleText.Text = "프로젝트 정보를 수정하세요.";

            // 수정 모드 버튼 내용 교체 — StackPanel 안 TextBlock을 찾아서 텍스트 변경
            if (ConfirmButton.Content is StackPanel sp)
            {
                var tb = sp.Children.OfType<TextBlock>().FirstOrDefault();
                if (tb != null) tb.Text = "수정 완료";
            }
        }

        // 다이얼로그 결과 — MainViewModel에서 꺼내 쓸 수 있게
        public Project? Result => ViewModel.Result;
    }
}