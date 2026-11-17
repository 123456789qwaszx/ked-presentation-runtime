using Ked.Presentation.Core;
public sealed partial class StageCommandParser
{
    // 덮개 — 이징을 적지 않으면 Linear(덮기 · 걷기는 투명도가 고르게 가는 것이 기본이다).
    // 덮기는 전경이다 — 다 덮일 때까지 기다린다. 걷기는 기다리지 않는다(docs/PLAN_screen-cover.md §3.1).
    private void EnqueueScreenCoverSpec(string presetKey, string durationToken, string easeToken)
        => Emit(new ScreenCoverCommandSpec
        {
            wait = true,
            presetKey = ScreenCoverPresetDBSO.NormalizeKey(presetKey),
            duration = YarnDurationParser.Parse(durationToken),
            ease = YarnEaseParser.Parse(easeToken, DG.Tweening.Ease.Linear),
        });

    private void EnqueueScreenCoverClearSpec(string durationToken, string easeToken)
        => Emit(new ScreenCoverCommandSpec
        {
            clear = true,
            duration = YarnDurationParser.Parse(durationToken),
            ease = YarnEaseParser.Parse(easeToken, DG.Tweening.Ease.Linear),
        });

    private void EnqueueScreenFlashPresetSpec(string presetKey, float intensity)
        => Emit(new ScreenFlashCommandSpec
        {
            presetKey = ScreenFlashPresetDBSO.NormalizeKey(presetKey),
            intensity = intensity,
        });

    private void EnqueueScreenFlashClearSpec()
        => Emit(new ScreenFlashCommandSpec
        {
            presetKey = "clear",
            intensity = 1f,
        });

    private void EnqueueScreenVignettePresetSpec(string presetKey, float intensity, string durationToken)
        => Emit(new ScreenVignetteCommandSpec
        {
            presetKey = ScreenVignettePresetDBSO.NormalizeKey(presetKey),
            intensity = intensity,
            duration = YarnDurationParser.Parse(durationToken),
        });

    private void EnqueueScreenVignetteClearSpec(string durationToken)
        => Emit(new ScreenVignetteCommandSpec
        {
            presetKey = "clear",
            intensity = 1f,
            duration = YarnDurationParser.Parse(durationToken),
        });

    private void EnqueueScreenNoisePresetSpec(string presetKey, float intensity, string durationToken)
        => Emit(new ScreenNoiseCommandSpec
        {
            presetKey = ScreenNoisePresetDBSO.NormalizeKey(presetKey),
            intensity = intensity,
            duration = YarnDurationParser.Parse(durationToken),
        });

    private void EnqueueScreenNoiseClearSpec(string durationToken)
        => Emit(new ScreenNoiseCommandSpec
        {
            presetKey = "clear",
            intensity = 1f,
            duration = YarnDurationParser.Parse(durationToken),
        });
}