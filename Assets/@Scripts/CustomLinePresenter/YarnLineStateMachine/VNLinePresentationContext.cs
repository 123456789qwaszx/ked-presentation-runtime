using TMPro;
using Yarn.Markup;
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
    public VNSeekLineDecision SeekDecision { get; set; }
    
    

    public bool IsPendingSeekTargetLine
    {
        get
        {
            return SeekDecision != null && SeekDecision.IsTargetLineReached;
        }
    }

    public bool ShouldPassThrough
    {
        get
        {
            return SeekDecision != null && SeekDecision.ShouldPassThroughPresentation;
        }
    }

    public bool ShouldUseImmediateTransition
    {
        get
        {
            return SeekDecision != null && SeekDecision.ShouldUseImmediateTransition;
        }
    }

    // ── Visual Run ─────────────────────────────────────
    public LinePresentationRun Run { get; set; }

    // ── Box Presentation ───────────────────────────────
    public DialogueBoxPresentationResult BoxResult { get; set; }
    public TMP_Text LineText { get; set; }
    public MarkupParseResult Text { get; set; }

    // ── Phase Tracking ─────────────────────────────────
    public VNLinePresentationPhase Phase { get; set; } = VNLinePresentationPhase.None;

    public string LineId
    {
        get { return Line != null ? Line.TextID : ""; }
    }

    public string CharacterName
    {
        get { return Line != null ? Line.CharacterName : ""; }
    }

    public bool HasValidRun
    {
        get { return Run != null && Run.IsValid; }
    }

    public string Snapshot()
    {
        string decisionText = SeekDecision == null ? "seekDecision=null" : SeekDecision.ToString();

        return $"phase={Phase}, node={NodeName}, line={LineId}, char={CharacterName}, " +
               $"runValid={HasValidRun}, {decisionText}";
    }
}