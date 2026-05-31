using CommunityToolkit.Mvvm.Messaging.Messages;
using Workpace.Models;

namespace Workpace.Messages
{
    // ValueChangedMessage<T> — CommunityToolkit이 제공하는 메시지 기본 클래스
    // T 자리에 전달할 데이터 타입을 넣으면 됨
    // 여기서는 선택된 Project를 전달할 거라서 T = Project?
    public class ProjectSelectedMessage : ValueChangedMessage<Project?>
    {
        // base(value) — 부모 클래스인 ValueChangedMessage에 값을 넘겨줌
        // 이렇게 하면 나중에 message.Value로 Project를 꺼낼 수 있음
        public ProjectSelectedMessage(Project? value) : base(value) { }
    }
}