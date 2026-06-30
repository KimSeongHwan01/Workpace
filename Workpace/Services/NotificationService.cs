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

        // 스트릭 리마인더 — 마지막으로 알림 보낸 시각 기록
        // "주기마다" 체크하려면 마지막 발송 시각 기준으로 경과 시간을 계산해야 함
        private DateTime _lastStreakNotified = DateTime.MinValue;

        // 작업 단위 마감 알림은 앱 켤 때 한 번만 보내므로 별도 추적 불필요

        // ── 테스트 시 여기만 바꾸면 됨 ──────────────
        private const int ProjectDeadlineAlertDays = 3; // 프로젝트 마감 며칠 전부터 알림
        private const int TaskDeadlineAlertDays = 3;    // Task 마감 며칠 전부터 알림
        // ─────────────────────────────────────────

        public NotificationService(DatabaseService db)
        {
            _db = db;
            _timer = new DispatcherTimer
            {
                // 1분마다 "스트릭 리마인더 주기가 지났는지" 체크
                Interval = TimeSpan.FromMinutes(1)
                // 테스트용 — 빠르게 확인하려면 아래로 교체
                // Interval = TimeSpan.FromSeconds(5)
            };
            _timer.Tick += OnTick;
        }

        // 앱 실행 시 호출
        public void Start()
        {
            // 마감 임박 알림(프로젝트 + 작업 단위)은 앱 켜질 때 즉시 체크
            CheckProjectDeadlineAlerts();
            CheckTaskDeadlineAlerts();

            _timer.Start();
        }

        public void Stop() => _timer.Stop();

        // 1분마다 실행 — 스트릭 리마인더 주기 체크
        private void OnTick(object? sender, EventArgs e)
        {
            var profile = _db.GetUserProfile();
            if (profile == null) return;

            if (!profile.StreakReminderEnabled) return;

            var now = DateTime.Now;

            // 마지막 발송 이후 설정한 주기(시간)가 지났는지 확인
            var intervalPassed = (now - _lastStreakNotified).TotalHours >= profile.StreakReminderIntervalHours;

            if (intervalPassed)
            {
                var todayDone = _db.GetStreakDates(1)
                    .Any(d => d.Date == DateTime.Today);

                if (!todayDone)
                {
                    SendNotification("스트릭 리마인더",
                        "오늘 아직 작업을 완료하지 않았어요! 스트릭을 이어가세요.", "🔥");
                }

                // 작업을 이미 했어도, 다음 주기까지는 다시 안 체크하도록 시각 갱신
                _lastStreakNotified = now;
            }
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
                        $"'{project.Name}' 마감까지 {daysLeft}일 남았어요!", "⚠️");
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
                    $"[{projectName}] '{task.Title}' 작업 마감까지 {daysLeft}일 남았어요!", "📌");
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