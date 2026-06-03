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

    public bool HasAnyAvailableOption
    {
        get
        {
            if (SourceOptions == null)
                return false;

            for (int i = 0; i < SourceOptions.Length; i++)
            {
                if (SourceOptions[i].IsAvailable)
                    return true;
            }

            return false;
        }
    }

    // Replay
    public DialogueOption ReplayOption { get; set; }
    public bool IsReplay { get; set; }

    // Box Presentation
    public VNOptionsBoxPresentationResult BoxResult { get; set; }

    // Result
    public DialogueOption SelectedOption { get; set; }

    // Phase Tracking
    public VNOptionsPresentationPhase Phase { get; set; } = VNOptionsPresentationPhase.None;

    public bool NoOptionsAvailable => SelectionDecision != null && SelectionDecision.ShouldReturnNoOption;
    public bool ShouldReplayRecordedChoice => SelectionDecision != null && SelectionDecision.ShouldRecordedChoiceDuringSeek;
    public bool ShouldPresentInteractive => SelectionDecision != null && SelectionDecision.ShouldPresentInteractive;
}