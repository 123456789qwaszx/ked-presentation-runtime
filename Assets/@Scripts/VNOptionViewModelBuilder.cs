using System.Collections.Generic;
using Yarn.Unity;

public static class VNOptionViewModelBuilder
{
    private static readonly VNOptionEffectPreviewResolver EffectResolver =
        new VNOptionEffectPreviewResolver();

    public static VNOptionViewModel Build(DialogueOption option)
    {
        string label = option.Line.TextWithoutCharacterName.Text;

        List<VNOptionEffectPreview> effects =
            EffectResolver.Resolve(option.Line.Metadata);

        return new VNOptionViewModel(
            sourceOption: option,
            label: label,
            isAvailable: option.IsAvailable,
            effects: effects);
    }
}