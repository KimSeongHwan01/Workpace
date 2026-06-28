using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;
using Workpace.Models;

namespace Workpace.ViewModels
{
    public partial class TaskCompleteDialogViewModel : ObservableObject
    {
        // ──────────────────────────────────────────
        // 생성자에서 받아오는 Task 제목
        // ──────────────────────────────────────────
        public string TaskTitle { get; }

        // ──────────────────────────────────────────
        // 커밋 타입 목록 (라디오 버튼으로 표시)
        // ──────────────────────────────────────────
        public ObservableCollection<CommitTypeItem> CommitTypes { get; } = new()
        {
            new CommitTypeItem { Type = "feat",     Description = "새로운 기능 추가" },
            new CommitTypeItem { Type = "fix",      Description = "버그 수정" },
            new CommitTypeItem { Type = "refactor", Description = "코드 개선 (기능 변화 없음)" },
            new CommitTypeItem { Type = "docs",     Description = "문서 / 주석 작성" },
            new CommitTypeItem { Type = "chore",    Description = "설정, 패키지 등 기타 작업" },
        };

        // ──────────────────────────────────────────
        // 선택된 커밋 타입 아이템
        // 변경될 때마다 커밋 메시지 미리보기 갱신
        // ──────────────────────────────────────────
        [ObservableProperty]
        private CommitTypeItem _selectedCommitType;

        partial void OnSelectedCommitTypeChanged(CommitTypeItem value)
        {
            // 선택된 타입으로 미리보기 메시지 업데이트
            UpdateCommitMessage();
        }

        // ──────────────────────────────────────────
        // 이슈 입력 필드
        // Problem이 입력되면 fix 자동 추천
        // ──────────────────────────────────────────
        [ObservableProperty]
        private string _problem = string.Empty;

        [ObservableProperty]
        private string _cause = string.Empty;

        [ObservableProperty]
        private string _solution = string.Empty;

        // ──────────────────────────────────────────
        // 커밋 메시지 미리보기
        // ──────────────────────────────────────────
        [ObservableProperty]
        private string _commitMessage = string.Empty;

        private void UpdateCommitMessage()
        {
            CommitMessage = $"{SelectedCommitType.Type}: {TaskTitle}";
        }

        // ──────────────────────────────────────────
        // 복사 버튼
        // ──────────────────────────────────────────
        [RelayCommand]
        private void CopyCommitMessage()
        {
            Clipboard.SetText(CommitMessage);
        }

        // 라디오 버튼 클릭 시 호출
        // 선택된 타입으로 교체하고 미리보기 갱신
        [RelayCommand]
        private void SelectCommitType(CommitTypeItem selected)
        {
            // 기존 선택 해제
            foreach (var item in CommitTypes)
                item.IsSelected = false;

            // 새로운 타입 선택
            selected.IsSelected = true;
            SelectedCommitType = selected;
        }

        // ──────────────────────────────────────────
        // 완료 / 취소 버튼
        // DialogResult 패턴으로 View에 결과 전달
        // ──────────────────────────────────────────
        public bool? DialogResult { get; private set; }
        public TaskCompleteResult? Result { get; private set; }

        [RelayCommand]
        private void Confirm(Window window)
        {
            // 결과 데이터 구성
            Result = new TaskCompleteResult
            {
                Problem = Problem,
                Cause = Cause,
                Solution = Solution,
                CommitType = SelectedCommitType.Type,
                CommitMessage = CommitMessage,
            };

            DialogResult = true;
            window.DialogResult = true;
            window.Close();
        }

        [RelayCommand]
        private void Cancel(Window window)
        {
            DialogResult = false;
            window.DialogResult = false;
            window.Close();
        }

        // ──────────────────────────────────────────
        // 생성자
        // Task 제목 받아서 초기 커밋 메시지 세팅
        // ──────────────────────────────────────────
        public TaskCompleteDialogViewModel(string taskTitle)
        {
            TaskTitle = taskTitle;

            // 기본값은 feat
            _selectedCommitType = CommitTypes.First(x => x.Type == "feat");
            _selectedCommitType.IsSelected = true;
            UpdateCommitMessage();
        }
    }

    /// <summary>
    /// 커밋 타입 라디오 버튼 하나하나를 나타내는 클래스
    /// </summary>
    public partial class CommitTypeItem : ObservableObject
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // 라디오 버튼 선택 상태 바인딩용
        [ObservableProperty]
        private bool _isSelected;
    }
}