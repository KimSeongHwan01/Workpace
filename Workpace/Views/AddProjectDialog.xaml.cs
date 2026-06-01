using System.Windows;
using Workpace.Models;
using Workpace.ViewModels;

namespace Workpace.Views
{
    public partial class AddProjectDialog : Window
    {
        public AddProjectDialogViewModel ViewModel { get; }

        public AddProjectDialog()
        {
            InitializeComponent();

            // ViewModel 생성 시 this(현재 창)를 넘겨줌
            // ViewModel에서 창을 닫을 때 필요해서
            ViewModel = new AddProjectDialogViewModel(this);
            DataContext = ViewModel;
        }

        // 다이얼로그 결과 — MainViewModel에서 꺼내 쓸 수 있게
        public Project? Result => ViewModel.Result;
    }
}