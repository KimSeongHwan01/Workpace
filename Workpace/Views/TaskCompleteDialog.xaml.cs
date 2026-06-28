using System.Windows;
using Workpace.Models;
using Workpace.ViewModels;

namespace Workpace.Views
{
    public partial class TaskCompleteDialog : Window
    {
        public TaskCompleteDialog(string taskTitle)
        {
            InitializeComponent();
            DataContext = new TaskCompleteDialogViewModel(taskTitle);
        }

        // 외부에서 결과를 꺼낼 수 있도록 노출
        public TaskCompleteResult? Result =>
            (DataContext as TaskCompleteDialogViewModel)?.Result;
    }
}