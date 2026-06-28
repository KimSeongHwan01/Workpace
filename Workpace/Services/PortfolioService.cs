using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Shapes;
using MigraDoc.Rendering;
using PdfSharp.Pdf;
using Workpace.Models;

namespace Workpace.Services
{
    public class PortfolioService
    {
        private readonly DatabaseService _db;

        public PortfolioService(DatabaseService db)
        {
            _db = db;
        }

        // ───────────────────────────────────────
        // 포트폴리오 데이터 수집
        // PDF 생성과 텍스트 복사 둘 다 여기서 데이터를 가져감
        // ───────────────────────────────────────
        public PortfolioData CollectData(Project project)
        {
            var tasks = _db.GetTasksByProject(project.Id);
            var doneTasks = tasks.Where(t => t.Status == "완료").ToList();
            var allIssues = new List<Issue>();
            foreach (var task in tasks)
                allIssues.AddRange(_db.GetIssuesByTask(task.Id));

            // 일정 준수율 계산
            // 현재 진행률이 목표 진행률보다 높거나 같으면 준수한 것
            var totalDays = (project.Deadline - project.StartDate).Days;
            var elapsedDays = Math.Min((DateTime.Today - project.StartDate).Days, totalDays);
            var targetProgress = totalDays > 0
                ? Math.Round((double)elapsedDays / totalDays * 100, 1)
                : 0;
            var currentProgress = tasks.Count > 0
                ? Math.Round((double)doneTasks.Count / tasks.Count * 100, 1)
                : 0;
            var scheduleAdherence = currentProgress >= targetProgress
                ? 100.0
                : Math.Round(currentProgress / targetProgress * 100, 1);

            // 완료 Task별 연결 파일 수집
            var taskFiles = new Dictionary<int, List<TaskFile>>();
            foreach (var task in doneTasks)
            {
                var files = _db.GetFilesByTask(task.Id);
                if (files.Count > 0)
                    taskFiles[task.Id] = files;
            }

            return new PortfolioData
            {
                Project = project,
                UserProfile = _db.GetUserProfile(),
                TotalTasks = tasks.Count,
                DoneTasks = doneTasks.Count,
                CompletionRate = tasks.Count > 0
                    ? Math.Round((double)doneTasks.Count / tasks.Count * 100, 1)
                    : 0,
                Issues = allIssues,
                DoneTaskList = doneTasks,
                ScheduleAdherence = scheduleAdherence,
                TaskFiles = taskFiles
            };
        }

        // ───────────────────────────────────────
        // PDF 생성 — MigraDoc으로 문서 구성 후 PdfSharp으로 저장
        // 섹션 포함 여부는 IncludeXxx 플래그로 제어
        // ───────────────────────────────────────
        public string GeneratePdf(PortfolioData data,
            bool includeBasicInfo = true,
            bool includeOverview = true,
            bool includeTechStack = true,
            bool includeMainFeatures = true,
            bool includeTroubleshooting = true,
            bool includeResults = true)
        {
            var desktopPath = Environment.GetFolderPath(
                Environment.SpecialFolder.DesktopDirectory);
            var fileName = $"{data.Project.Name}_포트폴리오_{DateTime.Now:yyyyMMdd}.pdf";
            var filePath = System.IO.Path.Combine(desktopPath, fileName);

            var document = BuildDocument(data, includeBasicInfo, includeOverview,
                includeTechStack, includeMainFeatures, includeTroubleshooting, includeResults);

            var renderer = new PdfDocumentRenderer();
            renderer.Document = document;
            renderer.RenderDocument();
            renderer.PdfDocument.Save(filePath);

            return filePath;
        }

