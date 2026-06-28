namespace Workpace.Models
{
    // 사용자 프로필 — 딱 한 번 입력하면 모든 포트폴리오에 자동 반영
    public class UserProfile
    {
        public int Id { get; set; }

        // 이름 — 포트폴리오 표지에 표시
        public string Name { get; set; } = string.Empty;

        // 이메일
        public string Email { get; set; } = string.Empty;

        // GitHub 프로필 URL
        public string GitHub { get; set; } = string.Empty;

        // 블로그 URL
        public string Blog { get; set; } = string.Empty;

        // LinkedIn URL
        public string LinkedIn { get; set; } = string.Empty;

        // 한 줄 자기소개
        public string Bio { get; set; } = string.Empty;
    }
}