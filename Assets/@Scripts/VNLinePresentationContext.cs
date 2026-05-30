using System.Threading;
using Yarn.Unity;

public sealed class VNLinePresentationContext
{
    // ── Input ──────────────────────────────────────────
    public LocalizedLine Line { get; set; }
    public LineCancellationToken Token { get; set; }
    public string NodeName { get; set; }

    // ── Derived ────────────────────────────────────────
    public YarnLineMeta Meta { get; set; }

    // ── Seek Decision ──────────────────────────────────
    public bool IsPendingSeekTargetLine { get; set; }
    public bool ShouldPassThrough { get; set; }

    // ── Visual Run ─────────────────────────────────────
    public LinePresentationRun Run { get; set; }

    // ── Box Presentation ───────────────────────────────
    public DialogueBoxPresentationResult BoxResult { get; set; }

    // ── Phase Tracking ─────────────────────────────────
    public VNLinePresentationPhase Phase { get; set; } = VNLinePresentationPhase.None;
}