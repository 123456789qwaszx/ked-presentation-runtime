using Yarn.Unity;

public sealed class DialogueAdvanceDispatcher
{
    private AdvanceGate _gate;
    private DialogueRunner _dialogueRunner;
    private VNLinePresentationState _linePresentationAdvanceState;

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
            BeforeNextLineDispatched?.Invoke();

            _dialogueRunner.RequestNextLine();
            _gate.AddCooldownSeconds(_gate.GetCooldownAfterNextLine(kind));
        }
    }
}