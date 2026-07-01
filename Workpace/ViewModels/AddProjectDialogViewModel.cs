using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using Workpace.Models;
using System.Collections.ObjectModel;

namespace Workpace.ViewModels
{
    public partial class AddProjectDialogViewModel : ObservableObject
    {
        // ── 사용자 입력값 ───────────────────────────
        [ObservableProperty]
        private string projectName = "";

        [ObservableProperty]
        private DateTime startDate = DateTime.Today;

        [ObservableProperty]
        private DateTime deadline = DateTime.Today.AddDays(30);

        [ObservableProperty]
        private string description = "";

        [ObservableProperty]
        private string gitHubUrl = "";

        [ObservableProperty]
        private string background = "";

        [ObservableProperty]
        private string techReason = "";

        [ObservableProperty]
        private string role = "";

        [ObservableProperty]
        private string architecture = "";

        // 기술 스택 목록 — 선택 가능한 전체 목록
        public ObservableCollection<TechStack> TechStacks { get; } = new()
        {
            new TechStack { Name = "C#" },
            new TechStack { Name = "WPF" },
            new TechStack { Name = "SQLite" },
            new TechStack { Name = "Python" },
            new TechStack { Name = "React" },
            new TechStack { Name = "Flutter" },
            new TechStack { Name = "Java" },
        };

        // "+ 기타 추가" 클릭 시 입력창을 보여줄지 여부
        [ObservableProperty]
        private bool isAddingTechStack;

        // 입력창에 사용자가 타이핑 중인 새 기술 스택 이름
        [ObservableProperty]
        private string newTechStackInput = "";

        // 선택된 기술 스택 목록 반환
        // Confirm() 에서 Project 생성 시 사용
        public List<string> SelectedTechStacks =>
            TechStacks.Where(t => t.IsSelected)
                      .Select(t => t.Name)
                      .ToList();

        // 확인 버튼 눌렀을 때 만들어진 프로젝트
        // null이면 취소한 것
        public Project? Result { get; private set; }

        // 다이얼로그 창 닫기 위해 Window 참조
        private readonly Window _window;

        // 수정 모드 여부 — 다이얼로그 제목/버튼 문구 분기용
        public bool IsEditMode { get; }

        public AddProjectDialogViewModel(Window window, Project? editingProject = null)
        {
            _window = window;
            IsEditMode = editingProject != null;

            // 수정 모드면 기존 프로젝트 값을 입력 필드에 채워줌
            // (TechStacks 선택 항목은 DB에 따로 저장되지 않아 복원 대상에서 제외 — 기존 구조의 한계)
            if (editingProject != null)
            {
                ProjectName = editingProject.Name;
                StartDate = editingProject.StartDate;
                Deadline = editingProject.Deadline;
                Description = editingProject.Description;
                GitHubUrl = editingProject.GitHubUrl;
                Background = editingProject.Background;
                TechReason = editingProject.TechReason;
                Role = editingProject.Role;
                Architecture = editingProject.Architecture;

                // 저장돼 있던 기술 스택 콤마 문자열을 분리해서
                // TechStacks 목록 중 이름이 일치하는 항목만 IsSelected = true로 켜줌
                var savedStacks = editingProject.TechStack
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                foreach (var stack in TechStacks)
                    stack.IsSelected = savedStacks.Contains(stack.Name);

                // 고정 7개 목록에는 없지만 저장돼 있던 이름(= 과거에 "기타 추가"로 넣었던 커스텀 항목)은
                // 목록 자체에 없어서 위 루프로 체크가 안 됨 → 새 토글 버튼으로 다시 만들어서 추가
                var customStacks = savedStacks.Where(
                    name => !TechStacks.Any(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)));

                foreach (var name in customStacks)
                    TechStacks.Add(new TechStack { Name = name, IsSelected = true });
            }
        }

        // ───────────────────────────────────────
        // 확인 버튼 — 입력값 검증 후 Project 생성
        // ───────────────────────────────────────
        [RelayCommand]
        private void Confirm()
        {
            // 프로젝트 이름 필수 검증
            if (string.IsNullOrWhiteSpace(ProjectName))
            {
                MessageBox.Show("프로젝트 이름을 입력해주세요.", "입력 오류",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 마감일이 시작일보다 앞이면 오류
            if (Deadline <= StartDate)
            {
                MessageBox.Show("마감일은 시작일보다 늦어야 합니다.", "입력 오류",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 검증 통과 — Project 객체 생성
            Result = new Project
            {
                Name = ProjectName,
                Type = "",
                StartDate = StartDate,
                Deadline = Deadline,
                Description = Description,
                GitHubUrl = GitHubUrl,
                Background = Background,
                TechReason = TechReason,
                Role = Role,
                Architecture = Architecture,
                TechStack = string.Join(",", SelectedTechStacks),
            };

            // 창 닫기
            _window.DialogResult = true;
            _window.Close();
        }

        // ───────────────────────────────────────
        // "+ 기타 추가" 버튼 — 입력창 표시
        // ───────────────────────────────────────
        [RelayCommand]
        private void ShowAddTechStack()
        {
            IsAddingTechStack = true;
        }

        // ───────────────────────────────────────
        // 입력한 이름으로 새 기술 스택 토글 버튼 추가
        // ───────────────────────────────────────
        [RelayCommand]
        private void AddCustomTechStack()
        {
            var name = NewTechStackInput.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return;

            // 이미 같은 이름(대소문자 무시)이 있으면 그 항목만 선택 처리하고 끝
            var existing = TechStacks.FirstOrDefault(
                t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                existing.IsSelected = true;
            }
            else
            {
                // 새로 추가하는 항목은 바로 선택된 상태로 시작
                // (입력해서 추가했다는 것 자체가 "이거 쓸 거다"라는 의사표현이므로)
                TechStacks.Add(new TechStack { Name = name, IsSelected = true });
            }

            // 입력창 초기화하고 닫기
            NewTechStackInput = "";
            IsAddingTechStack = false;
        }

        // ───────────────────────────────────────
        // 입력 취소
        // ───────────────────────────────────────
        [RelayCommand]
        private void CancelAddTechStack()
        {
            NewTechStackInput = "";
            IsAddingTechStack = false;
        }

        // ───────────────────────────────────────
        // 취소 버튼 — 그냥 닫기
        // ───────────────────────────────────────
        [RelayCommand]
        private void Cancel()
        {
            _window.DialogResult = false;
            _window.Close();
        }
    }
}