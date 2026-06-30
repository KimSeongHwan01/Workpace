using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using System.Windows;
using Workpace.Messages;
using Workpace.Models;
using Workpace.Services;
using Workpace.Views;

namespace Workpace.ViewModels
{
    public partial class ProjectViewModel : ObservableObject, IRecipient<ProjectSelectedMessage>
    {
        private readonly DatabaseService _db;
        private readonly PortfolioService _portfolioService;

        // 현재 선택된 프로젝트
        [ObservableProperty]
        private Project? currentProject;

        [ObservableProperty]
        private WorkTask? selectedTask;

        // 작업 상세 패널 표시 여부
        [ObservableProperty]
        private bool isDetailPanelVisible;

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

        // 핵심 기능으로 지정된 Task 개수
        [ObservableProperty]
        private int coreTaskCount;

        // 핵심 기능으로 지정된 Task 목록 — 사이드바에 표시용
        [ObservableProperty]
        private ObservableCollection<WorkTask> coreTasks = new();

        // 현재 연속 작업일 수
        [ObservableProperty]
        private int currentStreak;

        [ObservableProperty] private bool isStage1Done;
        [ObservableProperty] private bool isStage2Done;
        [ObservableProperty] private bool isStage3Done;
        [ObservableProperty] private bool isStage4Done;
        [ObservableProperty] private bool isStage5Done;

        // DB에서 불러온 전체 Task 목록 — 탭 필터링할 때 원본으로 사용
        private List<WorkTask> _allTasks = new();

        // 코드비하인드에서 드래그 후 Task 선택에 사용
        public IEnumerable<WorkTask> AllTasks => _allTasks;

