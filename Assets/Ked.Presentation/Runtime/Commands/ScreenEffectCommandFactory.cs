using System;

public sealed class ScreenEffectCommandFactory : INodeCommandFactory
{
    private readonly PresentationStage _stage;
    private readonly ScreenEffectRig _screenEffects;
    private readonly ScreenFlashPresetDBSO _flashPresetDb;
    private readonly ScreenNoisePresetDBSO _noisePresetDb;
    private readonly ScreenVignettePresetDBSO _vignettePresetDb;
    
    private readonly StageMaskMotionPresetDBSO _stageMaskMotionPresetDb;
    private readonly IStageMaskProvider _stageMaskProvider;
    private readonly RigBlurIntentSystem _rigBlur;

    public ScreenEffectCommandFactory(
        PresentationStage stage,
        ScreenEffectRig screenEffects,
        ScreenFlashPresetDBSO flashPresetDb,
        ScreenNoisePresetDBSO noisePresetDb,
        ScreenVignettePresetDBSO vignettePresetDb,
        StageMaskMotionPresetDBSO stageMaskMotionPresetDb,
        IStageMaskProvider stageMaskProvider,
        RigBlurIntentSystem rigBlur)
    {
        _stage = stage ?? throw new ArgumentNullException(nameof(stage));
        _screenEffects = screenEffects ? screenEffects : throw new ArgumentNullException(nameof(screenEffects));
        _flashPresetDb = flashPresetDb ? flashPresetDb : throw new ArgumentNullException(nameof(flashPresetDb));
        _noisePresetDb = noisePresetDb ? noisePresetDb : throw new ArgumentNullException(nameof(noisePresetDb));
        _vignettePresetDb = vignettePresetDb ? vignettePresetDb : throw new ArgumentNullException(nameof(vignettePresetDb));
        _stageMaskMotionPresetDb = stageMaskMotionPresetDb ? stageMaskMotionPresetDb : throw new ArgumentNullException(nameof(stageMaskMotionPresetDb));
        _stageMaskProvider = stageMaskProvider ?? throw new ArgumentNullException(nameof(stageMaskProvider));
        _rigBlur = rigBlur ?? throw new ArgumentNullException(nameof(rigBlur));
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
            StageMaskMotionCommandSpec s => new StageMaskMotionCommand(s, _stageMaskMotionPresetDb, _stageMaskProvider),
            StageMaskClearCommandSpec s => new StageMaskClearCommand(s, _stageMaskProvider),
            
            _ => null
        };

        return command != null;
    }
}