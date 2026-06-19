using System.Collections.Generic;
using Yarn.Unity;

public sealed class VNOptionViewModel
{
    public DialogueOption SourceOption { get; private set; }

    // 원본 dialogueOptions 배열 기준 index.
    // UI 표시 index가 아니라 replay에 사용할 원본 option index.
    public int SourceOptionIndex { get; private set; }

    // 현재 node 안에서 몇 번째 option set인지.
    public int ChoiceIndexInNode { get; private set; }

    public string Label { get; private set; }
    public bool IsAvailable { get; private set; }
    public List<VNOptionEffectPreview> Effects { get; private set; }

    public VNOptionViewModel(
        DialogueOption sourceOption,
        int sourceOptionIndex,
        int choiceIndexInNode,
        string label,
        bool isAvailable,
        List<VNOptionEffectPreview> effects)
    {
        SourceOption = sourceOption;
        SourceOptionIndex = sourceOptionIndex;
        ChoiceIndexInNode = choiceIndexInNode;
        Label = label ?? string.Empty;
        IsAvailable = isAvailable;
        Effects = effects ?? new List<VNOptionEffectPreview>();
    }
}