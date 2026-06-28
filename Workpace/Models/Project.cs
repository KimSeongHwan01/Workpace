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
        public string Background { get; set; } = string.Empty;
        // 포트폴리오 강화 필드
        // 기술 스택을 왜 선택했는지 — "C#은 WPF와 호환성이 좋아서"
        public string TechReason { get; set; } = string.Empty;

        // 참여 인원 및 본인 역할 — "1인 개발 / 기획·설계·개발 전담"
        public string Role { get; set; } = string.Empty;

        // 아키텍처 설명 — "MVVM 패턴, SQLite 로컬 저장"
        public string Architecture { get; set; } = string.Empty;

        // 회고 — 프로젝트 완료 시 딱 한 번 작성
        // 포트폴리오 마지막 섹션에 자동 반영됨
        public string RetrospectLearn { get; set; } = string.Empty;    // 배운 점
        public string RetrospectRegret { get; set; } = string.Empty;   // 아쉬운 점
        public string RetrospectImprove { get; set; } = string.Empty;  // 개선 방향

        // 계산 프로퍼티 (DB 저장 X)
        public int DaysLeft { get { return (Deadline - DateTime.Today).Days; } }
        public double TargetProgress
        {
            get
            {
                return Math.Min(100, (DateTime.Today - StartDate).TotalDays
                    / (Deadline - StartDate).TotalDays * 100);
            }
        }
    }
}