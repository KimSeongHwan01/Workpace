using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Windows;
using Workpace.Models;
using Workpace.Services;

namespace Workpace.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly DatabaseService _db;

        // ── 사용자 프로필 입력 필드 ────────────────────
        [ObservableProperty]
        private string name = "";

        [ObservableProperty]
        private string email = "";

        [ObservableProperty]
        private string blog = "";

        [ObservableProperty]
        private string linkedIn = "";

        [ObservableProperty]
        private string bio = "";

        // 알림 설정 토글
        [ObservableProperty]
        private bool streakReminderEnabled = true;

        [ObservableProperty]
        private bool projectDeadlineAlertEnabled = true;

        [ObservableProperty]
        private bool taskDeadlineAlertEnabled = true;

        [ObservableProperty]
        private int streakReminderIntervalHours = 1;

        // 프로필 로딩 중인지 여부
        // LoadProfile()이 토글 값을 채우는 동안 On{Property}Changed가 같이 발동되는데
        // 이건 "불러오는 것"이라 DB에 다시 쓰면 안 됨 → 이 플래그로 그 시점만 걸러냄
        private bool _isLoading;

        public SettingsViewModel()
        {
            _db = new DatabaseService();

            // 앱 시작 시 저장된 프로필 불러오기
            _isLoading = true;
            LoadProfile();
            _isLoading = false;
        }

        // ───────────────────────────────────────
        // 저장된 프로필 불러오기
        // DB에 프로필이 없으면 빈 값으로 시작
        // ───────────────────────────────────────
        private void LoadProfile()
        {
            var profile = _db.GetUserProfile();
            if (profile == null) return;

            Name = profile.Name;
            Email = profile.Email;
            Blog = profile.Blog;
            LinkedIn = profile.LinkedIn;
            Bio = profile.Bio;
            StreakReminderEnabled = profile.StreakReminderEnabled;
            ProjectDeadlineAlertEnabled = profile.ProjectDeadlineAlertEnabled;
            TaskDeadlineAlertEnabled = profile.TaskDeadlineAlertEnabled;
            StreakReminderIntervalHours = profile.StreakReminderIntervalHours;
        }

        // ───────────────────────────────────────
        // 저장 커맨드
        // 입력 필드 값을 UserProfile로 만들어서 DB에 저장
        // ───────────────────────────────────────
        [RelayCommand]
        private void Save()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                MessageBox.Show("이름을 입력해주세요.", "입력 오류",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var profile = new UserProfile
            {
                Name = Name,
                Email = Email,
                Blog = Blog,
                LinkedIn = LinkedIn,
                Bio = Bio,
                StreakReminderEnabled = StreakReminderEnabled,
                ProjectDeadlineAlertEnabled = ProjectDeadlineAlertEnabled,
                TaskDeadlineAlertEnabled = TaskDeadlineAlertEnabled,
                StreakReminderIntervalHours = StreakReminderIntervalHours
            };

            _db.SaveUserProfile(profile);

            MessageBox.Show("프로필이 저장되었습니다.", "저장 완료",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ───────────────────────────────────────
        // 알림 토글 — 값이 바뀌는 즉시 DB에 그 항목만 저장
        // 프로필 "저장" 버튼과 완전히 분리된 동작
        // ───────────────────────────────────────
        partial void OnStreakReminderEnabledChanged(bool value)
        {
            if (_isLoading) return;
            _db.UpdateStreakReminderEnabled(value);
        }

        partial void OnProjectDeadlineAlertEnabledChanged(bool value)
        {
            if (_isLoading) return;
            _db.UpdateProjectDeadlineAlertEnabled(value);
        }

        partial void OnTaskDeadlineAlertEnabledChanged(bool value)
        {
            if (_isLoading) return;
            _db.UpdateTaskDeadlineAlertEnabled(value);
        }
    }
}