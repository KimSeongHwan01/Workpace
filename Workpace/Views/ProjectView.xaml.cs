using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Workpace.Models;
using Workpace.ViewModels;

namespace Workpace.Views
{
    public partial class ProjectView : UserControl
    {
        // 드래그 시작 위치 저장 — 너무 민감하게 반응하지 않도록 거리 체크에 사용
        private Point _dragStartPoint;

        // 현재 드래그 중인 Task 저장
        private WorkTask? _draggingTask;

        // ListBox 안에서 눌렸을 때만 true
        private bool _isDragReady;

        private bool _wasDragging;

        public ProjectView()
        {
            InitializeComponent();

            // DataContext가 설정된 후 SelectedTab 변경 감지 시작
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // 이전 ViewModel 구독 해제
            if (e.OldValue is ProjectViewModel oldVm)
                oldVm.PropertyChanged -= OnViewModelPropertyChanged;

            // 새 ViewModel 구독
            if (e.NewValue is ProjectViewModel newVm)
            {
                newVm.PropertyChanged += OnViewModelPropertyChanged;
                // 초기 탭 스타일 적용
                UpdateTabStyles(newVm.SelectedTab);
            }
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ProjectViewModel.SelectedTab))
            {
                var vm = sender as ProjectViewModel;
                if (vm != null) UpdateTabStyles(vm.SelectedTab);
            }
        }

        // 선택된 탭만 Active 스타일, 나머지는 기본 스타일로 교체
        private void UpdateTabStyles(string selectedTab)
        {
            // 탭 이름 → 버튼 매핑
            var tabMap = new Dictionary<string, Button>
            {
                { "전체", TabAll },
                { "기획", TabPlan },
                { "설계", TabDesign },
                { "개발", TabDev },
                { "테스트", TabTest },
                { "배포", TabDeploy }
            };

            var activeStyle = (Style)FindResource("TabFilterButtonActiveStyle");
            var defaultStyle = (Style)FindResource("TabFilterButtonStyle");

            foreach (var (tab, button) in tabMap)
            {
                button.Style = tab == selectedTab ? activeStyle : defaultStyle;
            }
        }

        // ───────────────────────────────────────
        // 드래그 시작 감지 — ListBox에서 마우스 움직임 감지
        // PreviewMouseMove는 자식 요소보다 먼저 이벤트를 받음
        // ───────────────────────────────────────
        private void ListBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            // ListBox 안에서 누르지 않았으면 드래그 시작 안 함
            if (!_isDragReady) return;
            if (e.LeftButton != MouseButtonState.Pressed) return;

            var currentPos = e.GetPosition(null);
            var diff = currentPos - _dragStartPoint;
            bool isEnoughDistance =
                Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance;

            if (!isEnoughDistance) return;

            var listBox = sender as ListBox;
            if (listBox == null) return;

            var item = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);
            if (item == null) return;

            _draggingTask = item.DataContext as WorkTask;
            if (_draggingTask == null) return;

            _isDragReady = false; // 드래그 시작하면 플래그 초기화
            _wasDragging = true;

            DragDrop.DoDragDrop(item,
                new DataObject("WorkTask", _draggingTask.Id.ToString()),
                DragDropEffects.Move);

            _draggingTask = null;
        }

        // ───────────────────────────────────────
        // 드래그 시작 위치 기록 — PreviewMouseLeftButtonDown
        // ───────────────────────────────────────
        private void ListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 마우스를 누른 순간의 위치 저장
            // 나중에 PreviewMouseMove에서 얼마나 움직였는지 계산할 때 씀
            _dragStartPoint = e.GetPosition(null);

            _isDragReady = true;
        }

        // ───────────────────────────────────────
        // 드롭 수신 — 컬럼 StackPanel에 카드를 놓았을 때
        // ───────────────────────────────────────
        private void Column_Drop(object sender, DragEventArgs e)
        {
            // DataObject에서 Task Id 꺼내기
            if (!e.Data.GetDataPresent("WorkTask")) return;
            var taskIdStr = e.Data.GetData("WorkTask") as string;
            if (taskIdStr == null) return;

            // 드롭된 컬럼의 Tag에서 새 Status 읽기
            // Tag="할일" / "진행중" / "완료" 로 설정해뒀음
            var column = sender as StackPanel;
            var newStatus = column?.Tag as string;
            if (newStatus == null) return;

            // ViewModel의 MoveTaskCommand 호출
            // DataContext는 ProjectViewModel
            var vm = DataContext as ProjectViewModel;
            if (vm == null) return;

            // "taskId|newStatus" 형태로 전달
            vm.MoveTaskCommand.Execute($"{taskIdStr}|{newStatus}");
        }

        // ───────────────────────────────────────
        // 헬퍼 — 시각 트리에서 특정 타입의 부모 요소 찾기
        // ListBoxItem처럼 직접 접근하기 어려운 요소를 찾을 때 사용
        // ───────────────────────────────────────
        private static T? FindAncestor<T>(DependencyObject current)
            where T : DependencyObject
        {
            // VisualTreeHelper.GetParent: 시각 트리에서 부모 요소 반환
            // T 타입을 찾을 때까지 계속 올라감
            while (current != null)
            {
                if (current is T target) return target;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        // ───────────────────────────────────────
        // 마우스 뗄 때 — 드래그가 아니었으면 패널 열기
        // 드래그 중이었으면 _draggingTask가 null이 아님
        // ───────────────────────────────────────
        private void ListBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragReady = false;
            _wasDragging = false;

            // 마우스를 누른 위치와 뗀 위치의 거리 체크
            // 일정 거리 이상 움직였으면 드래그로 간주하고 패널 열지 않음
            var currentPos = e.GetPosition(null);
            var diff = currentPos - _dragStartPoint;
            bool wasDragged =
                Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance;

            if (wasDragged) return;

            if (FindAncestor<Button>((DependencyObject)e.OriginalSource) != null)
                return;

            var item = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);
            if (item?.DataContext is not WorkTask task) return;
            var vm = DataContext as ProjectViewModel;
            vm?.SelectTaskCommand.Execute(task);
        }
    }
}