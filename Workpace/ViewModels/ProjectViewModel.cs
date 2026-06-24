using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using Workpace.Messages;
using Workpace.Models;
using Workpace.Services;
using Workpace.Views;

namespace Workpace.ViewModels
{
    public partial class ProjectViewModel : ObservableObject, IRecipient<ProjectSelectedMessage>
    {
        private readonly DatabaseService _db;

        // 현재 선택된 프로젝트
        [ObservableProperty]
        private Project? currentProject;

        // ── 헤더 영역 ──────────────────────────────
        // "D-23일" 형태의 텍스트
        [ObservableProperty]
        private string dDayText = "";

        // ── 진행률 영역 ────────────────────────────
        // 현재 진행률 숫자 (0~100)
        [ObservableProperty]
        private double currentProgress;

        // 목표 진행률 숫자 (0~100)
        [ObservableProperty]
        private double targetProgress;

        // "68%" 형태의 텍스트
        [ObservableProperty]
        private string currentProgressText = "";

        // "78%" 형태의 텍스트
        [ObservableProperty]
        private string targetProgressText = "";

        // 페이스 경고 메시지 텍스트
        [ObservableProperty]
        private string paceWarningText = "";

        // 경고 표시 여부 — true면 경고 메시지 보임
        [ObservableProperty]
        private bool isBehindSchedule;

        // ── 칸반 보드 ──────────────────────────────
        [ObservableProperty]
        private ObservableCollection<WorkTask> todoTasks = new();

        [ObservableProperty]
        private ObservableCollection<WorkTask> inProgressTasks = new();

        [ObservableProperty]
        private ObservableCollection<WorkTask> doneTasks = new();

        // 현재 선택된 탭 (전체/기획/설계/개발/테스트/완료)
        [ObservableProperty]
        private string selectedTab = "전체";

        // DB에서 불러온 전체 Task 목록 — 탭 필터링할 때 원본으로 사용
        private List<WorkTask> _allTasks = new();

        public ProjectViewModel()
        {
            _db = new DatabaseService();
            WeakReferenceMessenger.Default.Register(this);
        }

        // ───────────────────────────────────────
        // 메시지 수신 — 프로젝트 선택 시 전체 화면 갱신
        // ───────────────────────────────────────
        public void Receive(ProjectSelectedMessage message)
        {
            CurrentProject = message.Value;

            if (CurrentProject == null)
            {
                ClearAll();
                return;
            }

            // 순서대로 계산 — 모든 프로퍼티가 채워지면 화면 전체 갱신
            CalculateDDay();
            CalculateProgress();
            LoadTasks(CurrentProject.Id);
        }

        // ───────────────────────────────────────
        // D-day 계산
        // 마감일 - 오늘 = 남은 일수
        // ───────────────────────────────────────
        private void CalculateDDay()
        {
            if (CurrentProject == null) return;

            var daysLeft = (CurrentProject.Deadline - DateTime.Today).Days;

            if (daysLeft > 0)
                DDayText = $"D-{daysLeft}일";
            else if (daysLeft == 0)
                DDayText = "D-day";
            else
                DDayText = $"D+{Math.Abs(daysLeft)}일";
        }

        // ───────────────────────────────────────
        // 진행률 계산
        // 현재 진행률 = 완료Task / 전체Task × 100
        // 목표 진행률 = 경과일 / 전체기간 × 100
        // ───────────────────────────────────────
        private void CalculateProgress()
        {
            if (CurrentProject == null) return;

            // 전체 Task, 완료 Task 가져오기
            var allTasks = _db.GetTasksByProject(CurrentProject.Id);
            var totalCount = allTasks.Count;
            var doneCount = allTasks.Count(t => t.Status == "완료");

            // 현재 진행률 계산
            CurrentProgress = totalCount > 0
                ? Math.Round((double)doneCount / totalCount * 100, 1)
                : 0;
            CurrentProgressText = $"{CurrentProgress}%";

            // 목표 진행률 계산
            var totalDays = (CurrentProject.Deadline - CurrentProject.StartDate).Days;
            var elapsedDays = (DateTime.Today - CurrentProject.StartDate).Days;
            TargetProgress = totalDays > 0
                ? Math.Round((double)elapsedDays / totalDays * 100, 1)
                : 0;
            // 목표 진행률은 100% 초과 안 되게 제한
            TargetProgress = Math.Min(TargetProgress, 100);
            TargetProgressText = $"{TargetProgress}%";

            // 페이스 경고 — 현재 진행률이 목표보다 낮으면 경고
            IsBehindSchedule = CurrentProgress < TargetProgress;
            if (IsBehindSchedule)
            {
                var diff = Math.Round(TargetProgress - CurrentProgress, 1);
                PaceWarningText = $"⚠ 현재 속도 마감 {diff}% 초과";
            }
            else
            {
                PaceWarningText = "";
            }
        }

