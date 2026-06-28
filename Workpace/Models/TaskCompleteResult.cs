namespace Workpace.Models
{
    /// <summary>
    /// Task 완료 팝업에서 사용자가 입력한 결과를 담는 모델
    /// 팝업 → ViewModel로 데이터를 전달하는 용도
    /// </summary>
    public class TaskCompleteResult
    {
        // 이슈 입력 여부 (비어있으면 false)
        public bool HasIssue => !string.IsNullOrWhiteSpace(Problem);

        // 이슈 내용
        public string Problem { get; set; } = string.Empty;
        public string Cause { get; set; } = string.Empty;
        public string Solution { get; set; } = string.Empty;

        // 선택한 커밋 타입 (feat, fix, refactor, docs, chore)
        public string CommitType { get; set; } = "feat";

        // 최종 커밋 메시지 (CommitType: TaskTitle 형태)
        public string CommitMessage { get; set; } = string.Empty;
    }
}