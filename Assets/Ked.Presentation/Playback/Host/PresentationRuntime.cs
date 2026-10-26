using System;
using UnityEngine;

/// <summary>
/// 조립이 끝난 연출 런타임. 재생 호스트를 하나 붙이면 돈다.
///
/// 무대·명령·Yarn·대사 표시·재생 제어(전진·스킵·오토·롤백 표적)를 갖는다.
/// 진행·저장을 모른다. 그 위에 무엇을 얹을지(노드 하나 / 챕터 진행)는 호스트가 정한다.
///
/// 호스트가 만지는 것은 둘이다 — 재생 단위의 수명(Unit)과 연출 기억의 입구(Memory).
/// 백로그·선택 기록·롤백 기록·줄 시크 같은 안쪽 부품은 내놓지 않는다.
/// </summary>
public sealed class PresentationRuntime
{
    private readonly VNAdvanceInputPoller _inputPoller;

    /// <summary>재생 단위의 수명. 호스트는 이것으로 열고, 재생하고, 멈추고, 루트부터 다시 달린다.</summary>
    public PlaybackUnit Unit { get; }

    /// <summary>연출 기억의 입구. 호스트는 이것으로 기억을 문자열로 꺼내고 되돌린다.</summary>
    public PlaybackMemory Memory { get; }

    public VNFeatureController Features { get; }
    public DialogueAdvanceDispatcher AdvanceDispatcher { get; }

    public VNLinePresentationFlow LineFlow { get; }

    public IPlaybackHost Host { get; private set; }

    public PresentationRuntime(
        ScenePresentation scenePresentation,
        VNFeatureController features,
        DialogueAdvanceDispatcher advanceDispatcher,
        VNLinePresentationFlow lineFlow,
        RollbackHistory rollbackHistory,
        ChoiceHistory choiceHistory,
        BacklogRecorder backlog,
        VNAdvanceInputPoller inputPoller)
    {
        Unit = new PlaybackUnit(scenePresentation, backlog, rollbackHistory);
        Memory = new PlaybackMemory(Unit, backlog, choiceHistory, rollbackHistory);
        Features = features;
        AdvanceDispatcher = advanceDispatcher;
        LineFlow = lineFlow;
        _inputPoller = inputPoller;
    }

    /// <summary>
    /// 재생 호스트를 붙인다. 한 실행에 호스트는 하나다.
    /// 두 주행자가 공존하면 입력이 매번 "누가 돌고 있나"를 물어야 하므로, 두 번째 연결은 거부한다.
    /// </summary>
    public void AttachHost(IPlaybackHost host)
    {
        if (host == null)
            throw new ArgumentNullException(nameof(host));

        if (Host != null)
        {
            throw new InvalidOperationException(
                $"[연출 런타임] 재생 호스트는 하나만 붙는다. 이미 {Host.GetType().Name}이(가) 붙어 있어 " +
                $"{host.GetType().Name}을(를) 거부한다.");
        }

        Host = host;

        _inputPoller.Initialize(
            AdvanceDispatcher,
            Features,
            host);

        Debug.Log($"[연출 런타임] 재생 호스트: {host.GetType().Name}");
    }
}
