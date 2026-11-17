using Ked.Presentation.Core;
using UnityEngine;

public sealed partial class StageCommandParser
{
    // <<stage_mask 프리셋 [무대=stage01] [시간]>> — 프리셋은 StageMaskMotionPresetDB의 키
    // (slant_in/out · hstrip_open/close/in/out · vstrip_open/close/in/out · band_in/out · iris_in/out ·
    // shutter_close/open · daze_close/open · strip_cover/clear).
    // 시간이 비면 override 없음 — 프리셋 자체 duration을 쓴다.
    //
    // stage_mask는 마스크만 움직인다 — 틀(컷인 창)을 모른다. 컷인 판 · 팝업 그림은 무대 안의 리그라 마스크가 함께 자른다
    // (docs/PLAN_stage-visibility.md §3.1). 열린 컷인 위의 stage_mask는 컷인을 연 채로 둔다 — 뒤의 cutin_close가 정리한다.
    private void EnqueueStageMaskMotionPresetSpec(
        string presetKey,
        string stage,
        string durationToken)
    {
        if (string.IsNullOrWhiteSpace(presetKey))
        {
            Debug.LogWarning("[StageCommandParser] <<stage_mask>>에 프리셋이 없다. 건너뛴다.");
            return;
        }

        Emit(new StageMaskMotionCommandSpec
        {
            stage = StageKeyParser.ParseStage(stage),
            presetKey = StageMaskMotionPresetDBSO.NormalizeKey(presetKey),
            durationOverride = string.IsNullOrEmpty(durationToken)
                ? -1f
                : YarnDurationParser.Parse(durationToken)
        });
    }

    private void EnqueueStageMaskClearSpec()
    {
        // 무대 전부 — 무대 집합은 조립이 정하므로 파서가 열거하지 않는다.
        Emit(new StageMaskClearCommandSpec
        {
            allStages = true,
            mode = StageMaskClearMode.UnmaskedFullVisible,
            hideEdge = true
        });
    }
}