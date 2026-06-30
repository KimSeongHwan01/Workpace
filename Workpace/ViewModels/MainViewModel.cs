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
    public partial class MainViewModel : ObservableObject
    {
        // DatabaseService 인스턴스 — DB 작업은 전부 여기를 통해서 함
        private readonly DatabaseService _db;

        public ProjectViewModel ProjectViewModel { get; }

        // ProjectViewModel의 CoreTaskCount를 사이드바에 노출
        // ProjectViewModel이 바뀌면 이 값도 같이 바뀌도록 연결
        public int CoreTaskCount => ProjectViewModel.CoreTaskCount;

        // ProjectViewModel의 CurrentStreak을 사이드바에 노출
        public int CurrentStreak => ProjectViewModel.CurrentStreak;

        public int BestStreak => _db.GetBestStreak();

        private readonly PortfolioViewModel _portfolioVM;

        private readonly NotificationService _notificationService;

        // ───────────────────────────────────────
        // ObservableCollection — 일반 List와 달리
        // 항목이 추가/삭제될 때 UI가 자동으로 갱신됨
        // ───────────────────────────────────────
        [ObservableProperty]
        private ObservableCollection<Project> projects = new();

        // 오른쪽 콘텐츠 영역에 표시할 화면
        // object 타입인 이유 — 나중에 ProjectView, StatisticsView 등 다양한 화면이 들어올 수 있어서
        [ObservableProperty]
        private object? currentView;

        // 현재 선택된 프로젝트 — 사이드바에서 클릭한 프로젝트
        [ObservableProperty]
        private Project? selectedProject;

        // 핵심 기능 Task 목록을 사이드바에 노출
        public System.Collections.ObjectModel.ObservableCollection<WorkTask> CoreTasks
            => ProjectViewModel.CoreTasks;

        public MainViewModel(ProjectViewModel projectVM)
        {
            ProjectViewModel = projectVM;
            _portfolioVM = new PortfolioViewModel();
            _db = new DatabaseService();
            LoadProjects(); // 앱 시작 시 DB에서 프로젝트 목록 불러오기

            // 앱 실행 시 바로 스트릭 계산
            ProjectViewModel.CurrentStreak = _db.GetCurrentStreak();

            ProjectViewModel.PropertyChanged += OnProjectViewModelPropertyChanged;

            // 알림 서비스 시작 전, 프로필 행이 없으면 기본값으로 미리 생성
            // (없으면 알림 토글들이 화면엔 켜져 있어도 실제로는 동작 안 함)
            _db.EnsureUserProfileExists();

            // 알림 서비스 시작
            _notificationService = new NotificationService(_db);
            _notificationService.Start();
        }

        // PropertyChanged 핸들러를 메서드로 분리
        // 람다로 쓰면 -= 로 해제할 수 없어서 메모리 누수 위험
        private void OnProjectViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ProjectViewModel.CoreTaskCount))
                OnPropertyChanged(nameof(CoreTaskCount));

            if (e.PropertyName == nameof(ProjectViewModel.CurrentStreak))
            {
                OnPropertyChanged(nameof(CurrentStreak));
                OnPropertyChanged(nameof(BestStreak));
            }

            if (e.PropertyName == nameof(ProjectViewModel.CurrentProgress))
                RefreshSelectedProjectProgress();
        }

        // ───────────────────────────────────────
        // READ — DB에서 전체 프로젝트 목록 불러오기
        // ───────────────────────────────────────
        private void LoadProjects()
        {
            Projects.Clear();
            var list = _db.GetAllProjects();
            foreach (var project in list)
            {
                // 현재 진행률 계산 (완료 Task / 전체 Task × 100)
                var tasks = _db.GetTasksByProject(project.Id);
                var total = tasks.Count;
                var done = tasks.Count(t => t.Status == "완료");
                project.CurrentProgress = total > 0
                    ? Math.Round((double)done / total * 100, 1)
                    : 0;

                Projects.Add(project);
            }
        }

        [RelayCommand]
        private void GoToStatistics()
        {
            // 통계 화면으로 전환
            // 매번 새로 만들지 않고 기존 인스턴스 재사용
            _statisticsVM.RefreshProjects();

            var view = new StatisticsView();
            view.DataContext = _statisticsVM;
            CurrentView = view;
        }

        // SettingsViewModel 인스턴스 — 한 번만 만들어서 재사용
        private readonly SettingsViewModel _settingsVM = new();

        // StatisticsViewModel 인스턴스 — 한 번만 만들어서 재사용
        private readonly StatisticsViewModel _statisticsVM = new();

        [RelayCommand]
        private void GoToSettings()
        {
            var view = new SettingsView();
            view.DataContext = _settingsVM;
            CurrentView = view;
        }

        // ───────────────────────────────────────
        // 포트폴리오 추출 화면으로 전환
        // 현재 선택된 프로젝트가 있어야 함
        // ───────────────────────────────────────
        [RelayCommand]
        private void GoToPortfolio()
        {
            if (SelectedProject == null)
            {
                MessageBox.Show("프로젝트를 먼저 선택해주세요.", "알림",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _portfolioVM.SetProject(SelectedProject);

            var view = new PortfolioView();
            view.DataContext = _portfolioVM;
            CurrentView = view;
        }

        // ───────────────────────────────────────
        // CREATE — 새 프로젝트 추가
        // [RelayCommand]가 AddProjectCommand를 자동 생성해줌
        // XAML에서 Command="{Binding AddProjectCommand}"로 연결
        // ───────────────────────────────────────
        [RelayCommand]
        private void AddProject()
        {
            // 다이얼로그 창 열기
            var dialog = new AddProjectDialog();

            dialog.Owner = Application.Current.MainWindow;

            // ShowDialog() — 창이 닫힐 때까지 여기서 기다림
            // 확인 누르면 true, 취소 누르면 false 반환
            if (dialog.ShowDialog() == true)
            {
                // 확인 눌렀을 때만 실행
                // dialog.Result — AddProjectDialogViewModel에서 만든 Project 객체
                var newProject = dialog.Result;

                if (newProject == null) return;

                // DB에 저장 후 Id 받아오기
                var newId = _db.AddProject(newProject);
                newProject.Id = newId;

                // 사이드바 목록 맨 앞에 추가
                Projects.Insert(0, newProject);
            }
        }

        // ───────────────────────────────────────
        // UPDATE — 프로젝트 수정 (연필 아이콘 클릭 시 호출)
        // AddProjectDialog를 "수정 모드"로 열어서 기존 값을 채워줌
        // ───────────────────────────────────────
        [RelayCommand]
        private void EditProject(Project project)
        {
            if (project == null) return;

            // 기존 프로젝트 값을 채운 수정 모드 다이얼로그
            var dialog = new AddProjectDialog(project);

            dialog.Owner = Application.Current.MainWindow;

            if (dialog.ShowDialog() == true)
            {
                var updated = dialog.Result;
                if (updated == null) return;

                // Id와 진행률(DB에 없는 계산값)은 기존 프로젝트 것을 그대로 유지
                updated.Id = project.Id;
                updated.CurrentProgress = project.CurrentProgress;

                _db.UpdateProject(updated);

                // 주의: Project.cs는 Name/Deadline 등 변경 시 PropertyChanged를 안 쏨
                // (CurrentProgress만 INPC 연결되어 있음)
                // → 같은 자리에서 객체 자체를 교체해야 ListBox가 새 값으로 다시 그려짐
                var index = Projects.IndexOf(project);
                if (index >= 0)
                    Projects[index] = updated;

                // 수정한 게 현재 선택된 프로젝트였다면 선택도 새 객체로 갈아끼움
                if (SelectedProject?.Id == updated.Id)
                    SelectedProject = updated;
            }
        }

        // ───────────────────────────────────────
        // DELETE — 파라미터로 받은 프로젝트 삭제
        // 우클릭 메뉴에서 직접 Project 객체를 넘겨받음
        // ───────────────────────────────────────
        [RelayCommand]
        private void DeleteProject(Project project)
        {
            if (project == null) return;

            _db.DeleteProject(project.Id);
            Projects.Remove(project);

            // 삭제된 프로젝트가 현재 선택된 프로젝트면 선택 해제
            if (SelectedProject?.Id == project.Id)
                SelectedProject = null;
        }

        // ───────────────────────────────────────
        // SelectedProject가 바뀌는 순간 자동으로 호출됨
        // CommunityToolkit이 [ObservableProperty] 감지해서 호출해줌
        // 메시지를 발송해서 ProjectViewModel에게 선택 변경을 알림
        // ───────────────────────────────────────
        partial void OnSelectedProjectChanged(Project? value)
        {
            WeakReferenceMessenger.Default.Send(new ProjectSelectedMessage(value));

            if (value != null)
            {
                // ProjectView 생성 시 DataContext를 ProjectViewModel로 설정
                var view = new ProjectView();
                view.DataContext = ProjectViewModel;
                CurrentView = view;
            }
            else
            {
                CurrentView = null;
            }
        }

        // ───────────────────────────────────────
        // 같은 프로젝트 재클릭 시 프로젝트 화면으로 전환
        // 포트폴리오 화면 등 다른 화면에서 돌아올 때 사용
        // ───────────────────────────────────────
        public void OnProjectReselected(Project project)
        {
            var view = new ProjectView();
            view.DataContext = ProjectViewModel;
            CurrentView = view;
        }

        // 현재 선택된 프로젝트의 사이드바 진행률 갱신
        private void RefreshSelectedProjectProgress()
        {
            if (SelectedProject == null) return;

            var tasks = _db.GetTasksByProject(SelectedProject.Id);
            var total = tasks.Count;
            var done = tasks.Count(t => t.Status == "완료");

            // Project가 INotifyPropertyChanged를 구현하므로
            // 값만 바꾸면 사이드바 UI가 자동으로 갱신됨
            SelectedProject.CurrentProgress = total > 0
                ? Math.Round((double)done / total * 100, 1)
                : 0;
        }
    }
}