        public ProjectViewModel()
        {
            _db = new DatabaseService();
            _portfolioService = new PortfolioService(_db);
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
            LoadTasks(CurrentProject.Id);
            CalculateProgress();

            CurrentStreak = _db.GetCurrentStreak();
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
            var totalCount = _allTasks.Count;
            var doneCount = _allTasks.Count(t => t.Status == "완료");

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
            // 0~100 범위로 제한 (음수 및 100% 초과 모두 방지)
            TargetProgress = Math.Clamp(TargetProgress, 0, 100);
            TargetProgressText = $"{TargetProgress}%";

            IsBehindSchedule = CurrentProgress < TargetProgress;
            if (IsBehindSchedule)
            {
                var diff = Math.Round(TargetProgress - CurrentProgress, 1);
                var overDays = (int)Math.Ceiling(totalDays * diff / 100);
                PaceWarningText = $"⚠ 지금 속도면 마감을 {overDays}일 초과해요";
            }
            else
            {
                PaceWarningText = "";
            }

            // 각 단계별 완료 여부 계산
            // 해당 단계 Task가 1개 이상 있고 전부 완료 상태일 때 true
            IsStage1Done = _allTasks.Any(t => t.Stage == "기획") && _allTasks.Where(t => t.Stage == "기획").All(t => t.Status == "완료");
            IsStage2Done = _allTasks.Any(t => t.Stage == "설계") && _allTasks.Where(t => t.Stage == "설계").All(t => t.Status == "완료");
            IsStage3Done = _allTasks.Any(t => t.Stage == "개발") && _allTasks.Where(t => t.Stage == "개발").All(t => t.Status == "완료");
            IsStage4Done = _allTasks.Any(t => t.Stage == "테스트") && _allTasks.Where(t => t.Stage == "테스트").All(t => t.Status == "완료");
            IsStage5Done = _allTasks.Any(t => t.Stage == "배포") && _allTasks.Where(t => t.Stage == "배포").All(t => t.Status == "완료");

            // 핵심 기능 개수 갱신
            CoreTaskCount = _allTasks.Count(t => t.IsCore);
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

            // 탭 필터 적용 후 핵심 기능 목록도 갱신
            CoreTaskCount = _allTasks.Count(t => t.IsCore);
            CoreTasks.Clear();
            foreach (var t in _allTasks.Where(t => t.IsCore))
                CoreTasks.Add(t);
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
            dialog.Owner = Application.Current.MainWindow; // 메인 창 기준 중앙 배치

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

                // Task 생성 시에도 오늘 작업한 것으로 스트릭 기록
                _db.RecordStreak();
                CurrentStreak = _db.GetCurrentStreak();

                CalculateProgress();

                //Task 추가 후 현재 탭 기준으로 재필터링
                ApplyFilter();
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

        // ───────────────────────────────────────
        // Task 이동 커맨드 — 드래그앤드롭으로 컬럼 간 이동 시 실행
        // parameter: "할일|진행중" 형태로 "task.Id|새로운Status" 를 받아옴
        // ───────────────────────────────────────
        [RelayCommand]
        private void MoveTask(string parameter)
        {
            // "taskId|newStatus" 형태로 파싱
            var parts = parameter.Split('|');
            if (parts.Length != 2) return;
            if (!int.TryParse(parts[0], out int taskId)) return;
            var newStatus = parts[1];

            // 전체 목록에서 해당 Task 찾기
            var task = _allTasks.FirstOrDefault(t => t.Id == taskId);
            if (task == null) return;

            var oldStatus = task.Status;
            if (oldStatus == newStatus) return; // 같은 컬럼이면 무시

            // 완료에서 다른 컬럼으로 빠질 때 활동 로그 삭제
            if (oldStatus == "완료" && newStatus != "완료" && CurrentProject != null)
            {
                _db.RemoveActivityLog(CurrentProject.Id, $"'{task.Title}' 완료");
            }

            // 기존 컬럼에서 제거
            switch (oldStatus)
            {
                case "할일": TodoTasks.Remove(task); break;
                case "진행중": InProgressTasks.Remove(task); break;
                case "완료": DoneTasks.Remove(task); break;
            }

            // Task 상태 변경
            task.Status = newStatus;

            // 새 컬럼에 추가
            switch (newStatus)
            {
                case "할일": TodoTasks.Add(task); break;
                case "진행중": InProgressTasks.Add(task); break;
                case "완료": DoneTasks.Add(task); break;
            }

            // DB 업데이트
            _db.UpdateTaskStatus(taskId, newStatus);

            // 진행률 재계산
            CalculateProgress();

            // ── 상태 변경 시 항상 스트릭 기록 ────────────────────
            // 어떤 상태로 이동하든 오늘 작업한 것으로 기록
            // UNIQUE(Date) + INSERT OR IGNORE로 하루에 1번만 카운트됨
            _db.RecordStreak();
            CurrentStreak = _db.GetCurrentStreak();

            // 활동 로그는 완료 시에만 기록
            if (newStatus == "완료" && CurrentProject != null)
            {
                _db.AddActivityLog(CurrentProject.Id, $"'{task.Title}' 완료");
            }

            // ── 완료로 이동할 때만 통합 팝업 ──────────────────
            if (newStatus == "완료")
            {
                // 기존 팝업 2개(이슈 유도 + 커밋 메시지)를 하나로 통합한 다이얼로그
                var dialog = new TaskCompleteDialog(task.Title);
                dialog.Owner = Application.Current.MainWindow;

                if (dialog.ShowDialog() == true && dialog.Result != null)
                {
                    var result = dialog.Result;

                    // 이슈 내용이 있으면 DB에 저장
                    if (result.HasIssue)
                    {
                        _db.AddIssue(new Issue
                        {
                            TaskId = task.Id,
                            Problem = result.Problem,
                            Cause = result.Cause,
                            Solution = result.Solution,
                        });

                        // 작업 상세 패널도 열어서 방금 저장된 이슈 확인 가능하게
                        SelectedTask = task;
                        IsDetailPanelVisible = true;
                        LoadIssues(task.Id);
                        LoadFiles(task.Id);
                    }
                }

                // ── 모든 Task 완료 시 회고 팝업 ────────────────
                var allDone = _allTasks.Count > 0 && _allTasks.All(t => t.Status == "완료");
                if (allDone)
                {
                    var dialog2 = new RetrospectDialog();
                    dialog2.Owner = Application.Current.MainWindow;

                    if (dialog2.ShowDialog() == true)
                    {
                        if (CurrentProject == null) return;

                        CurrentProject.RetrospectLearn = dialog2.ResultLearn;
                        CurrentProject.RetrospectRegret = dialog2.ResultRegret;
                        CurrentProject.RetrospectImprove = dialog2.ResultImprove;

                        _db.UpdateProject(CurrentProject);

                        MessageBox.Show("회고가 저장됐어요! 포트폴리오에 자동으로 반영돼요.",
                            "저장 완료", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }

        // 패널 닫기 커맨드
        [RelayCommand]
        private void CloseDetailPanel()
        {
            SelectedTask = null;
            IsDetailPanelVisible = false;
        }

        // ── 작업 상세 수정 모드 ────────────────────────
        // 수정 모드 여부 — true면 입력칸 표시, false면 텍스트 표시
        [ObservableProperty]
        private bool isEditMode = false;

        // 수정 중인 임시 값들 — 저장 전까지 원본에 영향 없음
        [ObservableProperty]
        private string editTitle = "";

        [ObservableProperty]
        private string editDescription = "";

        [ObservableProperty]
        private string editPriority = "";

        [ObservableProperty]
        private string editStage = "";

        [ObservableProperty]
        private DateTime? editDueDate;

        // ───────────────────────────────────────
        // 수정 모드 진입 커맨드
        // 현재 Task 값을 임시 필드에 복사
        // ───────────────────────────────────────
        [RelayCommand]
        private void StartEdit()
        {
            if (SelectedTask == null) return;

            // 원본 값을 임시 필드에 복사
            EditTitle = SelectedTask.Title;
            EditDescription = SelectedTask.Description;
            EditPriority = SelectedTask.Priority;
            EditStage = SelectedTask.Stage;
            EditDueDate = SelectedTask.DueDate;

            IsEditMode = true;
        }

        // ───────────────────────────────────────
        // 수정 저장 커맨드
        // 임시 필드 값을 Task에 반영하고 DB 업데이트
        // ───────────────────────────────────────
        [RelayCommand]
        private void SaveEdit()
        {
            if (SelectedTask == null) return;

            if (string.IsNullOrWhiteSpace(EditTitle))
            {
                MessageBox.Show("제목을 입력해주세요.", "입력 오류",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Task 원본에 반영
            SelectedTask.Title = EditTitle;
            SelectedTask.Description = EditDescription;
            SelectedTask.Priority = EditPriority;
            SelectedTask.Stage = EditStage;
            SelectedTask.DueDate = EditDueDate;

            // DB 업데이트
            _db.UpdateTask(SelectedTask);

            // 칸반 보드 갱신
            LoadTasks(SelectedTask.ProjectId);

            // LoadTasks()가 새 객체로 교체했으니
            // _allTasks에서 같은 Id의 Task를 찾아 SelectedTask 재설정
            var updatedTask = _allTasks.FirstOrDefault(t => t.Id == SelectedTask.Id);
            if (updatedTask != null)
                SelectedTask = updatedTask;

            IsEditMode = false;
        }

        // ───────────────────────────────────────
        // 수정 취소 커맨드
        // 임시 필드 버리고 수정 모드 종료
        // ───────────────────────────────────────
        [RelayCommand]
        private void CancelEdit()
        {
            IsEditMode = false;
        }

        // ── 이슈 기록 ──────────────────────────────
        // 현재 선택된 Task의 이슈 목록
        [ObservableProperty]
        private ObservableCollection<Issue> currentIssues = new();

        // 새 이슈 입력 필드
        [ObservableProperty]
        private string newIssueProblem = "";

        [ObservableProperty]
        private string newIssueCause = "";

        [ObservableProperty]
        private string newIssueSolution = "";

        [ObservableProperty]
        private string newIssueResult = "";

        // ───────────────────────────────────────
        // Task 선택 시 이슈 목록도 함께 로드
        // ───────────────────────────────────────
        [RelayCommand]
        private void SelectTask(WorkTask task)
        {
            SelectedTask = task;
            IsDetailPanelVisible = true;

            // 선택된 Task의 이슈 목록 로드
            LoadIssues(task.Id);
            LoadFiles(task.Id);

            // 입력 필드 초기화
            NewIssueProblem = "";
            NewIssueCause = "";
            NewIssueSolution = "";
            NewIssueResult = "";
        }

        // ───────────────────────────────────────
        // 이슈 목록 로드
        // ───────────────────────────────────────
        private void LoadIssues(int taskId)
        {
            CurrentIssues.Clear();
            var issues = _db.GetIssuesByTask(taskId);
            foreach (var issue in issues)
                CurrentIssues.Add(issue);
        }

        // ───────────────────────────────────────
        // 이슈 추가 커맨드
        // 문제/원인/해결 중 하나라도 입력됐으면 저장
        // ───────────────────────────────────────
        [RelayCommand]
        private void AddIssue()
        {
            if (SelectedTask == null) return;

            if (string.IsNullOrWhiteSpace(NewIssueProblem) &&
                string.IsNullOrWhiteSpace(NewIssueCause) &&
                string.IsNullOrWhiteSpace(NewIssueSolution))
            {
                MessageBox.Show("내용을 하나 이상 입력해주세요.", "입력 오류",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var issue = new Issue
            {
                TaskId = SelectedTask.Id,
                Problem = NewIssueProblem,
                Cause = NewIssueCause,
                Solution = NewIssueSolution,
                Result = NewIssueResult,
                CreatedAt = DateTime.Now
            };

            var newId = _db.AddIssue(issue);
            issue.Id = newId;
            CurrentIssues.Insert(0, issue); // 최신순으로 맨 위에 추가

            // 입력 필드 초기화
            NewIssueProblem = "";
            NewIssueCause = "";
            NewIssueSolution = "";
            NewIssueResult = "";
        }

        // ───────────────────────────────────────
        // 이슈 삭제 커맨드
        // ───────────────────────────────────────
        [RelayCommand]
        private void DeleteIssue(Issue issue)
        {
            if (issue == null) return;
            _db.DeleteIssue(issue.Id);
            CurrentIssues.Remove(issue);
        }

        // ── 연결된 파일 ────────────────────────────
        // 현재 선택된 Task의 파일 목록
        [ObservableProperty]
        private ObservableCollection<TaskFile> currentFiles = new();

        // ───────────────────────────────────────
        // 파일 목록 로드
        // ───────────────────────────────────────
        private void LoadFiles(int taskId)
        {
            CurrentFiles.Clear();
            var files = _db.GetFilesByTask(taskId);
            foreach (var file in files)
                CurrentFiles.Add(file);
        }

        // ───────────────────────────────────────
        // 파일 추가 커맨드
        // 탐색기 열어서 파일 선택 → DB 저장
        // ───────────────────────────────────────
        [RelayCommand]
        private void AddFile()
        {
            if (SelectedTask == null) return;

            // OpenFileDialog — 탐색기 열어서 파일 선택
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "파일 선택",
                Multiselect = false, // 단일 파일만 선택
                Filter = "모든 파일 (*.*)|*.*"
            };

            if (dialog.ShowDialog() != true) return;

            var file = new TaskFile
            {
                TaskId = SelectedTask.Id,
                FileName = System.IO.Path.GetFileName(dialog.FileName),
                FilePath = dialog.FileName
            };

            var newId = _db.AddFile(file);
            file.Id = newId;
            CurrentFiles.Add(file);
        }

        // ───────────────────────────────────────
        // 파일 삭제 커맨드
        // ───────────────────────────────────────
        [RelayCommand]
        private void DeleteFile(TaskFile file)
        {
            if (file == null) return;
            _db.DeleteFile(file.Id);
            CurrentFiles.Remove(file);
        }

        // ───────────────────────────────────────
        // 핵심 기능 토글 커맨드
        // 칸반 카드에서 🔒 클릭 시 IsCore를 반전시킴
        // 핵심 기능이 3개를 초과하면 마감일 연장 경고 표시
        // ───────────────────────────────────────
        [RelayCommand]
        private void ToggleCore(WorkTask task)
        {
            if (task == null) return;

            // IsCore가 false인데 3개 이상이면 추가 차단
            if (!task.IsCore && _allTasks.Count(t => t.IsCore) >= 3)
            {
                var result = MessageBox.Show(
                    $"핵심 기능은 3개까지 권장해요.\n\n" +
                    $"'{task.Title}'을 추가하면 마감 내 완료가 어려워질 수 있어요.\n\n" +
                    $"그래도 추가할까요?",
                    "스코프 경고",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No) return;

                // 마감일 건드리지 않음 — 페이스 경고가 자동으로 악화됨
            }

            // IsCore 반전
            task.IsCore = !task.IsCore;

            // 잠글 때 시각 기록, 해제할 때 초기화
            if (task.IsCore)
                task.CoreLockedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            else
                task.CoreLockedAt = string.Empty;

            _db.UpdateTask(task);

            CalculateProgress();

            // 잠근 순서대로 정렬해서 추가
            CoreTaskCount = _allTasks.Count(t => t.IsCore);
            CoreTasks.Clear();
            foreach (var t in _allTasks
                .Where(t => t.IsCore)
                .OrderBy(t => t.CoreLockedAt))
                CoreTasks.Add(t);
        }

        // ───────────────────────────────────────
        // 파일 열기 커맨드
        // 연결된 파일을 기본 앱으로 실행
        // ───────────────────────────────────────
        [RelayCommand]
        private void OpenFile(TaskFile file)
        {
            if (file == null) return;

            if (!System.IO.File.Exists(file.FilePath))
            {
                MessageBox.Show("파일을 찾을 수 없습니다.\n경로: " + file.FilePath,
                    "파일 없음", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Process.Start — 기본 연결 앱으로 파일 실행
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = file.FilePath,
                UseShellExecute = true // 기본 앱으로 열기
            });
        }
    }
}