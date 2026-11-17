using Ked.Presentation.Core;
using UnityEngine;

public sealed partial class StageCommandParser
{
    // <<stage_blur [층=mid] [양=1] [무대=stage00]>>
    private void EnqueueLayerBlurSpec(
        string stageKey,
        string layerKey,
        float amount)
    {
        Emit(new StageLayerBlurCommandSpec
        {
            stage = StageKeyParser.ParseStage(stageKey),
            layerDepth = StageKeyParser.ParseLayerDepth(layerKey),

            // 옛 blurRadius는 0~8 범위였다. 새 amount는 0~1이라 조정 필요함.
            amount = Mathf.Clamp01(amount)
        });
    }

    // 무대 · 층 전부 — 집합은 조립이 정하므로 파서가 열거하지 않는다.
    // 어둡게(docs/PLAN_screen-cover.md §3.2) — 층 all은 그 무대의 가장 앞 층(무대 전체). 이징을 적지 않으면 Linear.
    private void EnqueueStageLayerDimSpec(
        StageCommand c,
        string stageKey,
        string layerKey,
        string amountToken,
        string durationToken,
        string easeToken)
    {
        if (amountToken == null || !YarnNumberParser.TryParseFloat(amountToken, out float amount))
        {
            Debug.LogWarning($"[StageCommandParser] <<{c.Name}>> 세기가 없거나 숫자가 아니다 ('{amountToken}'). 건너뛴다 — 걷으려면 0을 적는다.");
            return;
        }

        bool all = string.Equals((layerKey ?? "").Trim(), "all", System.StringComparison.OrdinalIgnoreCase);

        Emit(new StageLayerDimCommandSpec
        {
            stage = StageKeyParser.ParseStage(stageKey),
            allLayers = all,
            layerDepth = all ? StageLayerLabels.Close : StageKeyParser.ParseLayerDepth(layerKey),
            amount = Mathf.Clamp01(amount),
            duration = YarnDurationParser.Parse(durationToken),
            ease = YarnEaseParser.Parse(easeToken, DG.Tweening.Ease.Linear),
        });
    }

    private void EnqueueStageLayerDimResetSpec(string durationToken, string easeToken)
    {
        Emit(new StageLayerDimCommandSpec
        {
            resetAll = true,
            amount = 0f,
            duration = YarnDurationParser.Parse(durationToken),
            ease = YarnEaseParser.Parse(easeToken, DG.Tweening.Ease.Linear),
        });
    }

    private void EnqueueStageLayerBlurClearSpec()
    {
        Emit(new StageLayerBlurCommandSpec
        {
            clearAll = true,
            amount = 0f
        });
    }
}
