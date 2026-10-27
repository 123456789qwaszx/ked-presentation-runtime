using Yarn.Unity;

public sealed class DialogueAdvanceDispatcher
{
    private AdvanceGate _gate;
    private DialogueRunner _dialogueRunner;
    private VNLinePresentationState _linePresentationAdvanceState;

    public bool IsDialogueRunning =>
        _dialogueRunner != null &&
        _dialogueRunner.IsDialogueRunning;
    
    /// <summary>
    /// 다음 라인을 요청하기 직전, 등가성 하네스의 관측점으로 사용.
    /// </summary>
    public event System.Action BeforeNextLineDispatched;

    public void Initialize(
        AdvanceGate gate,
        DialogueRunner dialogueRunner,
        VNLinePresentationState linePresentationAdvanceState)
    {
        _gate = gate;
        _dialogueRunner = dialogueRunner;
        _linePresentationAdvanceState = linePresentationAdvanceState;
    }

    public void DispatchAdvance() => DispatchAdvance(AdvanceRequestKind.User);
    public void DispatchAutoAdvance() => DispatchAdvance(AdvanceRequestKind.Auto);
    public void DispatchRapidSkipAdvance() => DispatchAdvance(AdvanceRequestKind.RapidSkip);

    /// <summary>
    /// #auto 줄(VNLineAdvanceTag) — 다 보인 줄을 다음으로 넘긴다. 서두르기는 하지 않는다(다 보이기 전에는 부르지 않는다).
    /// 다음 줄을 요청했으면 true. 관문이 막으면(연출 명령이 도는 중 · 시크 · 쿨다운) false — 부르는 쪽이 다음 프레임에 다시 부른다.
    /// 대화가 이미 끝났으면 넘길 것이 없다 — true.
    /// </summary>
    public bool TryDispatchLineAutoAdvance()
    {
        if (!_dialogueRunner.IsDialogueRunning)
            return true;

        if (!_linePresentationAdvanceState.IsLineFullyShown)
            return false;

        if (!_gate.TryAccept(AdvanceRequestKind.Auto))
            return false;

        RequestNextLine(AdvanceRequestKind.Auto);
        return true;
    }

    private void DispatchAdvance(AdvanceRequestKind kind)
    {
        if (!_gate.TryAccept(kind))
            return;

        if (!_dialogueRunner.IsDialogueRunning)
            return;

        if (!_linePresentationAdvanceState.IsLineFullyShown)
        {
            _dialogueRunner.RequestHurryUpLine();
            _gate.AddCooldownSeconds(_gate.GetCooldownAfterHurryUp(kind));
        }
        else
        {
            RequestNextLine(kind);
        }
    }

    private void RequestNextLine(AdvanceRequestKind kind)
    {
        BeforeNextLineDispatched?.Invoke();

        _dialogueRunner.RequestNextLine();
        _gate.AddCooldownSeconds(_gate.GetCooldownAfterNextLine(kind));
    }
}