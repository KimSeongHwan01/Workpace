using CommunityToolkit.Mvvm.ComponentModel;

namespace Workpace.Models
{
    // ObservableObject 상속 — 프로퍼티 변경 시 UI 자동 갱신
    public partial class WorkTask : ObservableObject
    {
        [ObservableProperty] private int id;
        [ObservableProperty] private int projectId;
        [ObservableProperty] private string title = string.Empty;
        [ObservableProperty] private string status = "할일";
        [ObservableProperty] private string priority = "보통";
        [ObservableProperty] private DateTime? dueDate;
        [ObservableProperty] private string stage = "기획";
        [ObservableProperty] private int progress = 0;
        [ObservableProperty] private bool isCore = false;
        public string CoreLockedAt { get; set; } = string.Empty; // 핵심 기능으로 잠근 시각
    }
}