using System.Collections.Generic;
using Yarn.Unity;

public sealed class VNOptionViewModel
{
    public DialogueOption SourceOption { get; private set; }
    public string Label { get; private set; }
    public bool IsAvailable { get; private set; }
    public List<VNOptionEffectPreview> Effects { get; private set; }

    public VNOptionViewModel(
        DialogueOption sourceOption,
        string label,
        bool isAvailable,
        List<VNOptionEffectPreview> effects)
    {
        SourceOption = sourceOption;
        Label = label ?? string.Empty;
        IsAvailable = isAvailable;
        Effects = effects ?? new List<VNOptionEffectPreview>();
    }
}