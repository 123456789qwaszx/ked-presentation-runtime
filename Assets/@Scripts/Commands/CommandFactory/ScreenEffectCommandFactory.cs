public sealed class ScreenEffectCommandFactory : INodeCommandFactory
{
    private readonly ScreenEffectRig _screenEffects;
    private readonly ScreenFlashPresetDBSO _flashPresetDb;
    private readonly ScreenNoisePresetDBSO _noisePresetDb;
    private readonly ScreenVignettePresetDBSO _vignettePresetDb;
    private readonly UIStageDepthLayerBlurRuntime _stageDepthLayerBlurRuntime;
    
    private readonly StageMaskMotionPresetDBSO _stageMaskMotionPresetDbSo;

    public ScreenEffectCommandFactory(
        ScreenEffectRig screenEffects,
        ScreenFlashPresetDBSO flashPresetDb,
        ScreenNoisePresetDBSO noisePresetDb,
        ScreenVignettePresetDBSO vignettePresetDb,
        UIStageDepthLayerBlurRuntime stageDepthLayerBlurRuntime,
        StageMaskMotionPresetDBSO stageMaskMotionPresetDbSo)
    {
        _screenEffects = screenEffects;
        _flashPresetDb = flashPresetDb;
        _noisePresetDb = noisePresetDb;
        _vignettePresetDb = vignettePresetDb;
        _stageDepthLayerBlurRuntime = stageDepthLayerBlurRuntime;
        _stageMaskMotionPresetDbSo = stageMaskMotionPresetDbSo;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            ScreenFlashCommandSpec s    => new ScreenFlashCommand(s, _screenEffects, _flashPresetDb),
            ScreenNoiseCommandSpec s    => new ScreenNoiseCommand(s, _screenEffects, _noisePresetDb),
            ScreenVignetteCommandSpec s => new ScreenVignetteCommand(s, _screenEffects, _vignettePresetDb),
            
            StageDepthDefocusCommandSpec s => new StageDepthDefocusCommand(s, _stageDepthLayerBlurRuntime),
            
            // Stage-local Mask
            StageMaskMotionCommandSpec s => new StageMaskMotionCommand(s, _stageMaskMotionPresetDbSo),
            StageMaskClearCommandSpec s => new StageMaskClearCommand(s),
            
            _ => null
        };

        return command != null;
    }
}