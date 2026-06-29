using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Workpace.Models
{
    public class Project : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        // CallerMemberName — 호출한 프로퍼티 이름을 자동으로 넣어줌
        // 직접 문자열 안 써도 돼서 오타 실수 방지
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime Deadline { get; set; }
        public string Description { get; set; } = string.Empty;
        public string GitHubUrl { get; set; } = string.Empty;
        public string Background { get; set; } = string.Empty;
        public string TechReason { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Architecture { get; set; } = string.Empty;
        public string RetrospectLearn { get; set; } = string.Empty;
        public string RetrospectRegret { get; set; } = string.Empty;
        public string RetrospectImprove { get; set; } = string.Empty;

        // DB 저장 X — 바뀔 때 UI에 알림 보냄
        private double _currentProgress;
        public double CurrentProgress
        {
            get => _currentProgress;
            set
            {
                if (_currentProgress == value) return;
                _currentProgress = value;
                OnPropertyChanged();
                // CurrentProgress가 바뀌면 SidebarStatusText도 같이 갱신
                OnPropertyChanged(nameof(SidebarStatusText));
            }
        }

        public string SidebarStatusText
        {
            get
            {
                var daysLeft = (Deadline - DateTime.Today).Days;
                var dday = daysLeft > 0 ? $"D-{daysLeft}일"
                         : daysLeft == 0 ? "D-day"
                         : $"D+{Math.Abs(daysLeft)}일";
                return $"{dday} · {CurrentProgress:0}%";
            }
        }
    }
}