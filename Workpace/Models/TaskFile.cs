using CommunityToolkit.Mvvm.ComponentModel;

namespace Workpace.Models
{
    public partial class TaskFile : ObservableObject
    {
        [ObservableProperty] private int id;
        [ObservableProperty] private int taskId;
        [ObservableProperty] private string fileName = string.Empty;
        [ObservableProperty] private string filePath = string.Empty;
    }
}