using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using Workpace.Models;

namespace Workpace.ViewModels
{
    public partial class AddTaskDialogViewModel : ObservableObject
    {
        private int _projectId;

        [ObservableProperty]
        private string taskName = new WorkTask().Title;

        [ObservableProperty]
        private string priority = new WorkTask().Priority;

        [ObservableProperty]
        private DateTime? duedate = new WorkTask().DueDate;

        [ObservableProperty]
        private string stage = new WorkTask().Stage;
        
        [ObservableProperty]
        private int progress = new WorkTask().Progress;

        public AddTaskDialogViewModel(int projectId)
        {
            _projectId = projectId;
        }

        [RelayCommand]
        private void Confirm()
        {
            if(string.IsNullOrWhiteSpace(TaskName))
            {
                MessageBox.Show("작업 이름을 입력해주세요.", "입력 오류",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        [RelayCommand]
        private void Cancel()
        {

        }
    }
}
