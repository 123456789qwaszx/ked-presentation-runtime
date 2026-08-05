using UnityEngine;
using Yarn.Unity;

public sealed class DialogueAdvanceDispatcher : MonoBehaviour
{
    private AdvanceGate _gate;
    private DialogueRunner _dialogueRunner;
    private VNLinePresentationState _linePresentationAdvanceState;

    /// <summary>다음 라인 요청 직전(라인 완전 표시 상태)에 난다. U14 등가성 하네스가 쓴다.</summary>
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
    public void DispatchSpeedUpModeAdvance() => DispatchAdvance(AdvanceRequestKind.SpeedUpMode);
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
            // U14 등가성 하네스의 관측점: "라인 완전 표시 + 전진 직전" = 정착 상태가 안정된 순간.
            BeforeNextLineDispatched?.Invoke();

            _dialogueRunner.RequestNextLine();
            _gate.AddCooldownSeconds(_gate.GetCooldownAfterNextLine(kind));
        }
    }
}