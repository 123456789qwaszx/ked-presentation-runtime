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

    public void DispatchAdvance()
    {
        if (!_gate.TryAcceptUserAdvance())
            return;

        if (TryDispatchToYarn())
            return;

        _gate.AddCooldownSeconds(0.03f);
    }

    public void DispatchSeekNext()
    {
        if (!_dialogueRunner.IsDialogueRunning)
            return;

        if (!_linePresentationAdvanceState.IsLineFullyShown)
        {
            //_inlineMarkupHandler?.FlushPendingSignals();
            _dialogueRunner.RequestHurryUpLine();
        }

        _dialogueRunner.RequestNextLine();
    }

    private bool TryDispatchToYarn()
    {
        if (!_dialogueRunner.IsDialogueRunning)
            return false;

        if (!_linePresentationAdvanceState.IsLineFullyShown)
        {
            //_inlineMarkupHandler?.FlushPendingSignals();
            _dialogueRunner.RequestHurryUpLine();
            _gate.AddCooldownSeconds(_gate.CooldownAfterHurryUpSec);
        }
        else
        {
            _dialogueRunner.RequestNextLine();
            _gate.AddCooldownSeconds(_gate.CooldownAfterNextLineSec);
        }

        return true;
    }
}