        // ───────────────────────────────────────
        // 공통 문서 빌드 로직
        // ───────────────────────────────────────
        private Document BuildDocument(PortfolioData data,
            bool includeBasicInfo, bool includeOverview, bool includeTechStack,
            bool includeMainFeatures, bool includeTroubleshooting, bool includeResults)
        {
            var document = new Document();
            document.Info.Title = data.Project.Name;

            var style = document.Styles["Normal"]!;
            style.Font.Name = "Malgun Gothic";
            style.Font.Size = 10;

            var section = document.AddSection();
            section.PageSetup.TopMargin = Unit.FromCentimeter(2);
            section.PageSetup.BottomMargin = Unit.FromCentimeter(2);
            section.PageSetup.LeftMargin = Unit.FromCentimeter(2.5);
            section.PageSetup.RightMargin = Unit.FromCentimeter(2.5);

            // ── 표지 — 프로젝트명 + 개발자 프로필 ──────────
            var title = section.AddParagraph(data.Project.Name);
            title.Format.Font.Size = 24;
            title.Format.Font.Bold = true;
            title.Format.SpaceAfter = 6;

            // 부제 — Description이 있으면 표시, 없으면 기본 문구
            var subtitleText = string.IsNullOrWhiteSpace(data.Project.Description)
                ? "개인 프로젝트 포트폴리오"
                : data.Project.Description;
            var subtitle = section.AddParagraph(subtitleText);
            subtitle.Format.Font.Size = 12;
            subtitle.Format.Font.Color = Colors.Gray;
            subtitle.Format.SpaceAfter = 12;

            // 개발자 프로필 — UserProfile이 있을 때만 표시
            if (data.UserProfile != null)
            {
                if (!string.IsNullOrWhiteSpace(data.UserProfile.Name))
                    AddBody(section, $"👤 {data.UserProfile.Name}");
                if (!string.IsNullOrWhiteSpace(data.UserProfile.Email))
                    AddBody(section, $"✉ {data.UserProfile.Email}");
                if (!string.IsNullOrWhiteSpace(data.UserProfile.GitHub))
                    AddBody(section, $"🔗 GitHub: {data.UserProfile.GitHub}");
                if (!string.IsNullOrWhiteSpace(data.UserProfile.Blog))
                    AddBody(section, $"📝 Blog: {data.UserProfile.Blog}");
                if (!string.IsNullOrWhiteSpace(data.UserProfile.Bio))
                {
                    var bio = section.AddParagraph(data.UserProfile.Bio);
                    bio.Format.Font.Italic = true;
                    bio.Format.Font.Color = Colors.Gray;
                    bio.Format.SpaceAfter = 4;
                }
            }

            // 구분선
            var line = section.AddParagraph("─".PadRight(60, '─'));
            line.Format.Font.Color = Colors.LightGray;
            line.Format.SpaceAfter = 16;

            // ── 프로젝트 기본 정보 ──────────────────────────
            if (includeBasicInfo)
            {
                AddHeading(section, "📋 프로젝트 기본 정보");
                AddBody(section, $"기간: {data.Project.StartDate:yyyy-MM-dd} ~ {data.Project.Deadline:yyyy-MM-dd}");
                AddBody(section, $"총 기간: {(data.Project.Deadline - data.Project.StartDate).Days}일");
                AddBody(section, $"프로젝트 유형: {data.Project.Type}");
                if (!string.IsNullOrWhiteSpace(data.Project.GitHubUrl))
                    AddBody(section, $"GitHub: {data.Project.GitHubUrl}");

                // 역할 — Role 필드가 있을 때만
                if (!string.IsNullOrWhiteSpace(data.Project.Role))
                    AddBody(section, $"역할: {data.Project.Role}");

                // 아키텍처 — Architecture 필드가 있을 때만
                if (!string.IsNullOrWhiteSpace(data.Project.Architecture))
                    AddBody(section, $"아키텍처: {data.Project.Architecture}");
            }

            // ── 프로젝트 개요 ───────────────────────────────
            if (includeOverview)
            {
                AddHeading(section, "📌 프로젝트 개요");
                AddBody(section, string.IsNullOrWhiteSpace(data.Project.Background)
                    ? "개발 배경이 입력되지 않았습니다."
                    : data.Project.Background);
            }

            // ── 기술 스택 + 선정 이유 ───────────────────────
            if (includeTechStack)
            {
                AddHeading(section, "🛠 기술 스택");

                // Type 필드에 기술스택이 콤마로 저장되어 있으면 항목별로 분리
                // 예: "C#, WPF, SQLite" → 각각 bullet으로 표시
                if (!string.IsNullOrWhiteSpace(data.Project.Type))
                {
                    var techs = data.Project.Type.Split(',')
                        .Select(t => t.Trim())
                        .Where(t => !string.IsNullOrEmpty(t));
                    foreach (var tech in techs)
                        AddBody(section, $"• {tech}");
                }

                // 기술 스택 선정 이유 — TechReason이 있을 때만
                if (!string.IsNullOrWhiteSpace(data.Project.TechReason))
                {
                    var reasonLabel = section.AddParagraph("선정 이유");
                    reasonLabel.Format.Font.Bold = true;
                    reasonLabel.Format.SpaceBefore = 8;
                    reasonLabel.Format.SpaceAfter = 4;
                    AddBody(section, data.Project.TechReason);
                }
            }

            // ── 주요 기능 — Stage별로 묶어서 표시 ───────────
            if (includeMainFeatures)
            {
                AddHeading(section, "⚡ 주요 기능");

                if (data.DoneTaskList.Count == 0)
                {
                    AddBody(section, "완료된 작업이 없습니다.");
                }
                else
                {
                    // Stage 순서 정의 — 기획부터 완료 순으로 정렬
                    var stageOrder = new[] { "기획", "설계", "개발", "테스트", "완료" };

                    // Stage별로 그룹핑
                    var grouped = data.DoneTaskList
                        .GroupBy(t => t.Stage)
                        .OrderBy(g => Array.IndexOf(stageOrder, g.Key));

                    foreach (var group in grouped)
                    {
                        // Stage 소제목
                        var stageLabel = section.AddParagraph($"[ {group.Key} ]");
                        stageLabel.Format.Font.Bold = true;
                        stageLabel.Format.SpaceBefore = 8;
                        stageLabel.Format.SpaceAfter = 4;

                        foreach (var task in group)
                        {
                            AddBody(section, $"  • {task.Title}");

                            // 연결된 파일이 있으면 표시
                            if (data.TaskFiles.TryGetValue(task.Id, out var files))
                            {
                                var imageExts = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };

                                foreach (var file in files)
                                {
                                    var ext = System.IO.Path.GetExtension(file.FilePath).ToLower();

                                    if (imageExts.Contains(ext) && System.IO.File.Exists(file.FilePath))
                                    {
                                        // 이미지 파일 → PDF에 직접 삽입
                                        try
                                        {
                                            var image = section.AddImage(file.FilePath);
                                            image.Width = Unit.FromCentimeter(12); // 최대 너비 12cm
                                            image.LockAspectRatio = true;          // 비율 유지
                                            section.AddParagraph("").Format.SpaceAfter = 4;
                                        }
                                        catch
                                        {
                                            // 이미지 삽입 실패 시 파일명만 표시
                                            AddBody(section, $"    📎 {file.FileName}");
                                        }
                                    }
                                    else
                                    {
                                        // 이미지 외 파일 → 파일명 + 경로 텍스트로 표시
                                        AddBody(section, $"    📎 {file.FileName}");
                                        AddBody(section, $"       {file.FilePath}");
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // ── 트러블슈팅 — Before & After 구조 ───────────
            if (includeTroubleshooting && data.Issues.Count > 0)
            {
                AddHeading(section, "🔧 트러블슈팅");

                foreach (var issue in data.Issues)
                {
                    // 문제 제목
                    var issueTitle = section.AddParagraph($"▸ {issue.Problem}");
                    issueTitle.Format.Font.Bold = true;
                    issueTitle.Format.SpaceBefore = 10;
                    issueTitle.Format.SpaceAfter = 4;

                    if (!string.IsNullOrWhiteSpace(issue.Cause))
                        AddBody(section, $"  원인: {issue.Cause}");
                    if (!string.IsNullOrWhiteSpace(issue.Solution))
                        AddBody(section, $"  해결: {issue.Solution}");

                    // Result — 성과/배운점이 있을 때만 표시
                    if (!string.IsNullOrWhiteSpace(issue.Result))
                    {
                        var result = section.AddParagraph($"  ✅ 성과: {issue.Result}");
                        result.Format.Font.Color = new Color(16, 185, 129); // 초록색
                        result.Format.SpaceAfter = 4;
                    }
                }
            }

            // ── 프로젝트 성과 ───────────────────────────────
            if (includeResults)
            {
                AddHeading(section, "📊 프로젝트 성과");
                AddBody(section, $"• 전체 작업: {data.TotalTasks}개");
                AddBody(section, $"• 완료 작업: {data.DoneTasks}개");
                AddBody(section, $"• 완료율: {data.CompletionRate}%");
                AddBody(section, $"• 일정 준수율: {data.ScheduleAdherence}%");
                AddBody(section, $"• 트러블슈팅 해결: {data.Issues.Count}건");
            }

            // ── 회고 — 프로젝트 완료 시 작성한 내용 자동 반영 ──
            if (!string.IsNullOrWhiteSpace(data.Project.RetrospectLearn) ||
                !string.IsNullOrWhiteSpace(data.Project.RetrospectRegret) ||
                !string.IsNullOrWhiteSpace(data.Project.RetrospectImprove))
            {
                AddHeading(section, "💭 회고");

                if (!string.IsNullOrWhiteSpace(data.Project.RetrospectLearn))
                {
                    var learnLabel = section.AddParagraph("배운 점");
                    learnLabel.Format.Font.Bold = true;
                    learnLabel.Format.SpaceBefore = 6;
                    learnLabel.Format.SpaceAfter = 4;
                    AddBody(section, data.Project.RetrospectLearn);
                }

                if (!string.IsNullOrWhiteSpace(data.Project.RetrospectRegret))
                {
                    var regretLabel = section.AddParagraph("아쉬운 점");
                    regretLabel.Format.Font.Bold = true;
                    regretLabel.Format.SpaceBefore = 6;
                    regretLabel.Format.SpaceAfter = 4;
                    AddBody(section, data.Project.RetrospectRegret);
                }

                if (!string.IsNullOrWhiteSpace(data.Project.RetrospectImprove))
                {
                    var improveLabel = section.AddParagraph("개선 방향");
                    improveLabel.Format.Font.Bold = true;
                    improveLabel.Format.SpaceBefore = 6;
                    improveLabel.Format.SpaceAfter = 4;
                    AddBody(section, data.Project.RetrospectImprove);
                }
            }

            return document;
        }

        // ───────────────────────────────────────
        // 텍스트 복사용 문자열 생성
        // ───────────────────────────────────────
        public string GenerateText(PortfolioData data)
        {
            var sb = new System.Text.StringBuilder();

            // ── 표지 ────────────────────────────────────────
            sb.AppendLine(data.Project.Name);
            if (!string.IsNullOrWhiteSpace(data.Project.Description))
                sb.AppendLine(data.Project.Description);
            sb.AppendLine("=".PadRight(50, '='));
            sb.AppendLine();

            // 개발자 프로필
            if (data.UserProfile != null)
            {
                if (!string.IsNullOrWhiteSpace(data.UserProfile.Name))
                    sb.AppendLine($"👤 {data.UserProfile.Name}");
                if (!string.IsNullOrWhiteSpace(data.UserProfile.Email))
                    sb.AppendLine($"✉ {data.UserProfile.Email}");
                if (!string.IsNullOrWhiteSpace(data.UserProfile.GitHub))
                    sb.AppendLine($"GitHub: {data.UserProfile.GitHub}");
                if (!string.IsNullOrWhiteSpace(data.UserProfile.Blog))
                    sb.AppendLine($"Blog: {data.UserProfile.Blog}");
                if (!string.IsNullOrWhiteSpace(data.UserProfile.Bio))
                    sb.AppendLine(data.UserProfile.Bio);
                sb.AppendLine();
            }

            // ── 프로젝트 기본 정보 ──────────────────────────
            sb.AppendLine("[프로젝트 기본 정보]");
            sb.AppendLine($"기간: {data.Project.StartDate:yyyy-MM-dd} ~ {data.Project.Deadline:yyyy-MM-dd}");
            sb.AppendLine($"총 기간: {(data.Project.Deadline - data.Project.StartDate).Days}일");
            sb.AppendLine($"프로젝트 유형: {data.Project.Type}");
            if (!string.IsNullOrWhiteSpace(data.Project.GitHubUrl))
                sb.AppendLine($"GitHub: {data.Project.GitHubUrl}");
            if (!string.IsNullOrWhiteSpace(data.Project.Role))
                sb.AppendLine($"역할: {data.Project.Role}");
            if (!string.IsNullOrWhiteSpace(data.Project.Architecture))
                sb.AppendLine($"아키텍처: {data.Project.Architecture}");
            sb.AppendLine();

            // ── 프로젝트 개요 ───────────────────────────────
            sb.AppendLine("[프로젝트 개요]");
            sb.AppendLine(string.IsNullOrWhiteSpace(data.Project.Background)
                ? "개발 배경이 입력되지 않았습니다."
                : data.Project.Background);
            sb.AppendLine();

            // ── 기술 스택 + 선정 이유 ───────────────────────
            sb.AppendLine("[기술 스택]");
            if (!string.IsNullOrWhiteSpace(data.Project.Type))
            {
                var techs = data.Project.Type.Split(',')
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrEmpty(t));
                foreach (var tech in techs)
                    sb.AppendLine($"• {tech}");
            }
            if (!string.IsNullOrWhiteSpace(data.Project.TechReason))
            {
                sb.AppendLine();
                sb.AppendLine("선정 이유:");
                sb.AppendLine(data.Project.TechReason);
            }
            sb.AppendLine();

            // ── 주요 기능 — Stage별 분류 ────────────────────
            sb.AppendLine("[주요 기능]");
            if (data.DoneTaskList.Count == 0)
            {
                sb.AppendLine("완료된 작업이 없습니다.");
            }
            else
            {
                var stageOrder = new[] { "기획", "설계", "개발", "테스트", "완료" };
                var grouped = data.DoneTaskList
                    .GroupBy(t => t.Stage)
                    .OrderBy(g => Array.IndexOf(stageOrder, g.Key));

                foreach (var group in grouped)
                {
                    sb.AppendLine($"[ {group.Key} ]");
                    foreach (var task in group)
                        sb.AppendLine($"  • {task.Title}");
                    sb.AppendLine();
                }
            }

            // ── 트러블슈팅 ──────────────────────────────────
            if (data.Issues.Count > 0)
            {
                sb.AppendLine("[트러블슈팅]");
                foreach (var issue in data.Issues)
                {
                    sb.AppendLine($"▸ {issue.Problem}");
                    if (!string.IsNullOrWhiteSpace(issue.Cause))
                        sb.AppendLine($"  원인: {issue.Cause}");
                    if (!string.IsNullOrWhiteSpace(issue.Solution))
                        sb.AppendLine($"  해결: {issue.Solution}");
                    if (!string.IsNullOrWhiteSpace(issue.Result))
                        sb.AppendLine($"  ✅ 성과: {issue.Result}");
                    sb.AppendLine();
                }
            }

            // ── 프로젝트 성과 ───────────────────────────────
            sb.AppendLine("[프로젝트 성과]");
            sb.AppendLine($"• 전체 작업: {data.TotalTasks}개");
            sb.AppendLine($"• 완료 작업: {data.DoneTasks}개");
            sb.AppendLine($"• 완료율: {data.CompletionRate}%");
            sb.AppendLine($"• 일정 준수율: {data.ScheduleAdherence}%");
            sb.AppendLine($"• 트러블슈팅 해결: {data.Issues.Count}건");

            // ── 회고 ────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(data.Project.RetrospectLearn) ||
                !string.IsNullOrWhiteSpace(data.Project.RetrospectRegret) ||
                !string.IsNullOrWhiteSpace(data.Project.RetrospectImprove))
            {
                sb.AppendLine();
                sb.AppendLine("[회고]");

                if (!string.IsNullOrWhiteSpace(data.Project.RetrospectLearn))
                {
                    sb.AppendLine("배운 점:");
                    sb.AppendLine(data.Project.RetrospectLearn);
                    sb.AppendLine();
                }

                if (!string.IsNullOrWhiteSpace(data.Project.RetrospectRegret))
                {
                    sb.AppendLine("아쉬운 점:");
                    sb.AppendLine(data.Project.RetrospectRegret);
                    sb.AppendLine();
                }

                if (!string.IsNullOrWhiteSpace(data.Project.RetrospectImprove))
                {
                    sb.AppendLine("개선 방향:");
                    sb.AppendLine(data.Project.RetrospectImprove);
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        // ───────────────────────────────────────
        // 헬퍼 — 섹션 제목 추가
        // ───────────────────────────────────────
        private void AddHeading(Section section, string text)
        {
            var para = section.AddParagraph(text);
            para.Format.Font.Size = 14;
            para.Format.Font.Bold = true;
            para.Format.SpaceBefore = 16;
            para.Format.SpaceAfter = 6;
        }

        // ───────────────────────────────────────
        // 헬퍼 — 본문 텍스트 추가
        // ───────────────────────────────────────
        private void AddBody(Section section, string text)
        {
            var para = section.AddParagraph(text);
            para.Format.SpaceAfter = 4;
        }

        // ───────────────────────────────────────
        // PDF 생성 — 경로 지정 버전 (미리보기용)
        // ───────────────────────────────────────
        public void GeneratePdfToPath(PortfolioData data, string filePath,
            bool includeBasicInfo = true,
            bool includeOverview = true,
            bool includeTechStack = true,
            bool includeMainFeatures = true,
            bool includeTroubleshooting = true,
            bool includeResults = true)
        {
            // GeneratePdf()와 동일한 로직, 경로만 다름
            var document = BuildDocument(data, includeBasicInfo, includeOverview,
                includeTechStack, includeMainFeatures, includeTroubleshooting, includeResults);

            var renderer = new PdfDocumentRenderer();
            renderer.Document = document;
            renderer.RenderDocument();
            renderer.PdfDocument.Save(filePath);
        }
    }

    // ───────────────────────────────────────
    // 포트폴리오 데이터 컨테이너
    // CollectData()에서 수집한 데이터를 담아서 전달
    // ───────────────────────────────────────
    public class PortfolioData
    {
        public Project Project { get; set; } = null!;

        // 개발자 프로필 — 설정 화면에서 입력한 정보
        public UserProfile? UserProfile { get; set; }

        public int TotalTasks { get; set; }
        public int DoneTasks { get; set; }
        public double CompletionRate { get; set; }

        // 일정 준수율 — 현재 진행률 / 목표 진행률 × 100
        public double ScheduleAdherence { get; set; }

        public List<Issue> Issues { get; set; } = new();
        public List<WorkTask> DoneTaskList { get; set; } = new();

        // 완료 Task별 연결 파일 목록 (TaskId → 파일 목록)
        public Dictionary<int, List<TaskFile>> TaskFiles { get; set; } = new();
    }
}