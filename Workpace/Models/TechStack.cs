using CommunityToolkit.Mvvm.ComponentModel;

namespace Workpace.Models
{
    // 기술 스택 하나를 나타내는 클래스
    // IsSelected — 사용자가 선택했는지 여부
    public partial class TechStack : ObservableObject
    {
        public string Name { get; set; } = "";

        [ObservableProperty]
        private bool isSelected;
    }
}