        // ───────────────────────────────────────
        // DB에서 Task 불러와서 칸반 보드 분류
        // ───────────────────────────────────────
        private void LoadTasks(int projectId)
        {
            _allTasks = _db.GetTasksByProject(projectId);
            ApplyFilter();
        }

        // ───────────────────────────────────────
        // 탭 필터 적용
        // SelectedTab이 "전체"면 전부 표시
        // 그 외엔 Stage가 일치하는 것만 표시
        // ───────────────────────────────────────
        private void ApplyFilter()
        {
            var filtered = SelectedTab == "전체"
                ? _allTasks
                : _allTasks.Where(t => t.Stage == SelectedTab).ToList();

            TodoTasks.Clear();
            InProgressTasks.Clear();
            DoneTasks.Clear();

            foreach (var task in filtered)
            {
                switch (task.Status)
                {
                    case "할일": TodoTasks.Add(task); break;
                    case "진행중": InProgressTasks.Add(task); break;
                    case "완료": DoneTasks.Add(task); break;
                }
            }
        }

        // ───────────────────────────────────────
        // 탭 필터 커맨드
        // XAML에서 CommandParameter로 탭 이름 받아옴
        // ───────────────────────────────────────
        [RelayCommand]
        private void Filter(string tab)
        {
            SelectedTab = tab;
            ApplyFilter();
        }

        // ───────────────────────────────────────
        // 전체 초기화 — 프로젝트 선택 해제 시
        // ───────────────────────────────────────
        private void ClearAll()
        {
            DDayText = "";
            CurrentProgress = 0;
            TargetProgress = 0;
            CurrentProgressText = "";
            TargetProgressText = "";
            PaceWarningText = "";
            IsBehindSchedule = false;
            _allTasks.Clear();
            TodoTasks.Clear();
            InProgressTasks.Clear();
            DoneTasks.Clear();
        }

        // ───────────────────────────────────────
        // 작업 추가 커맨드
        // CommandParameter로 어느 컬럼에 추가할지 Status를 받아옴
        // 예: "할일", "진행중", "완료"
        // ───────────────────────────────────────
        [RelayCommand]
        private void AddTask(string status)
        {
            if (CurrentProject == null) return;

            var dialog = new AddTaskDialog(CurrentProject.Id);

            if (dialog.ShowDialog() == true)
            {
                var newTask = dialog.Result;
                if (newTask == null) return;

                // CommandParameter로 받은 status를 Task에 적용
                // 어느 컬럼 버튼을 눌렀는지에 따라 달라짐
                newTask.Status = status;

                var newId = _db.AddTask(newTask);
                newTask.Id = newId;

                // Status에 따라 알맞은 칸반 컬럼에 추가
                switch (newTask.Status)
                {
                    case "할일": TodoTasks.Add(newTask); break;
                    case "진행중": InProgressTasks.Add(newTask); break;
                    case "완료": DoneTasks.Add(newTask); break;
                }

                _allTasks.Add(newTask);
                CalculateProgress();
            }
        }

        // ───────────────────────────────────────
        // Task 삭제 커맨드
        // 카드 우클릭 메뉴에서 삭제 클릭 시 실행됨
        // 삭제할 Task 객체를 CommandParameter로 받아옴
        // ───────────────────────────────────────
        [RelayCommand]
        private void DeleteTask(WorkTask task)
        {
            if (task == null) return;

            // DB에서 삭제
            _db.DeleteTask(task.Id);

            // 칸반 보드 컬럼에서 제거
            // Status에 따라 어느 컬렉션에서 지울지 결정
            switch (task.Status)
            {
                case "할일": TodoTasks.Remove(task); break;
                case "진행중": InProgressTasks.Remove(task); break;
                case "완료": DoneTasks.Remove(task); break;
            }

            // 전체 원본 목록에서도 제거
            // 탭 필터링 기준이 되는 목록이라서 여기서도 지워야 함
            _allTasks.Remove(task);

            // 진행률 다시 계산
            // Task 수가 줄었으니까 진행률이 바뀔 수 있음
            CalculateProgress();
        }
    }
}