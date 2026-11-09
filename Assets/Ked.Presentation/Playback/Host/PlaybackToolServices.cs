using Yarn.Unity;

/// <summary>
/// 개발 도구가 받는 재생 안쪽 부품.
///
/// PresentationRuntime은 이것들을 내놓지 않는다(그 클래스 주석 — 롤백 기록 같은 안쪽 부품은 호스트에 보이지 않는다).
/// 개발 도구만 조립 루트에서 직접 받는다 — 등가성 하네스가 Initialize로 받는 것과 같은 길이다.
/// 지금은 쓰는 쪽(녹화기)이 보는 넷만 담는다. 하네스를 이 훅으로 옮길 때 그가 받는 나머지
/// (session · stage · shot · stageRigs · motions · 대본 폴더)를 더한다.
/// </summary>
public sealed class PlaybackToolServices
{
    public PlaybackToolServices(
        DialogueRunner dialogueRunner,
        DialogueAdvanceDispatcher advanceDispatcher,
        LinePresentationFlow lineFlow,
        RollbackHistory rollbackHistory)
    {
        DialogueRunner = dialogueRunner;
        AdvanceDispatcher = advanceDispatcher;
        LineFlow = lineFlow;
        RollbackHistory = rollbackHistory;
    }

    public DialogueRunner DialogueRunner { get; }

    /// <summary>넘김 — 사람 입력 · 자동 진행 · 건너뛰기와 같은 길.</summary>
    public DialogueAdvanceDispatcher AdvanceDispatcher { get; }

    /// <summary>LineDisplayed = 줄의 글자가 다 나온 순간(LinePresentationPhase.DisplayCommitted).</summary>
    public LinePresentationFlow LineFlow { get; }

    /// <summary>Points[^1].lineId = 지금 줄의 ID — 하네스가 줄을 찾는 자리와 같다.</summary>
    public RollbackHistory RollbackHistory { get; }
}
