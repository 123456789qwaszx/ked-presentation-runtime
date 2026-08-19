using UnityEngine;

// 옛 경로로 되돌리려면 CommandBridge.cs의 BindStageDepthDefocus가
// Enqueue*DepthBlurSpec(= CommandBridge.StageDepthDefocus.cs)을 부르게 하면 된다.
public sealed partial class YarnCommandBridge
{
    private static readonly PresentationStageKey[] BlurStages =
    {
        PresentationStageKey.Stage00,
        PresentationStageKey.Stage01,
        PresentationStageKey.Stage02,
    };

    private static readonly PresentationDepthLayerKey[] BlurLayers =
    {
        PresentationDepthLayerKey.Far,
        PresentationDepthLayerKey.Back,
        PresentationDepthLayerKey.Mid,
        PresentationDepthLayerKey.Front,
        PresentationDepthLayerKey.Close,
    };

    private void EnqueueStage00LayerBlurSpec(string layerKey = "mid", float amount = 1f)
        => EnqueueLayerBlurSpec(PresentationStageKey.Stage00, layerKey, amount);

    private void EnqueueStage01LayerBlurSpec(string layerKey = "mid", float amount = 1f)
        => EnqueueLayerBlurSpec(PresentationStageKey.Stage01, layerKey, amount);

    private void EnqueueStage02LayerBlurSpec(string layerKey = "mid", float amount = 1f)
        => EnqueueLayerBlurSpec(PresentationStageKey.Stage02, layerKey, amount);

    private void EnqueueLayerBlurSpec(
        PresentationStageKey stage,
        string layerKey,
        float amount)
    {
        if (!PresentationCommandKeyParser.TryParseDepthLayerKey(
                layerKey,
                out PresentationDepthLayerKey layer))
            return;

        Collect(new StageLayerBlurCommandSpec
        {
            stage = stage,
            layer = layer,

            // 옛 blurRadius는 0~8 범위였다. 새 amount는 0~1이라 조정 필요함.
            amount = Mathf.Clamp01(amount)
        });
    }

    private void EnqueueStageLayerBlurClearSpec()
    {
        for (int s = 0; s < BlurStages.Length; s++)
        for (int l = 0; l < BlurLayers.Length; l++)
        {
            Collect(new StageLayerBlurCommandSpec
            {
                stage = BlurStages[s],
                layer = BlurLayers[l],
                amount = 0f
            });
        }
    }
}