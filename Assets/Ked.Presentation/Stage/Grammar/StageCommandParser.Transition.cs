using Ked.Presentation.Core;
using UnityEngine;

public sealed partial class StageCommandParser
{
    // <<tx 프리셋 [무대=stage01] [시간]>> — 프리셋은 StageMaskMotionPresetDB의 키
    // (slant_in/out · hstrip_open/close/in/out · vstrip_open/close/in/out · band_in/out · iris_in/out ·
    // shutter_close/open · daze_close/open · strip_cover/clear).
    // 시간이 비면 override 없음 — 프리셋 자체 duration을 쓴다.
    //
    // 무대를 어디까지 보여 줄지 새로 정하면 그 무대의 틀(컷인 창 · 판, 그림 팝업)은 끝난다 — 틀 되돌리기를 먼저 내고 마스크를 움직인다.
    // 무대 리그(마스크)와 무대 틀에 걸친 규칙이라 두 커맨드로 나눠 여기(문법)에 드러낸다. 마스크 커맨드는 틀을 모른다
    // (stage-frame.md §4, docs/PLAN_stage-code-homes.md §3).
    private void EnqueueStageMaskMotionPresetSpec(
        string presetKey,
        string stage,
        string durationToken)
    {
        if (string.IsNullOrWhiteSpace(presetKey))
        {
            Debug.LogWarning("[StageCommandParser] <<tx>>에 프리셋이 없다. 건너뛴다.");
            return;
        }

        string stageKey = StageKeyParser.ParseStage(stage);
        string key = StageMaskMotionPresetDBSO.NormalizeKey(presetKey);

        Emit(new StageFrameResetCommandSpec
        {
            stage = stageKey,
            cause = $"tx '{key}'"
        });

        Emit(new StageMaskMotionCommandSpec
        {
            stage = stageKey,
            presetKey = key,
            durationOverride = string.IsNullOrEmpty(durationToken)
                ? -1f
                : YarnDurationParser.Parse(durationToken)
        });
    }

    private void EnqueueStageMaskClearSpec()
    {
        // 무대 전부 — 무대 집합은 조립이 정하므로 파서가 열거하지 않는다. 틀도 전부 중립으로(위와 같은 규칙, 경고 없이).
        Emit(new StageFrameResetCommandSpec
        {
            allStages = true
        });

        Emit(new StageMaskClearCommandSpec
        {
            allStages = true,
            mode = StageMaskClearMode.UnmaskedFullVisible,
            hideEdge = true
        });
    }
}