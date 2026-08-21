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

    public bool HasAnyAvailableOption
    {
        get
        {
            if (SourceOptions == null)
                return false;

            for (int i = 0; i < SourceOptions.Length; i++)
            {
                DialogueOption option = SourceOptions[i];

                if (option != null && option.IsAvailable)
                    return true;
            }

            return false;
        }
    }
}