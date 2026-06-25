public sealed partial class YarnCommandBridge
{
    // 공통 진입점 — char_visual의 EnqueueCharVisualPresetSpec에 대응.
    private void EnqueueStageMaskMotionPresetSpec(
        string presetKey,
        string stage = "01",
        float duration = -1f)
    {
        var spec = new StageMaskMotionCommandSpec
        {
            stage = PresentationStageKeyParser.Parse(stage, PresentationStageKey.Stage01),
            presetKey = StageMaskMotionPresetDBSO.NormalizeKey(presetKey),
            durationOverride = duration,
            wait = false
        };

        Collect(spec);
    }

// 개별 트랜지션 = preset key만 고정하는 얇은 래퍼.
    private void EnqueueSlantedMaskCutInSpec(string stage = "01", float duration = -1f)
        => EnqueueStageMaskMotionPresetSpec("slant_in", stage, duration);

    private void EnqueueSlantedMaskCutOutSpec(string stage = "01", float duration = -1f)
        => EnqueueStageMaskMotionPresetSpec("slant_out", stage, duration);

    private void EnqueueHorizontalStripOpenInSpec(string stage = "01", float duration = -1f)
        => EnqueueStageMaskMotionPresetSpec("hstrip_open", stage, duration);

    private void EnqueueHorizontalStripCloseOutSpec(string stage = "01", float duration = -1f)
        => EnqueueStageMaskMotionPresetSpec("hstrip_close", stage, duration);

    private void EnqueueHorizontalStripCutInSpec(string stage = "01", float duration = -1f)
        => EnqueueStageMaskMotionPresetSpec("hstrip_in", stage, duration);

    private void EnqueueHorizontalStripCutOutSpec(string stage = "01", float duration = -1f)
        => EnqueueStageMaskMotionPresetSpec("hstrip_out", stage, duration);

    private void EnqueueVerticalStripOpenInSpec(string stage = "01", float duration = -1f)
        => EnqueueStageMaskMotionPresetSpec("vstrip_open", stage, duration);

    private void EnqueueVerticalStripCloseOutSpec(string stage = "01", float duration = -1f)
        => EnqueueStageMaskMotionPresetSpec("vstrip_close", stage, duration);

    private void EnqueueVerticalStripCutInSpec(string stage = "01", float duration = -1f)
        => EnqueueStageMaskMotionPresetSpec("vstrip_in", stage, duration);

    private void EnqueueVerticalStripCutOutSpec(string stage = "01", float duration = -1f)
        => EnqueueStageMaskMotionPresetSpec("vstrip_out", stage, duration);

    private void EnqueueDiagonalBandCutInSpec(string stage = "01", float duration = -1f)
        => EnqueueStageMaskMotionPresetSpec("band_in", stage, duration);

    private void EnqueueDiagonalBandCutOutSpec(string stage = "01", float duration = -1f)
        => EnqueueStageMaskMotionPresetSpec("band_out", stage, duration);

    private void EnqueueCircleIrisInSpec(string stage = "01", float duration = -1f)
        => EnqueueStageMaskMotionPresetSpec("iris_in", stage, duration);

    private void EnqueueCircleIrisOutSpec(string stage = "01", float duration = -1f)
        => EnqueueStageMaskMotionPresetSpec("iris_out", stage, duration);

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

    private void EnqueueDazeFadeCloseSpec(string stage = "00", float duration = -1f)        // tx_daze
        => EnqueueStageMaskMotionPresetSpec("daze_close", stage, duration);

    private void EnqueueVerticalStripCoverSpec(string stage = "00", float duration = -1f)   // tx_strip
        => EnqueueStageMaskMotionPresetSpec("strip_cover", stage, duration);

    private void EnqueueTransitionOutDazeFadeSpec(string stage = "00", float duration = -1f) // tx_out_daze
        => EnqueueStageMaskMotionPresetSpec("daze_open", stage, duration);

    private void EnqueueTransitionOutStripSpec(string stage = "00", float duration = -1f)    // tx_out_strip
        => EnqueueStageMaskMotionPresetSpec("strip_clear", stage, duration);
}