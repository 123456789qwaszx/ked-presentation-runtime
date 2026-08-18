public sealed class ScreenEffectCommandFactory : INodeCommandFactory
{
    private readonly ScreenEffectRig _screenEffects;
    private readonly ScreenFlashPresetDBSO _flashPresetDb;
    private readonly ScreenNoisePresetDBSO _noisePresetDb;
    private readonly ScreenVignettePresetDBSO _vignettePresetDb;
    private readonly UIStageDepthLayerBlurRuntime _stageDepthLayerBlurRuntime;
    
    private readonly StageMaskMotionPresetDBSO _stageMaskMotionPresetDbSo;
    private readonly IStageMaskProvider _stageMaskProvider;
    private readonly IStageDepthContentSlotProvider _stageDepthContentSlots;

    public ScreenEffectCommandFactory(
        ScreenEffectRig screenEffects,
        ScreenFlashPresetDBSO flashPresetDb,
        ScreenNoisePresetDBSO noisePresetDb,
        ScreenVignettePresetDBSO vignettePresetDb,
        UIStageDepthLayerBlurRuntime stageDepthLayerBlurRuntime,
        StageMaskMotionPresetDBSO stageMaskMotionPresetDbSo,
        IStageMaskProvider stageMaskProvider,
        IStageDepthContentSlotProvider stageDepthContentSlots)
    {
        _screenEffects = screenEffects;
        _flashPresetDb = flashPresetDb;
        _noisePresetDb = noisePresetDb;
        _vignettePresetDb = vignettePresetDb;
        _stageDepthLayerBlurRuntime = stageDepthLayerBlurRuntime;
        _stageMaskMotionPresetDbSo = stageMaskMotionPresetDbSo;
        _stageMaskProvider = stageMaskProvider;
        _stageDepthContentSlots = stageDepthContentSlots;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            ScreenFlashCommandSpec s    => new ScreenFlashCommand(s, _screenEffects, _flashPresetDb),
            ScreenNoiseCommandSpec s    => new ScreenNoiseCommand(s, _screenEffects, _noisePresetDb),
            ScreenVignetteCommandSpec s => new ScreenVignetteCommand(s, _screenEffects, _vignettePresetDb),
            
            StageDepthDefocusCommandSpec s => new StageDepthDefocusCommand(s, _stageDepthLayerBlurRuntime),

            StageLayerBlurCommandSpec s => new StageLayerBlurCommand(s, _stageDepthContentSlots),
            
            // Stage-local Mask
            StageMaskMotionCommandSpec s => new StageMaskMotionCommand(s, _stageMaskMotionPresetDbSo, _stageMaskProvider),
            StageMaskClearCommandSpec s => new StageMaskClearCommand(s, _stageMaskProvider),
            
            _ => null
        };

        return command != null;
    }
}