using System;

public sealed class ScreenEffectCommandFactory : INodeCommandFactory
{
    private readonly PresentationStage _stage;
    private readonly ScreenEffectRig _screenEffects;
    private readonly ScreenFlashPresetDBSO _flashPresetDb;
    private readonly ScreenNoisePresetDBSO _noisePresetDb;
    private readonly ScreenVignettePresetDBSO _vignettePresetDb;
    
    private readonly StageMaskMotionPresetDBSO _stageMaskMotionPresetDb;
    private readonly StageRigRegistry _stages;
    private readonly StageFrameRegistry _frames;
    private readonly RigBlurIntentSystem _rigBlur;
    private readonly CutInPresetDBSO _cutInPreset;
    private readonly PopupPresetDBSO _popupPreset;

    public ScreenEffectCommandFactory(
        PresentationStage stage,
        ScreenEffectRig screenEffects,
        ScreenFlashPresetDBSO flashPresetDb,
        ScreenNoisePresetDBSO noisePresetDb,
        ScreenVignettePresetDBSO vignettePresetDb,
        StageMaskMotionPresetDBSO stageMaskMotionPresetDb,
        StageRigRegistry stages,
        StageFrameRegistry frames,
        RigBlurIntentSystem rigBlur,
        CutInPresetDBSO cutInPreset = null,
        PopupPresetDBSO popupPreset = null)
    {
        _stage = stage ?? throw new ArgumentNullException(nameof(stage));
        _screenEffects = screenEffects ? screenEffects : throw new ArgumentNullException(nameof(screenEffects));
        _flashPresetDb = flashPresetDb ? flashPresetDb : throw new ArgumentNullException(nameof(flashPresetDb));
        _noisePresetDb = noisePresetDb ? noisePresetDb : throw new ArgumentNullException(nameof(noisePresetDb));
        _vignettePresetDb = vignettePresetDb ? vignettePresetDb : throw new ArgumentNullException(nameof(vignettePresetDb));
        _stageMaskMotionPresetDb = stageMaskMotionPresetDb ? stageMaskMotionPresetDb : throw new ArgumentNullException(nameof(stageMaskMotionPresetDb));
        _stages = stages ?? throw new ArgumentNullException(nameof(stages));
        _frames = frames;
        _rigBlur = rigBlur ?? throw new ArgumentNullException(nameof(rigBlur));

        // 프리셋 에셋이 없으면 코드 기본값(원본 게임 실측)으로 — 조용히 넘기지 않는다.
        if (cutInPreset == null)
        {
            UnityEngine.Debug.LogWarning(
                "[ScreenEffectCommandFactory] 컷인 프리셋(CutInPresetDBSO)이 연결되지 않았다. 코드 기본값으로 연다 — " +
                "Create › Ked › Presentation › Cut-in Preset으로 에셋을 만들어 조립에 연결하면 런타임에서 편집할 수 있다.");
            cutInPreset = CutInPresetDBSO.CreateDefault();
        }

        _cutInPreset = cutInPreset;

        if (popupPreset == null)
        {
            UnityEngine.Debug.LogWarning(
                "[ScreenEffectCommandFactory] 그림 팝업 프리셋(PopupPresetDBSO)이 연결되지 않았다. 코드 기본값(원본 실측)으로 연다 — " +
                "Create › Ked › Presentation › Popup Preset으로 에셋을 만들어 조립에 연결하면 런타임에서 편집할 수 있다.");
            popupPreset = PopupPresetDBSO.CreateDefault();
        }

        _popupPreset = popupPreset;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            ScreenFlashCommandSpec s    => new ScreenFlashCommand(s, _screenEffects, _flashPresetDb),
            ScreenNoiseCommandSpec s    => new ScreenNoiseCommand(s, _screenEffects, _noisePresetDb),
            ScreenVignetteCommandSpec s => new ScreenVignetteCommand(s, _screenEffects, _vignettePresetDb),
            
            StageLayerBlurCommandSpec s => new StageLayerBlurCommand(_rigBlur, s),
            
            // Stage-local Mask
            StageMaskMotionCommandSpec s => new StageMaskMotionCommand(s, _stageMaskMotionPresetDb, _stages, _frames),
            StageMaskClearCommandSpec s => new StageMaskClearCommand(s, _stages, _frames),

            // 컷인 — 덮는 무대의 창 · 판 · 마스크(docs/PLAN_cutin.md)
            CutInOpenCommandSpec s => new CutInOpenCommand(s, _cutInPreset, _frames),
            CutInCloseCommandSpec s => new CutInCloseCommand(s, _cutInPreset, _frames),

            // 그림 팝업 — 덮는 무대의 뒷막 + 그림(docs/PLAN_popup.md)
            PopupOpenCommandSpec s => new PopupOpenCommand(s, _popupPreset, _frames),
            PopupCloseCommandSpec s => new PopupCloseCommand(s, _popupPreset, _frames),
            
            _ => null
        };

        return command != null;
    }
}