using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using Workpace.Models;

namespace Workpace.ViewModels
{
    public partial class AddProjectDialogViewModel : ObservableObject
    {
        // ── 사용자 입력값 ───────────────────────────
        [ObservableProperty]
        private string projectName = "";

        [ObservableProperty]
        private string selectedType = "소프트웨어개발";

        [ObservableProperty]
        private DateTime startDate = DateTime.Today;

        [ObservableProperty]
        private DateTime deadline = DateTime.Today.AddDays(30);

        [ObservableProperty]
        private string description = "";

        [ObservableProperty]
        private string gitHubUrl = "";

        // 프로젝트 유형 목록 — ComboBox에 표시할 항목들
        public List<string> ProjectTypes { get; } = new()
        {
            "소프트웨어개발",
            "디자인",
            "학교과제",
            "직장업무",
            "개인목표"
        };

        // 확인 버튼 눌렀을 때 만들어진 프로젝트
        // null이면 취소한 것
        public Project? Result { get; private set; }

        // 다이얼로그 창 닫기 위해 Window 참조
        private readonly Window _window;

        public AddProjectDialogViewModel(Window window)
        {
            _window = window;
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
                Type = SelectedType,
                StartDate = StartDate,
                Deadline = Deadline,
                Description = Description,
                GitHubUrl = GitHubUrl
            };

            // 창 닫기
            _window.DialogResult = true;
            _window.Close();
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