using System.Collections.Generic;
using Yarn.Unity;

public static class VNOptionViewModelBuilder
{
    private static readonly VNOptionEffectPreviewResolver EffectResolver = new ();

    public static VNOptionViewModel Build(
        DialogueOption option,
        int sourceOptionIndex,
        int choiceIndexInNode)
    {
        string label = option.Line.TextWithoutCharacterName.Text;

        List<VNOptionEffectPreview> effects = EffectResolver.Resolve(option.Line.Metadata);

        return new VNOptionViewModel(
            sourceOption: option,
            sourceOptionIndex: sourceOptionIndex,
            choiceIndexInNode: choiceIndexInNode,
            label: label,
            isAvailable: option.IsAvailable,
            effects: effects);
    }
}