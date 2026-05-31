using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using Workpace.Messages;
using Workpace.Models;
using Workpace.Services;

namespace Workpace.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        // DatabaseService 인스턴스 — DB 작업은 전부 여기를 통해서 함
        private readonly DatabaseService _db;

        // ───────────────────────────────────────
        // ObservableCollection — 일반 List와 달리
        // 항목이 추가/삭제될 때 UI가 자동으로 갱신됨
        // ───────────────────────────────────────
        [ObservableProperty]
        private ObservableCollection<Project> projects = new();

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

        public MainViewModel()
        {
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
            // 임시 테스트용 프로젝트 — 나중에 입력 다이얼로그로 교체
            var newProject = new Project
            {
                Name = "새 프로젝트",
                Type = "소프트웨어개발",
                StartDate = DateTime.Today,
                Deadline = DateTime.Today.AddDays(30),
                Description = "",
                GitHubUrl = ""
            };

            var newId = _db.AddProject(newProject);
            newProject.Id = newId;
            Projects.Insert(0, newProject); // 목록 맨 앞에 추가
        }

        // ───────────────────────────────────────
        // DELETE — 선택된 프로젝트 삭제
        // SelectedProject가 null이면 아무것도 안 함
        // ───────────────────────────────────────
        [RelayCommand]
        private void DeleteProject()
        {
            if (SelectedProject == null) return;

            _db.DeleteProject(SelectedProject.Id);
            Projects.Remove(SelectedProject);
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
        }
    }
}