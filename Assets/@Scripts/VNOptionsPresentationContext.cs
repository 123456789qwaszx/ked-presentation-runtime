using System.Collections.Generic;
using Yarn.Unity;

public sealed class VNOptionsPresentationContext
{
    // Input
    public DialogueOption[] SourceOptions { get; set; }
    public LineCancellationToken Token { get; set; }
    public string NodeName { get; set; }

    // Derived
    public int ChoiceIndexInNode { get; set; }
    public VNOptionSelectionDecision SelectionDecision { get; set; }
    public List<VNOptionViewModel> ViewModels { get; set; }

    // Box Presentation
    public VNOptionsBoxPresentationResult BoxResult { get; set; }

    // Result
    public DialogueOption SelectedOption { get; set; }

    // Phase Tracking
    public VNOptionsPresentationPhase Phase { get; set; } = VNOptionsPresentationPhase.None;

    public bool ShouldReturnNoOption => SelectionDecision != null && SelectionDecision.ShouldReturnNoOption;
    public bool ShouldReplayRecordedSelection => SelectionDecision != null && SelectionDecision.ShouldReplayRecordedSelection;
    public bool ShouldPresentInteractive => SelectionDecision != null && SelectionDecision.ShouldPresentInteractive;
}