using UnityEngine;
using Yarn.Unity;

public sealed class DialogueAdvanceRouter
{
    private readonly AdvanceGate _gate;
    private readonly DialogueRunner _dialogueRunner;
    private readonly InlineEventMarkupHandler _inlineMarkupHandler;

    private readonly float _cooldownAfterHurryUpSec;
    private readonly float _cooldownAfterNextLineSec;

    public DialogueAdvanceRouter(
        AdvanceGate gate,
        DialogueRunner dialogueRunner,
        InlineEventMarkupHandler inlineMarkupHandler,
        float cooldownAfterHurryUpSec  = 0.3f,  // 300ms
        float cooldownAfterNextLineSec = 0.24f) // 240ms
    {
        _gate = gate;
        _dialogueRunner = dialogueRunner;
        _inlineMarkupHandler = inlineMarkupHandler;

        _cooldownAfterHurryUpSec  = cooldownAfterHurryUpSec;
        _cooldownAfterNextLineSec = Mathf.Max(cooldownAfterNextLineSec, 0.1f); // Prevent double-skip: enforce a minimum cooldown
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

            _gate.AddCooldownSeconds(_cooldownAfterHurryUpSec);
        }
        else
        {
            _dialogueRunner.RequestNextLine();

            _gate.AddCooldownSeconds(_cooldownAfterNextLineSec);
        }

        return true;
    }
}