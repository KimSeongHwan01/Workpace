using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using Workpace.Models;

namespace Workpace.ViewModels
{
    public partial class AddTaskDialogViewModel : ObservableObject
    {
        private int _projectId;

        // 창을 닫기 위해 Window 참조 저장
        private readonly Window _window;

        // 확인 눌렀을 때 만들어진 Task
        // null이면 취소한 것
        public WorkTask? Result { get; private set; }

        [ObservableProperty]
        private string taskName = new WorkTask().Title;
        [ObservableProperty]
        private string description = new WorkTask().Description;
        [ObservableProperty]
        private string priority = new WorkTask().Priority;
        [ObservableProperty]
        private DateTime? duedate = new WorkTask().DueDate;
        [ObservableProperty]
        private string stage = new WorkTask().Stage;
        [ObservableProperty]
        private int progress = new WorkTask().Progress;

        public AddTaskDialogViewModel(int projectId, Window window)
        {
            _projectId = projectId;
            _window = window;
        }

        [RelayCommand]
        private void Confirm()
        {
            if (string.IsNullOrWhiteSpace(TaskName))
            {
                MessageBox.Show("작업 이름을 입력해주세요.", "입력 오류",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 입력값으로 WorkTask 객체 생성
            Result = new WorkTask
            {
                ProjectId = _projectId,
                Title = TaskName,
                Description = Description,
                Priority = Priority,
                DueDate = Duedate,
                Stage = Stage,
                Progress = Progress,
                Status = "할일" // 새 Task는 항상 할일에서 시작
            };

            // 확인 눌렀다고 알리고 창 닫기
            _window.DialogResult = true;
            _window.Close();
        }

        [RelayCommand]
        private void Cancel()
        {
            // 취소 눌렀다고 알리고 창 닫기
            _window.DialogResult = false;
            _window.Close();
        }
    }
}