using UnityEngine;

// screen_blur 계열의 새 경로. 야른 어휘는 그대로 두고 기구만 바꾼다 —
// 레이어를 통째로 캡처해 오버레이로 덮는 대신, 그 레이어 아래 리그들의 셰이더 블러를 직접 움직인다.
//
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

            // 옛 blurRadius는 0~8 범위였다. 새 amount는 0~1이라 저작값(0.8~1.2)이
            // 그대로 옮겨오지 않는다 — 기구가 바뀌었으므로 어차피 재조정 대상이다.
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
