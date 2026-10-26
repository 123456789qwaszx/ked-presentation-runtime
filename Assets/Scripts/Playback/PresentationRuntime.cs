using System;
using UnityEngine;

/// <summary>
/// 조립이 끝난 연출 런타임. 재생 호스트를 하나 붙이면 돈다.
///
/// 무대·명령·Yarn·대사 표시·재생 제어(전진·스킵·오토·롤백 표적)를 갖는다.
/// 진행·저장을 모른다. 그 위에 무엇을 얹을지(노드 하나 / 챕터 진행)는 호스트가 정한다.
/// </summary>
public sealed class PresentationRuntime
{
    private readonly VNAdvanceInputPoller _inputPoller;

    public ScenePresentation ScenePresentation { get; }
    public VNFeatureController Features { get; }
    public DialogueAdvanceDispatcher AdvanceDispatcher { get; }

    public VNLinePresentationFlow LineFlow { get; }
    public VNLinePresentationState LineState { get; }

    public RollbackHistory RollbackHistory { get; }
    public ChoiceHistory ChoiceHistory { get; }
    public BacklogRecorder Backlog { get; }

    public IPlaybackHost Host { get; private set; }

    public PresentationRuntime(
        ScenePresentation scenePresentation,
        VNFeatureController features,
        DialogueAdvanceDispatcher advanceDispatcher,
        VNLinePresentationFlow lineFlow,
        VNLinePresentationState lineState,
        RollbackHistory rollbackHistory,
        ChoiceHistory choiceHistory,
        BacklogRecorder backlog,
        VNAdvanceInputPoller inputPoller)
    {
        ScenePresentation = scenePresentation;
        Features = features;
        AdvanceDispatcher = advanceDispatcher;
        LineFlow = lineFlow;
        LineState = lineState;
        RollbackHistory = rollbackHistory;
        ChoiceHistory = choiceHistory;
        Backlog = backlog;
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
