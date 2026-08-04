public sealed partial class YarnCommandBridge
{
    // durationToken이 빈 문자열이면 override 없음 - 프리셋 자체 duration을 사용한다.
    // (이전의 duration = -1f sentinel을 대체)
    private void EnqueueStageMaskMotionPresetSpec(
        string presetKey,
        string stage = "01",
        string durationToken = "")
        => Collect(new StageMaskMotionCommandSpec
        {
            stage = PresentationStageKeyParser.Parse(stage),
            presetKey = StageMaskMotionPresetDBSO.NormalizeKey(presetKey),
            durationOverride = string.IsNullOrEmpty(durationToken)
                ? -1f
                : YarnDurationParser.Parse(durationToken)
        });

    private void EnqueueSlantedMaskCutInSpec(string stage = "01", string durationToken = "")
        => EnqueueStageMaskMotionPresetSpec("slant_in", stage, durationToken);

    private void EnqueueSlantedMaskCutOutSpec(string stage = "01", string durationToken = "")
        => EnqueueStageMaskMotionPresetSpec("slant_out", stage, durationToken);

    private void EnqueueHorizontalStripOpenInSpec(string stage = "01", string durationToken = "")
        => EnqueueStageMaskMotionPresetSpec("hstrip_open", stage, durationToken);

    private void EnqueueHorizontalStripCloseOutSpec(string stage = "01", string durationToken = "")
        => EnqueueStageMaskMotionPresetSpec("hstrip_close", stage, durationToken);

    private void EnqueueHorizontalStripCutInSpec(string stage = "01", string durationToken = "")
        => EnqueueStageMaskMotionPresetSpec("hstrip_in", stage, durationToken);

    private void EnqueueHorizontalStripCutOutSpec(string stage = "01", string durationToken = "")
        => EnqueueStageMaskMotionPresetSpec("hstrip_out", stage, durationToken);

    private void EnqueueVerticalStripOpenInSpec(string stage = "01", string durationToken = "")
        => EnqueueStageMaskMotionPresetSpec("vstrip_open", stage, durationToken);

    private void EnqueueVerticalStripCloseOutSpec(string stage = "01", string durationToken = "")
        => EnqueueStageMaskMotionPresetSpec("vstrip_close", stage, durationToken);

    private void EnqueueVerticalStripCutInSpec(string stage = "01", string durationToken = "")
        => EnqueueStageMaskMotionPresetSpec("vstrip_in", stage, durationToken);

    private void EnqueueVerticalStripCutOutSpec(string stage = "01", string durationToken = "")
        => EnqueueStageMaskMotionPresetSpec("vstrip_out", stage, durationToken);

    private void EnqueueDiagonalBandCutInSpec(string stage = "01", string durationToken = "")
        => EnqueueStageMaskMotionPresetSpec("band_in", stage, durationToken);

    private void EnqueueDiagonalBandCutOutSpec(string stage = "01", string durationToken = "")
        => EnqueueStageMaskMotionPresetSpec("band_out", stage, durationToken);

    private void EnqueueCircleIrisInSpec(string stage = "01", string durationToken = "")
        => EnqueueStageMaskMotionPresetSpec("iris_in", stage, durationToken);

    private void EnqueueCircleIrisOutSpec(string stage = "01", string durationToken = "")
        => EnqueueStageMaskMotionPresetSpec("iris_out", stage, durationToken);

    // preset
    private void EnqueueDazeFadeCloseSpec(string stage = "00", string durationToken = "")
        => EnqueueStageMaskMotionPresetSpec("daze_close", stage, durationToken);

    private void EnqueueVerticalStripCoverSpec(string stage = "00", string durationToken = "")
        => EnqueueStageMaskMotionPresetSpec("strip_cover", stage, durationToken);

    private void EnqueueTransitionOutDazeFadeSpec(string stage = "00", string durationToken = "")
        => EnqueueStageMaskMotionPresetSpec("daze_open", stage, durationToken);

    private void EnqueueTransitionOutStripSpec(string stage = "00", string durationToken = "")
        => EnqueueStageMaskMotionPresetSpec("strip_clear", stage, durationToken);
    
    private void EnqueueStageMaskClearSpec()
    {
        Collect(new StageMaskClearCommandSpec
        {
            stage = PresentationStageKey.Stage00,
            mode = StageMaskClearMode.UnmaskedFullVisible,
            hideEdge = true
        });
        Collect(new StageMaskClearCommandSpec
        {
            stage = PresentationStageKey.Stage01,
            mode = StageMaskClearMode.UnmaskedFullVisible,
            hideEdge = true
        });
        Collect(new StageMaskClearCommandSpec
        {
            stage = PresentationStageKey.Stage02,
            mode = StageMaskClearMode.UnmaskedFullVisible,
            hideEdge = true
        });
    }
}