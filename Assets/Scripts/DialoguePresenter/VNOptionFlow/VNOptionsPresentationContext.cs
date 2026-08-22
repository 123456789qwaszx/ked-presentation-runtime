using System.Collections.Generic;
using Yarn.Unity;

public sealed class VNOptionsPresentationContext
{
    public DialogueOption[] SourceOptions;
    public LineCancellationToken Token;
    public string NodeName;

    public int ChoiceSequence;

    public List<VNOptionViewModel> ViewModels;
    public IPresentationOptionsBoxView OptionsBoxView;

    public DialogueOption SelectedOption;

    // Phase Tracking
    public VNOptionsPresentationPhase Phase = VNOptionsPresentationPhase.None;

    public bool HasAnyAvailableOption
    {
        get
        {
            for (int i = 0; i < SourceOptions.Length; i++)
            {
                if (SourceOptions[i].IsAvailable)
                    return true;
            }

            return false;
        }
    }
}