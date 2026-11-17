using System;

public sealed class ScreenEffectCommandFactory : INodeCommandFactory
{
    private readonly ScreenEffectRig _screenEffects;
    private readonly ScreenFlashPresetDBSO _flashPresetDb;
    private readonly ScreenNoisePresetDBSO _noisePresetDb;
    private readonly ScreenVignettePresetDBSO _vignettePresetDb;
    
    private readonly StageMaskMotionPresetDBSO _stageMaskMotionPresetDb;
    private readonly StageRigRegistry _stages;
    private readonly RigBlurIntentSystem _rigBlur;
    private readonly ScreenCoverPresetDBSO _coverPresetDb;

    public ScreenEffectCommandFactory(
        ScreenEffectRig screenEffects,
        ScreenFlashPresetDBSO flashPresetDb,
        ScreenNoisePresetDBSO noisePresetDb,
        ScreenVignettePresetDBSO vignettePresetDb,
        StageMaskMotionPresetDBSO stageMaskMotionPresetDb,
        StageRigRegistry stages,
        RigBlurIntentSystem rigBlur,
        ScreenCoverPresetDBSO coverPresetDb = null)
    {
        _screenEffects = screenEffects ? screenEffects : throw new ArgumentNullException(nameof(screenEffects));
        _flashPresetDb = flashPresetDb ? flashPresetDb : throw new ArgumentNullException(nameof(flashPresetDb));
        _noisePresetDb = noisePresetDb ? noisePresetDb : throw new ArgumentNullException(nameof(noisePresetDb));
        _vignettePresetDb = vignettePresetDb ? vignettePresetDb : throw new ArgumentNullException(nameof(vignettePresetDb));
        _stageMaskMotionPresetDb = stageMaskMotionPresetDb ? stageMaskMotionPresetDb : throw new ArgumentNullException(nameof(stageMaskMotionPresetDb));
        _stages = stages ?? throw new ArgumentNullException(nameof(stages));
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
            
            StageLayerBlurCommandSpec s => new StageLayerBlurCommand(_rigBlur, s),

            // 어둡게 — 무대 리그의 층마다 선 어둡기 판(docs/PLAN_screen-cover.md §3.2)
            StageLayerDimCommandSpec s => new StageLayerDimCommand(s, _stages),
            
            // Stage-local Mask
            StageMaskMotionCommandSpec s => new StageMaskMotionCommand(s, _stageMaskMotionPresetDb, _stages),
            StageMaskClearCommandSpec s => new StageMaskClearCommand(s, _stages),

            _ => null
        };

        return command != null;
    }
}