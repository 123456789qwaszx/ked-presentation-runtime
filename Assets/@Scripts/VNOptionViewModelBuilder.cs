using System.Collections.Generic;
using Yarn.Unity;

public static class VNOptionViewModelBuilder
{
    public static VNOptionViewModel Build(DialogueOption option)
    {
        string rawText = option.Line.TextWithoutCharacterName.Text;

        List<VNOptionEffectPreview> effects;
        string label = VNOptionEffectPreviewParser.Parse(rawText, out effects);

        return new VNOptionViewModel(
            sourceOption: option,
            label: label,
            isAvailable: option.IsAvailable,
            effects: effects);
    }
}