using UnityEngine;
using Yarn.Unity;

public sealed class DialogueAdvanceDispatcher : MonoBehaviour
{
    private AdvanceGate _gate;
    private DialogueRunner _dialogueRunner;
    private DialogueRunner _subPresentationRunner;
    private InlineEventMarkupHandler _inlineMarkupHandler;
    private LinePresentationAdvanceState _linePresentationAdvanceState;

    public void Initialize(
        AdvanceGate gate, 
        DialogueRunner dialogueRunner,
        DialogueRunner subPresentationRunner,
        InlineEventMarkupHandler inlineMarkupHandler, 
        LinePresentationAdvanceState  linePresentationAdvanceState)
    {
        _gate = gate;
        _dialogueRunner = dialogueRunner;
        _subPresentationRunner = subPresentationRunner;
        _inlineMarkupHandler = inlineMarkupHandler;
        _linePresentationAdvanceState = linePresentationAdvanceState;
    }
    
    public void DispatchAdvance()
    {
        // Single gate for ALL user advance requests (mouse/space/etc.)
        if (!_gate.TryAcceptUserAdvance())
            return;

        // If Yarn is running, route into Yarn
        if (TryDispatchToYarn())
            return;

        // Dialogue is not running — apply a small cooldown to prevent rapid re-triggering
        _gate.AddCooldownSeconds(0.03f);
    }
    
    public void DispatchSeekNext()
    {
        if (_dialogueRunner == null || !_dialogueRunner.IsDialogueRunning)
            return;
        
        if (!_linePresentationAdvanceState.IsLineFullyShown)
        {
            _inlineMarkupHandler.FlushPendingSignals();
            _dialogueRunner.RequestHurryUpLine();
        }
        
        _dialogueRunner.RequestNextLine();
    }
    
    private bool TryDispatchToYarn()
    {
        if (_dialogueRunner == null || !_dialogueRunner.IsDialogueRunning)
            return false;

        if (!_linePresentationAdvanceState.IsLineFullyShown)
        {
            _inlineMarkupHandler.FlushPendingSignals();
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
    
    public void DispatchSubPresentationAdvance()
    {
        if (_subPresentationRunner == null || !_subPresentationRunner.IsDialogueRunning)
            return;

        _subPresentationRunner.RequestNextLine();
    }
}

// public void DispatchSeekHurryUp()
// {
//     if (_dialogueRunner == null || !_dialogueRunner.IsDialogueRunning)
//         return;
//         
//     if (!_linePresentationAdvanceState.IsLineFullyShown)
//     {
//         _inlineMarkupHandler.FlushPendingSignals();
//         _dialogueRunner.RequestHurryUpLine();
//     }
// }