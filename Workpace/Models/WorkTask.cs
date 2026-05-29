namespace Workpace.Models
{
    public class WorkTask
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = "할일";   // 할일, 진행중, 완료
        public string Priority { get; set; } = "보통"; // 높음, 보통, 낮음
        public DateTime? DueDate { get; set; }
        public string Stage { get; set; } = "기획";    // 기획, 설계, 개발, 테스트, 완료
        public int Progress { get; set; } = 0;
    }
}