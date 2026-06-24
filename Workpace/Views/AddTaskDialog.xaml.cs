using System.Windows;
using Workpace.Models;
using Workpace.ViewModels;

namespace Workpace.Views
{
    public partial class AddTaskDialog : Window
    {
        private readonly AddTaskDialogViewModel _viewModel;

        // projectId — 어떤 프로젝트에 Task를 추가할지 받아옴
        public AddTaskDialog(int projectId)
        {
            InitializeComponent();

            // ViewModel 생성 시 projectId와 현재 창(this) 전달
            _viewModel = new AddTaskDialogViewModel(projectId, this);
            DataContext = _viewModel;
        }

        // 외부(ProjectViewModel)에서 결과 꺼낼 수 있게
        public WorkTask? Result => _viewModel.Result;
    }
}