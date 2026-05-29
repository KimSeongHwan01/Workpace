namespace Workpace.Models
{
    public class Project
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;   // 소프트웨어개발, 디자인, 학교과제 등
        public DateTime StartDate { get; set; }
        public DateTime Deadline { get; set; }
        public string Description { get; set; } = string.Empty;
        public string GitHubUrl { get; set; } = string.Empty;

        // 계산 프로퍼티 (DB 저장 X)
        public int DaysLeft => (Deadline - DateTime.Today).Days;
        public double TargetProgress =>
            Math.Min(100, (DateTime.Today - StartDate).TotalDays
            / (Deadline - StartDate).TotalDays * 100);
    }
}