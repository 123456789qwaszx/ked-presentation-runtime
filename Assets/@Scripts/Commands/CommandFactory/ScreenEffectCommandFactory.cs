public sealed class ScreenEffectCommandFactory : INodeCommandFactory
{
    private readonly ScreenEffectRig _screenEffects;
    private readonly ScreenFlashPresetDBSO _flashPresetDb;
    private readonly ScreenNoisePresetDBSO _noisePresetDb;
    private readonly ScreenVignettePresetDBSO _vignettePresetDb;
    private readonly UIStageDepthLayerBlurRuntime _stageDepthLayerBlurRuntime;

    public ScreenEffectCommandFactory(
        ScreenEffectRig screenEffects,
        ScreenFlashPresetDBSO flashPresetDb,
        ScreenNoisePresetDBSO noisePresetDb,
        ScreenVignettePresetDBSO vignettePresetDb,
        UIStageDepthLayerBlurRuntime stageDepthLayerBlurRuntime)
    {
        _screenEffects = screenEffects;
        _flashPresetDb = flashPresetDb;
        _noisePresetDb = noisePresetDb;
        _vignettePresetDb = vignettePresetDb;
        _stageDepthLayerBlurRuntime = stageDepthLayerBlurRuntime;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            ScreenFlashCommandSpec s    => new ScreenFlashCommand(s, _screenEffects, _flashPresetDb),
            ScreenNoiseCommandSpec s    => new ScreenNoiseCommand(s, _screenEffects, _noisePresetDb),
            ScreenVignetteCommandSpec s => new ScreenVignetteCommand(s, _screenEffects, _vignettePresetDb),
            
            StageDepthDefocusCommandSpec s => new StageDepthDefocusCommand(s, _stageDepthLayerBlurRuntime),
            _ => null
        };

        return command != null;
    }
}