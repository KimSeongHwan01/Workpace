using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using Workpace.Messages;
using Workpace.Models;
using Workpace.Services;

namespace Workpace.ViewModels
{
    // IRecipient<T> — "나는 이 메시지를 받을 수 있어" 라고 선언하는 인터페이스
    // ProjectSelectedMessage를 수신하겠다는 뜻
    public partial class ProjectViewModel : ObservableObject, IRecipient<ProjectSelectedMessage>
    {
        private readonly DatabaseService _db;

        // 현재 선택된 프로젝트 — null이면 아무것도 표시 안 함
        [ObservableProperty]
        private Project? currentProject;

        // 칸반 보드 3개 컬렉션
        // Status 값으로 분류: "할일" / "진행중" / "완료"
        [ObservableProperty]
        private ObservableCollection<WorkTask> todoTasks = new();

        [ObservableProperty]
        private ObservableCollection<WorkTask> inProgressTasks = new();

        [ObservableProperty]
        private ObservableCollection<WorkTask> doneTasks = new();

        public ProjectViewModel()
        {
            _db = new DatabaseService();

            // 메시지 수신 등록
            // 이 줄이 없으면 메시지를 보내도 여기서 못 받음
            WeakReferenceMessenger.Default.Register(this);
        }

        // ───────────────────────────────────────
        // IRecipient<ProjectSelectedMessage> 구현
        // MainViewModel이 메시지를 보내면 이 메서드가 자동으로 호출됨
        // ───────────────────────────────────────
        public void Receive(ProjectSelectedMessage message)
        {
            CurrentProject = message.Value;

            // 프로젝트가 선택 해제되면 칸반 보드 비우기
            if (CurrentProject == null)
            {
                ClearBoard();
                return;
            }

            LoadTasks(CurrentProject.Id);
        }

        // ───────────────────────────────────────
        // DB에서 Task 불러와서 3개 컬렉션으로 분류
        // ───────────────────────────────────────
        private void LoadTasks(int projectId)
        {
            ClearBoard();

            var tasks = _db.GetTasksByProject(projectId);

            foreach (var task in tasks)
            {
                switch (task.Status)
                {
                    case "할일":
                        TodoTasks.Add(task);
                        break;
                    case "진행중":
                        InProgressTasks.Add(task);
                        break;
                    case "완료":
                        DoneTasks.Add(task);
                        break;
                }
            }
        }

        // ───────────────────────────────────────
        // 3개 컬렉션 전부 초기화
        // 프로젝트 전환 시 이전 데이터 남지 않게 비워줌
        // ───────────────────────────────────────
        private void ClearBoard()
        {
            TodoTasks.Clear();
            InProgressTasks.Clear();
            DoneTasks.Clear();
        }
    }
}