public interface IStageDepthLayerBlurRuntime
{
    void ShowDefocus(
        CommandRunScope scope,
        PresentationStageKey stage,
        PresentationDepthLayerKey layer,
        float alpha,
        float duration,
        float blurRadius,
        int iterations,
        UIStageBlurDownsample downsample,
        float coveragePaddingPixels);

    void HideDefocus(
        PresentationStageKey stage,
        PresentationDepthLayerKey layer,
        float duration);
}