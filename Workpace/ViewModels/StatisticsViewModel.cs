using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.ObjectModel;
using Workpace.Models;
using Workpace.Services;

namespace Workpace.ViewModels
{
    public partial class StatisticsViewModel : ObservableObject
    {
        private readonly DatabaseService _db;

        // ── 프로젝트 선택 드롭다운 ────────────────────
        // 전체 프로젝트 목록
        [ObservableProperty]
        private ObservableCollection<Project> projects = new();

        // 현재 선택된 프로젝트
        [ObservableProperty]
        private Project? selectedProject;

        // ── 요약 카드 ─────────────────────────────────
        [ObservableProperty]
        private int doneTaskCount;

        [ObservableProperty]
        private int inProgressTaskCount;

        [ObservableProperty]
        private int totalDays;

        // ── 막대그래프 ────────────────────────────────
        // LiveCharts2 — ISeries 배열로 바인딩
        [ObservableProperty]
        private ISeries[] weeklySeries = Array.Empty<ISeries>();

        // X축 라벨 (월~일)
        [ObservableProperty]
        private Axis[] weeklyXAxes = Array.Empty<Axis>();

        // ── 단계별 진행률 ─────────────────────────────
        // 각 Stage의 진행률 (0~100)
        [ObservableProperty]
        private double planProgress;
        [ObservableProperty]
        private double designProgress;
        [ObservableProperty]
        private double devProgress;
        [ObservableProperty]
        private double testProgress;
        [ObservableProperty]
        private double doneProgress;

        // ── 히트맵 ────────────────────────────────────
        // 히트맵 셀 목록 — UniformGrid에 바인딩
        [ObservableProperty]
        private ObservableCollection<HeatmapCell> heatmapCells = new();

        // ── 스트릭 ────────────────────────────────────
        [ObservableProperty]
        private int currentStreak;

        [ObservableProperty]
        private int bestStreak;

        [ObservableProperty]
        private ObservableCollection<MonthLabel> monthLabels = new();

        // Canvas 크기 — 셀 14px 기준
        [ObservableProperty]
        private double heatmapWidth;

        [ObservableProperty]
        private double heatmapHeight = 7 * 14; // 7행 × 14px

        public StatisticsViewModel()
        {
            _db = new DatabaseService();
            LoadProjects();
        }

        // ───────────────────────────────────────
        // 프로젝트 목록 로드
        // ───────────────────────────────────────
        private void LoadProjects()
        {
            var list = _db.GetAllProjects();
            foreach (var p in list)
                Projects.Add(p);

            // 첫 번째 프로젝트 자동 선택
            if (Projects.Count > 0)
                SelectedProject = Projects[0];
        }

        // ───────────────────────────────────────
        // SelectedProject가 바뀌면 자동 호출
        // 모든 통계 데이터 갱신
        // ───────────────────────────────────────
        partial void OnSelectedProjectChanged(Project? value)
        {
            if (value == null) return;
            LoadStatistics(value);
        }

        // ───────────────────────────────────────
        // 전체 통계 로드
        // ───────────────────────────────────────
        private void LoadStatistics(Project project)
        {
            var allTasks = _db.GetTasksByProject(project.Id);

            // ── 요약 카드 ──────────────────────────────
            DoneTaskCount = allTasks.Count(t => t.Status == "완료");
            InProgressTaskCount = allTasks.Count(t => t.Status == "진행중");
            TotalDays = (project.Deadline - project.StartDate).Days;

            // ── 막대그래프 ─────────────────────────────
            LoadWeeklyChart(project.Id);

            // ── 단계별 진행률 ──────────────────────────
            LoadStageProgress(project.Id);

            // ── 히트맵 ─────────────────────────────────
            LoadHeatmap();

            // ── 스트릭 ─────────────────────────────────
            CurrentStreak = _db.GetCurrentStreak();
            BestStreak = GetBestStreak();
        }

        // ───────────────────────────────────────
        // 이번 주 막대그래프 데이터 로드
        // ───────────────────────────────────────
        private void LoadWeeklyChart(int projectId)
        {
            var weekly = _db.GetWeeklyCompletedTasks(projectId);
            var days = new[] { "월", "화", "수", "목", "금", "토", "일" };
            var values = days.Select(d => (double)weekly[d]).ToArray();

            // LiveCharts2 막대그래프 시리즈 생성
            WeeklySeries = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Values = values,
                    Fill = new SolidColorPaint(SKColor.Parse("#7C3AED")),
                    Stroke = null,
                    Rx = 4, // 모서리 둥글게
                    Ry = 4,
                    Name = "완료 작업"
                }
            };

