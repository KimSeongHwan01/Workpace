using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Workpace.Models;
using Workpace.Services;
using System.Collections.ObjectModel;

namespace Workpace.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly DatabaseService _db;

        // UI 바인딩용 컬렉션
        [ObservableProperty]
        private ObservableCollection<Project> projects = new();

        [ObservableProperty]
        private Project? selectedProject;

        public MainViewModel()
        {
            _db = new DatabaseService();
            LoadProjects();
        }

        private void LoadProjects()
        {
            // 다음 단계에서 DB 조회 코드 추가
        }

        [RelayCommand]
        private void AddProject()
        {
            // 프로젝트 추가 다이얼로그 열기
        }
    }
}