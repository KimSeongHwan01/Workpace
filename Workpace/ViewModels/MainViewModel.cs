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
    public partial class MainViewModel : ObservableObject
    {
        // DatabaseService 인스턴스 — DB 작업은 전부 여기를 통해서 함
        private readonly DatabaseService _db;
        
        private readonly ProjectViewModel _projectVM;

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

        // 아직 기능 없는 빈 커맨드 — 나중에 채울 예정
        [RelayCommand]
        private void GoToWorkspace() { }

        [RelayCommand]
        private void GoToStatistics() { }

        [RelayCommand]
        private void GoToSettings() { }

        public MainViewModel(ProjectViewModel projectVM)
        {
            _projectVM = projectVM;
            _db = new DatabaseService();
            LoadProjects(); // 앱 시작 시 DB에서 프로젝트 목록 불러오기
        }

        // ───────────────────────────────────────
        // READ — DB에서 전체 프로젝트 목록 불러오기
        // ───────────────────────────────────────
        private void LoadProjects()
        {
            Projects.Clear(); // 기존 목록 초기화

            var list = _db.GetAllProjects();
            foreach (var project in list)
            {
                Projects.Add(project);
            }
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
                view.DataContext = _projectVM;
                CurrentView = view;
            }
            else
            {
                CurrentView = null;
            }
        }
    }
}