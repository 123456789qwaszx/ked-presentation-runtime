using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

public sealed class VNOptionsPresentationContext
{
    public DialogueOption[] SourceOptions;
    public LineCancellationToken Token;
    public string NodeName;

    public int ChoiceSequence;

    public List<VNOptionViewModel> ViewModels;

    // 옵션 항목을 붙일 자리. 박스가 뜬 뒤 flow가 채운다.
    public RectTransform ItemContainer;

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