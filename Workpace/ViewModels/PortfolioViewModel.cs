using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Workpace.Models;
using Workpace.Services;

namespace Workpace.ViewModels
{
    public partial class PortfolioViewModel : ObservableObject
    {
        private readonly DatabaseService _db;
        private readonly PortfolioService _portfolioService;

        // 현재 선택된 프로젝트
        [ObservableProperty]
        private Project? currentProject;

        // 수집된 포트폴리오 데이터
        private PortfolioData? _portfolioData;

        // ── 프로젝트 요약 표시용 ──────────────────
        [ObservableProperty]
        private string totalDays = "-";

        [ObservableProperty]
        private string dateRange = "-";

        [ObservableProperty]
        private string doneTasksText = "-";

        [ObservableProperty]
        private string completionRateText = "-";

        [ObservableProperty]
        private string troubleshootingText = "-";

        // ── 섹션 포함 여부 체크박스 ──────────────
        [ObservableProperty]
        private bool includeBasicInfo = true;

        [ObservableProperty]
        private bool includeOverview = true;

        [ObservableProperty]
        private bool includeTechStack = true;

        [ObservableProperty]
        private bool includeMainFeatures = true;

        [ObservableProperty]
        private bool includeTroubleshooting = true;

        [ObservableProperty]
        private bool includeResults = true;

        // ── 생성된 PDF 경로 (미리보기용) ──────────
        [ObservableProperty]
        private string? generatedPdfPath;

        public PortfolioViewModel()
        {
            _db = new DatabaseService();
            _portfolioService = new PortfolioService(_db);
        }

        // ───────────────────────────────────────
        // 프로젝트 설정 — MainViewModel에서 호출
        // 화면 전환 시 현재 프로젝트 데이터 로드
        // ───────────────────────────────────────
        public void SetProject(Project project)
        {
            CurrentProject = project;
            _portfolioData = _portfolioService.CollectData(project);

            // 요약 카드 데이터 설정
            TotalDays = $"{(project.Deadline - project.StartDate).Days}일";
            DateRange = $"{project.StartDate:yyyy.MM.dd} ~ {project.Deadline:yyyy.MM.dd}";
            DoneTasksText = $"{_portfolioData.DoneTasks}개 / 전체 {_portfolioData.TotalTasks}개";
            CompletionRateText = $"{_portfolioData.CompletionRate}%";
            TroubleshootingText = $"{_portfolioData.Issues.Count}건";

            // PDF 미리보기 자동 생성
            GeneratePreview();
        }

        // ───────────────────────────────────────
        // PDF 미리보기 생성
        // 임시 폴더에 저장 후 WebView2로 표시
        // ───────────────────────────────────────
        // ───────────────────────────────────────
        // 디바운싱용 타이머 — 마지막 체크박스 변경 후
        // 500ms 뒤에 PDF 생성 (연속 클릭 시 중복 생성 방지)
        // ───────────────────────────────────────
        private System.Threading.CancellationTokenSource? _previewCts;

        private async void GeneratePreview()
        {
            if (_portfolioData == null) return;

            // 이전 대기 중인 작업 취소
            _previewCts?.Cancel();
            _previewCts = new System.Threading.CancellationTokenSource();
            var token = _previewCts.Token;

            try
            {
                // 500ms 대기 — 연속 클릭 시 마지막 것만 실행
                await Task.Delay(500, token);

                if (token.IsCancellationRequested) return;

                var tempPath = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(),
                    $"workpace_preview_{DateTime.Now:yyyyMMddHHmmss}.pdf");

                await Task.Run(() =>
                    _portfolioService.GeneratePdfToPath(_portfolioData, tempPath,
                        IncludeBasicInfo, IncludeOverview, IncludeTechStack,
                        IncludeMainFeatures, IncludeTroubleshooting, IncludeResults),
                    token);

                if (!token.IsCancellationRequested)
                    GeneratedPdfPath = tempPath;
            }
            catch (TaskCanceledException)
            {
                // 취소된 경우 무시
            }
        }

        // ───────────────────────────────────────
        // 섹션 체크박스 변경 시 미리보기 자동 갱신
        // ───────────────────────────────────────
        partial void OnIncludeBasicInfoChanged(bool value) => GeneratePreview();
        partial void OnIncludeOverviewChanged(bool value) => GeneratePreview();
        partial void OnIncludeTechStackChanged(bool value) => GeneratePreview();
        partial void OnIncludeMainFeaturesChanged(bool value) => GeneratePreview();
        partial void OnIncludeTroubleshootingChanged(bool value) => GeneratePreview();
        partial void OnIncludeResultsChanged(bool value) => GeneratePreview();

        // ───────────────────────────────────────
        // PDF 다운로드 — 바탕화면에 저장
        // ───────────────────────────────────────
        [RelayCommand]
        private void DownloadPdf()
        {
            if (_portfolioData == null)
            {
                MessageBox.Show("프로젝트를 먼저 선택해주세요.");
                return;
            }

            try
            {
                var filePath = _portfolioService.GeneratePdf(_portfolioData,
                    IncludeBasicInfo, IncludeOverview, IncludeTechStack,
                    IncludeMainFeatures, IncludeTroubleshooting, IncludeResults);

                MessageBox.Show($"저장 완료!\n\n{filePath}",
                    "완료", MessageBoxButton.OK, MessageBoxImage.Information);

                // 탐색기에서 파일 선택
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{filePath}\""
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"PDF 생성 오류: {ex.Message}", "오류",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ───────────────────────────────────────
        // 텍스트 복사 — 클립보드에 복사
        // ───────────────────────────────────────
        [RelayCommand]
        private void CopyText()
        {
            if (_portfolioData == null) return;

            var text = _portfolioService.GenerateText(_portfolioData);
            Clipboard.SetText(text);

            MessageBox.Show("클립보드에 복사되었습니다!", "완료",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}