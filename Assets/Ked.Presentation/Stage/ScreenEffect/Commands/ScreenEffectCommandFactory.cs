using System;

// 화면 효과 리그의 커맨드 — 덮개 · 플래시 · 노이즈 · 비네트(모든 무대 위, 화면 전체).
// screen_blur는 블러 시스템(RigBlurIntentSystem)의 것인데 아직 여기서 만든다(docs/PLAN_stage-code-homes.md §5).
public sealed class ScreenEffectCommandFactory : INodeCommandFactory
{
    private readonly ScreenEffectRig _screenEffects;
    private readonly ScreenFlashPresetDBSO _flashPresetDb;
    private readonly ScreenNoisePresetDBSO _noisePresetDb;
    private readonly ScreenVignettePresetDBSO _vignettePresetDb;
    private readonly RigBlurIntentSystem _rigBlur;
    private readonly ScreenCoverPresetDBSO _coverPresetDb;

    public ScreenEffectCommandFactory(
        ScreenEffectRig screenEffects,
        ScreenFlashPresetDBSO flashPresetDb,
        ScreenNoisePresetDBSO noisePresetDb,
        ScreenVignettePresetDBSO vignettePresetDb,
        RigBlurIntentSystem rigBlur,
        ScreenCoverPresetDBSO coverPresetDb = null)
    {
        _screenEffects = screenEffects ? screenEffects : throw new ArgumentNullException(nameof(screenEffects));
        _flashPresetDb = flashPresetDb ? flashPresetDb : throw new ArgumentNullException(nameof(flashPresetDb));
        _noisePresetDb = noisePresetDb ? noisePresetDb : throw new ArgumentNullException(nameof(noisePresetDb));
        _vignettePresetDb = vignettePresetDb ? vignettePresetDb : throw new ArgumentNullException(nameof(vignettePresetDb));
        _rigBlur = rigBlur ?? throw new ArgumentNullException(nameof(rigBlur));

        if (coverPresetDb == null)
        {
            UnityEngine.Debug.LogWarning(
                "[ScreenEffectCommandFactory] 덮개 프리셋(ScreenCoverPresetDBSO)이 연결되지 않았다. 코드 기본값(black · white)으로 덮는다 — " +
                "Create › CPS › ScreenEffect › Cover Preset DB로 에셋을 만들어 조립에 연결하면 색을 더할 수 있다.");
            coverPresetDb = ScreenCoverPresetDBSO.CreateDefault();
        }

        _coverPresetDb = coverPresetDb;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            // 덮개 — 화면 전체를 덮고 걷는다(docs/PLAN_screen-cover.md)
            ScreenCoverCommandSpec s    => new ScreenCoverCommand(s, _screenEffects, _coverPresetDb),

            ScreenFlashCommandSpec s    => new ScreenFlashCommand(s, _screenEffects, _flashPresetDb),
            ScreenNoiseCommandSpec s    => new ScreenNoiseCommand(s, _screenEffects, _noisePresetDb),
            ScreenVignetteCommandSpec s => new ScreenVignetteCommand(s, _screenEffects, _vignettePresetDb),

            // 층 블러 — 블러 시스템에 의도만 넘긴다
            StageLayerBlurCommandSpec s => new StageLayerBlurCommand(_rigBlur, s),

            _ => null
        };

        return command != null;
    }
}