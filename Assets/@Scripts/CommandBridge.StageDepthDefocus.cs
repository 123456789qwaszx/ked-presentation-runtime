using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    private const float DefaultDepthDefocusAlpha = 1f;
    private const float DefaultDepthDefocusBlurRadius = 3f;
    private const int DefaultDepthDefocusIterations = 2;
    private const UIStageBlurDownsample DefaultDepthDefocusDownsample = UIStageBlurDownsample.Quarter;
    private const float DefaultDepthDefocusDuration = 0.35f;


    private void EnqueueStageDepthBlurSpec(string stageKey)
    {
        if (!PresentationCommandKeyParser.TryParseStageKey(stageKey, out PresentationStageKey stage))
            return;

        EnqueueStageDepthDefocusSpec(
            stage,
            PresentationDepthLayerKey.Far,
            true,
            0.85f,
            5.5f,
            3,
            UIStageBlurDownsample.Quarter,
            DefaultDepthDefocusDuration);

        EnqueueStageDepthDefocusSpec(
            stage,
            PresentationDepthLayerKey.Back,
            true,
            0.65f,
            3.75f,
            2,
            UIStageBlurDownsample.Quarter,
            DefaultDepthDefocusDuration);

        EnqueueStageDepthDefocusSpec(
            stage,
            PresentationDepthLayerKey.Mid,
            true,
            0.35f,
            1.75f,
            2,
            UIStageBlurDownsample.Quarter,
            DefaultDepthDefocusDuration);

        EnqueueStageDepthDefocusSpec(
            stage,
            PresentationDepthLayerKey.Front,
            false,
            0f,
            DefaultDepthDefocusBlurRadius,
            DefaultDepthDefocusIterations,
            DefaultDepthDefocusDownsample,
            DefaultDepthDefocusDuration);

        EnqueueStageDepthDefocusSpec(
            stage,
            PresentationDepthLayerKey.Close,
            false,
            0f,
            DefaultDepthDefocusBlurRadius,
            DefaultDepthDefocusIterations,
            DefaultDepthDefocusDownsample,
            DefaultDepthDefocusDuration);
    }

    private void EnqueueStageDepthBlurClearSpec(string stageKey)
    {
        if (!PresentationCommandKeyParser.TryParseStageKey(stageKey, out PresentationStageKey stage))
            return;

        EnqueueStageDepthDefocusOffSpec(stage, PresentationDepthLayerKey.Far, DefaultDepthDefocusDuration);
        EnqueueStageDepthDefocusOffSpec(stage, PresentationDepthLayerKey.Back, DefaultDepthDefocusDuration);
        EnqueueStageDepthDefocusOffSpec(stage, PresentationDepthLayerKey.Mid, DefaultDepthDefocusDuration);
        EnqueueStageDepthDefocusOffSpec(stage, PresentationDepthLayerKey.Front, DefaultDepthDefocusDuration);
        EnqueueStageDepthDefocusOffSpec(stage, PresentationDepthLayerKey.Close, DefaultDepthDefocusDuration);
    }

    private void EnqueueStageDepthBlurLayerSpec(
        string stageKey,
        string layerKey)
    {
        EnqueueStageDepthBlurLayerTimedSpec(
            stageKey,
            layerKey,
            DefaultDepthDefocusBlurRadius,
            DefaultDepthDefocusDuration);
    }

    private void EnqueueStageDepthBlurLayerTimedSpec(
        string stageKey,
        string layerKey,
        float blurRadius,
        float duration)
    {
        EnqueueStageDepthDefocusSpec(
            stageKey,
            layerKey,
            DefaultDepthDefocusAlpha,
            blurRadius,
            DefaultDepthDefocusIterations,
            "quarter",
            duration);
    }

    private void EnqueueStageDepthBlurLayerAlphaSpec(
        string stageKey,
        string layerKey,
        float alpha,
        float blurRadius,
        float duration)
    {
        EnqueueStageDepthDefocusSpec(
            stageKey,
            layerKey,
            alpha,
            blurRadius,
            DefaultDepthDefocusIterations,
            "quarter",
            duration);
    }

    private void EnqueueStageDepthDefocusSpec(
        string stageKey,
        string layerKey,
        float alpha,
        float blurRadius,
        int iterations,
        string downsampleKey,
        float duration)
    {
        if (!PresentationCommandKeyParser.TryParseStageKey(stageKey, out PresentationStageKey stage))
            return;

        if (!PresentationCommandKeyParser.TryParseDepthLayerKey(layerKey, out PresentationDepthLayerKey layer))
            return;

        if (!PresentationCommandKeyParser.TryParseBlurDownsampleKey(downsampleKey, out UIStageBlurDownsample downsample))
            return;

        EnqueueStageDepthDefocusSpec(
            stage,
            layer,
            true,
            alpha,
            blurRadius,
            iterations,
            downsample,
            duration);
    }

    private void EnqueueStageDepthDefocusOffSpec(
        string stageKey,
        string layerKey,
        float duration)
    {
        if (!PresentationCommandKeyParser.TryParseStageKey(stageKey, out PresentationStageKey stage))
            return;

        if (!PresentationCommandKeyParser.TryParseDepthLayerKey(layerKey, out PresentationDepthLayerKey layer))
            return;

        EnqueueStageDepthDefocusOffSpec(stage, layer, duration);
    }

    private void EnqueueStageDepthDefocusOffSpec(
        PresentationStageKey stage,
        PresentationDepthLayerKey layer,
        float duration)
    {
        EnqueueStageDepthDefocusSpec(
            stage,
            layer,
            false,
            0f,
            DefaultDepthDefocusBlurRadius,
            DefaultDepthDefocusIterations,
            DefaultDepthDefocusDownsample,
            duration);
    }

    private void EnqueueStageDepthDefocusSpec(
        PresentationStageKey stage,
        PresentationDepthLayerKey layer,
        bool visible,
        float alpha,
        float blurRadius,
        int iterations,
        UIStageBlurDownsample downsample,
        float duration)
    {
        var spec = new StageDepthDefocusCommandSpec
        {
            stage = stage,
            layer = layer,
            visible = visible,

            alpha = Mathf.Clamp01(alpha),
            blurRadius = Mathf.Max(0f, blurRadius),
            iterations = Mathf.Clamp(iterations, 1, 6),
            downsample = downsample,

            duration = Mathf.Max(0f, duration)
        };

        Collect(spec);
    }
}