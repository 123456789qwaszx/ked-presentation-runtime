using UnityEngine;
using Yarn.Unity;

public sealed class DialogueAdvanceDispatcher : MonoBehaviour
{
    private AdvanceGate _gate;
    private DialogueRunner _dialogueRunner;
    private VNLinePresentationState _linePresentationAdvanceState;

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
            _dialogueRunner.RequestNextLine();
            _gate.AddCooldownSeconds(_gate.GetCooldownAfterNextLine(kind));
        }
    }
}