            // X축 라벨 설정
            WeeklyXAxes = new Axis[]
            {
                new Axis
                {
                    Labels = days,
                    LabelsPaint = new SolidColorPaint(SKColor.Parse("#6B7280")),
                    TicksPaint = null,
                    SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#F3F4F6"))
                }
            };
        }

        // ───────────────────────────────────────
        // 단계별 진행률 로드
        // ───────────────────────────────────────
        private void LoadStageProgress(int projectId)
        {
            var progress = _db.GetStageProgress(projectId);
            PlanProgress = progress["기획"];
            DesignProgress = progress["설계"];
            DevProgress = progress["개발"];
            TestProgress = progress["테스트"];
            DoneProgress = progress["완료"];
        }

        // ───────────────────────────────────────
        // 히트맵 데이터 로드
        // 최근 1년(365일)을 월/화/수/목/금/토/일 7행으로 표시
        // ───────────────────────────────────────
        private void LoadHeatmap()
        {
            HeatmapCells.Clear();
            MonthLabels.Clear();

            var streakDates = _db.GetStreakDates(365).ToHashSet();

            // 365일 전에서 가장 가까운 월요일로 시작
            var today = DateTime.Today;
            var start = today.AddDays(-364);
            // 월요일로 맞추기
            var dow = (int)start.DayOfWeek;
            start = start.AddDays(dow == 0 ? -6 : -(dow - 1));

            // 전체 주 수 계산
            var totalWeeks = (int)Math.Ceiling((today - start).TotalDays / 7) + 1;

            int? lastMonth = null;
            for (int col = 0; col < totalWeeks; col++)
            {
                var weekStart = start.AddDays(col * 7);
                if (weekStart.Month != lastMonth)
                {
                    MonthLabels.Add(new MonthLabel
                    {
                        Text = $"{weekStart.Month}월",
                        ColIndex = col
                    });
                    lastMonth = weekStart.Month;
                }
            }

            HeatmapWidth = totalWeeks * 14;

            // 셀 생성 — 행(요일) × 열(주)
            for (int col = 0; col < totalWeeks; col++)
            {
                for (int row = 0; row < 7; row++)
                {
                    var date = start.AddDays(col * 7 + row);
                    if (date > today) continue; // 미래 날짜는 표시 안 함

                    HeatmapCells.Add(new HeatmapCell
                    {
                        Date = date,
                        HasActivity = streakDates.Contains(date),
                        Row = row,
                        Col = col
                    });
                }
            }
        }

        // ───────────────────────────────────────
        // 최고 스트릭 계산
        // 전체 Streaks 날짜에서 가장 긴 연속 구간 찾기
        // ───────────────────────────────────────
        private int GetBestStreak()
        {
            var dates = _db.GetStreakDates(365)
                .OrderBy(d => d)
                .ToList();

            if (dates.Count == 0) return 0;

            int best = 1, current = 1;

            for (int i = 1; i < dates.Count; i++)
            {
                if ((dates[i] - dates[i - 1]).Days == 1)
                {
                    current++;
                    best = Math.Max(best, current);
                }
                else
                {
                    current = 1;
                }
            }

            return best;
        }

        // ───────────────────────────────────────
        // 통계 화면 진입 시 프로젝트 목록 갱신
        // 새 프로젝트가 추가됐을 수 있으니 매번 다시 로드
        // ───────────────────────────────────────
        public void RefreshProjects()
        {
            var current = SelectedProject?.Id;
            Projects.Clear();

            var list = _db.GetAllProjects();
            foreach (var p in list)
                Projects.Add(p);

            // 이전에 선택했던 프로젝트 유지, 없으면 첫 번째 선택
            SelectedProject = Projects.FirstOrDefault(p => p.Id == current)
                ?? Projects.FirstOrDefault();
        }
    }

    // ───────────────────────────────────────
    // 히트맵 셀 데이터 모델
    // 행(요일 0~6), 열(주차) 정보 포함
    // ───────────────────────────────────────
    public class HeatmapCell
    {
        public DateTime Date { get; set; }
        public bool HasActivity { get; set; }
        public int Row { get; set; }  // 0=월 ~ 6=일
        public int Col { get; set; }  // 0=첫째주 ~
    }

    // ───────────────────────────────────────
    // 월 헤더 데이터 모델
    // ───────────────────────────────────────
    public class MonthLabel
    {
        public string Text { get; set; } = "";
        public int ColIndex { get; set; }
    }
}