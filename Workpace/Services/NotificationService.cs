using Notification.Core;
using System.Windows;
using System.Windows.Threading;
using Workpace.Models;
using Workpace.Views;

namespace Workpace.Services
{
    public class NotificationService
    {
        private readonly List<CustomToast> _activeToasts = new();

        private readonly DatabaseService _db;
        private readonly DispatcherTimer _timer;

        // 스트릭 리마인더 — 오늘 알림을 이미 보냈는지 앱 실행 중 기록
        private DateTime? _lastStreakReminderDate = null;

        // 스트릭 알림 시작 시간 — 20시(기본 설정)
        // ── 테스트 시 여기만 바꾸면 됨 ──────────────
        private static readonly TimeSpan StreakReminderStartTime =
            new(hours: 16, minutes: 20, seconds: 50);

        // 테스트용 - true면 오늘 작업을 했어도 스트릭 알림 강제 표시
        private static readonly bool ForceStreakReminder = false;

        // 작업 단위 마감 알림은 앱 켤 때 한 번만 보내므로 별도 추적 불필요

        // ── 테스트 시 여기만 바꾸면 됨 ──────────────
        private const int ProjectDeadlineAlertDays = 3; // 프로젝트 마감 며칠 전부터 알림
        private const int TaskDeadlineAlertDays = 3;    // Task 마감 며칠 전부터 알림
        // ─────────────────────────────────────────

        public NotificationService(DatabaseService db)
        {
            _db = db;
            _timer = new DispatcherTimer();
            _timer.Tick += OnTick;
        }

        // 앱 실행 시 호출
        public void Start()
        {
            // 마감 임박 알림(프로젝트 + 작업 단위)은 앱 켜질 때 즉시 체크
            CheckProjectDeadlineAlerts();
            CheckTaskDeadlineAlerts();

            CheckStreakReminder();          // 이미 시간이 지난 경우 즉시 알림
            ScheduleNextStreakReminder();   // 시간이 안 지났으면 해당 시간까지 예약
        }

        public void Stop() => _timer.Stop();

        private void OnTick(object? sender, EventArgs e)
        {
            _timer.Stop();

            CheckStreakReminder();

            ScheduleNextStreakReminder();
        }

        private void ScheduleNextStreakReminder()
        {
            var now = DateTime.Now;

            var nextReminder = DateTime.Today.Add(StreakReminderStartTime);

            // 오늘 알림 시간이 이미 지났으면 내일 같은 시간으로 예약
            if (nextReminder <= now)
                nextReminder = nextReminder.AddDays(1);

            var interval = nextReminder - now;

            // 너무 짧거나 0 이하 방지
            if (interval < TimeSpan.FromSeconds(1))
                interval = TimeSpan.FromSeconds(1);

            _timer.Interval = interval;
            _timer.Start();
        }

        private void CheckStreakReminder()
        {
            var profile = _db.GetUserProfile();
            if (profile == null) return;

            if (!profile.StreakReminderEnabled) return;

            // 프로젝트가 하나도 없으면 알림 X — 작업할 게 없는 상태
            var projects = _db.GetAllProjects();
            if (projects.Count == 0) return;

            var now = DateTime.Now;

            // 아직 알림 시간이 안 됐으면 알림 X
            if (now.TimeOfDay < StreakReminderStartTime) return;

            // 오늘 이미 스트릭 알림을 보냈으면 알림 X
            if (_lastStreakReminderDate == DateTime.Today) return;

            // 테스트 모드가 아닐 때만 오늘 작업 완료 여부 확인
            if (!ForceStreakReminder)
            {
                var todayDone = _db.GetStreakDates(1)
                    .Any(d => d.Date == DateTime.Today);

                if (todayDone) return;
            }
            
            SendNotification(
                "스트릭 리마인더",
                "오늘 아직 완료한 작업이 없어요. 작은 작업 하나만 끝내고 스트릭을 이어가볼까요?",
                "IconFlame"
            );

            // 오늘은 다시 보내지 않도록 기록
            _lastStreakReminderDate = DateTime.Today;
        }

        // 프로젝트 마감 임박 체크 — 앱 켤 때 한 번
        private void CheckProjectDeadlineAlerts()
        {
            var profile = _db.GetUserProfile();
            if (profile == null || !profile.ProjectDeadlineAlertEnabled) return;

            var projects = _db.GetAllProjects();
            foreach (var project in projects)
            {
                var daysLeft = (project.Deadline.Date - DateTime.Today).Days;
                if (daysLeft >= 0 && daysLeft <= ProjectDeadlineAlertDays)
                    SendNotification("마감 임박",
                        $"'{project.Name}' 마감까지 {daysLeft}일 남았어요!", "IconAlertTriangle");
            }
        }

        // 작업(Task) 단위 마감 임박 체크 — 앱 켤 때 한 번
        private void CheckTaskDeadlineAlerts()
        {
            var profile = _db.GetUserProfile();
            if (profile == null || !profile.TaskDeadlineAlertEnabled) return;

            var tasks = _db.GetTasksWithUpcomingDueDate(TaskDeadlineAlertDays);
            var projects = _db.GetAllProjects().ToDictionary(p => p.Id, p => p.Name);

            foreach (var task in tasks)
            {
                var daysLeft = (task.DueDate!.Value.Date - DateTime.Today).Days;
                var projectName = projects.TryGetValue(task.ProjectId, out var name) ? name : "알 수 없는 프로젝트";

                SendNotification("작업 마감 임박",
                        $"[{projectName}] '{task.Title}' 작업 마감까지 {daysLeft}일 남았어요!", "IconPin");
            }
        }

        // 토스트 알림 발송
        private async void SendNotification(string title, string message, string icon = "🔔")
        {
            await Task.Delay(3000); // 3초 지연

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var toast = new CustomToast(icon, title, message);

                toast.Closed += (s, e) =>
                {
                    _activeToasts.Remove(toast);
                    RepositionToasts();
                };

                _activeToasts.Add(toast);
                toast.Show();
                RepositionToasts();
            });
        }

        // 활성 토스트들을 화면 우측 하단부터 위로 쌓기
        private void RepositionToasts()
        {
            var workArea = SystemParameters.WorkArea;
            double bottom = workArea.Bottom - 16;

            // 최근 토스트가 맨 아래, 오래된 토스트가 위로 쌓이도록
            for (int i = _activeToasts.Count - 1; i >= 0; i--)
            {
                var toast = _activeToasts[i];
                toast.Left = workArea.Right - toast.Width - 16;
                toast.Top = bottom - toast.ActualHeight;
                bottom -= toast.ActualHeight + 10; // 토스트 간 간격 10px
            }
        }
    }
}