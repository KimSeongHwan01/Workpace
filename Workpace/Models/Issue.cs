namespace Workpace.Models
{
    public class Issue
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public string Problem { get; set; } = string.Empty;
        public string Cause { get; set; } = string.Empty;
        public string Solution { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}