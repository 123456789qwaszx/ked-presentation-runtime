using UnityEngine;
using Yarn.Unity;

public sealed class DialogueAdvanceDispatcher : MonoBehaviour
{
    private AdvanceGate _gate;
    private DialogueRunner _dialogueRunner;
    private InlineEventMarkupHandler _inlineMarkupHandler;

    public void Initialize(
        AdvanceGate gate,
        DialogueRunner dialogueRunner,
        InlineEventMarkupHandler inlineMarkupHandler)
    {
        _gate = gate;
        _dialogueRunner = dialogueRunner;
        _inlineMarkupHandler = inlineMarkupHandler;
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

    
    private bool TryDispatchToYarn()
    {
        if (_dialogueRunner == null || !_dialogueRunner.IsDialogueRunning) return false;

        if (!_gate.IsLineFullyShown())
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
}