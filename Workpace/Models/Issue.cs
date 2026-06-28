namespace Workpace.Models
{
    public class Issue
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public string Problem { get; set; } = string.Empty;
        public string Cause { get; set; } = string.Empty;
        public string Solution { get; set; } = string.Empty;
        // 해결 결과 및 성과 수치
        // "로딩 속도 30% 개선", "API 응답 시간 1.2초 → 0.3초"처럼
        // 숫자로 증명 가능한 성과를 적는 필드
        // 포트폴리오에서 Before & After 형식으로 자동 표시됨
        public string Result { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}