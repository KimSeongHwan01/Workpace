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
        private string gitHub = "";

        [ObservableProperty]
        private string blog = "";

        [ObservableProperty]
        private string linkedIn = "";

        [ObservableProperty]
        private string bio = "";

        public SettingsViewModel()
        {
            _db = new DatabaseService();

            // 앱 시작 시 저장된 프로필 불러오기
            LoadProfile();
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
            GitHub = profile.GitHub;
            Blog = profile.Blog;
            LinkedIn = profile.LinkedIn;
            Bio = profile.Bio;
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
                GitHub = GitHub,
                Blog = Blog,
                LinkedIn = LinkedIn,
                Bio = Bio
            };

            _db.SaveUserProfile(profile);

            MessageBox.Show("프로필이 저장되었습니다.", "저장 완료",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}