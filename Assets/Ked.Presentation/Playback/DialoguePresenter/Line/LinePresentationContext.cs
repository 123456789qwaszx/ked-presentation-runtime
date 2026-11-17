using TMPro;
using Yarn.Markup;
using Yarn.Unity;

public enum SeekLineDecision
{
    NotSeeking = 0,
    SkipVisualAndDispatchSeekNext,
    TargetLineReached,
}

public sealed class LinePresentationContext
{
    // Input
    public LocalizedLine Line { get; set; }
    public LineCancellationToken Token { get; set; }
    public bool ShouldUseImmediateTransition { get; set; }
    public bool IsRapidSkip { get; set; }

    // Seek Kind
    public SeekKind EntrySeekKind { get; set; }
    
    // Record Decision
    public LineRecordDecision RecordDecision { get; set; }

    // Seek Decision
    public SeekLineDecision SeekDecision { get; set; }

    // Advance Mode
    public LineAdvanceMode AdvanceMode { get; set; }

    // 이 줄의 표지 목록 — 줄 머리가 짓고(StepHead.PlayLine) 이 줄의 흐름만 끝낸다(docs/PLAN_line-beat.md I5).
    public InlineCommandTrack.Line Beats { get; set; }

    // Visual Run
    public LinePresentationRun Run { get; set; }

    // Box Presentation
    public TMP_Text LineText { get; set; }
    public MarkupParseResult Text { get; set; }

    // Phase Tracking
    public LinePresentationPhase Phase { get; set; } = LinePresentationPhase.None;
}