public sealed class CharacterRigCommandFactory : INodeCommandFactory
{
    private readonly CharRigSlotResolver _rigSlotResolver;
    private readonly CharacterRigBuilder _rigBuilder;
    private readonly PortraitResolver _portraitResolver;

    private readonly RoleAnchorTuningDBSO _roleTuningDb;
    
    private readonly CharacterFocusTuningDBSO _characterFocusTuningDb;
    private readonly CharacterVisualFocusPresetDBSO _characterVisualFocusPresetDb;
    
    private readonly CharacterDepthTuningSO _characterDepthTuning;
    private readonly IShotResponseStageProvider _stageProvider;

    public CharacterRigCommandFactory(
        CharRigSlotResolver charRigSlotResolver,
        CharacterRigBuilder charRigBuilder,
        PortraitResolver portraitResolver,
        RoleAnchorTuningDBSO roleTuningDb,
        CharacterFocusTuningDBSO characterFocusTuningDb,
        CharacterVisualFocusPresetDBSO characterVisualFocusPresetDb,
        CharacterDepthTuningSO characterDepthTuning,
        IShotResponseStageProvider stageProvider)
    {
        _rigSlotResolver = charRigSlotResolver;
        _rigBuilder = charRigBuilder;
        _portraitResolver = portraitResolver;
        _roleTuningDb = roleTuningDb;
        _characterFocusTuningDb = characterFocusTuningDb;
        _characterVisualFocusPresetDb = characterVisualFocusPresetDb;
        _characterDepthTuning = characterDepthTuning;
        _stageProvider = stageProvider;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            null => null,

            // Setup / Casting
            SetupCharRigCommandSpec s => new SetupCharRigCommand(_rigSlotResolver, _rigBuilder, s),
            CastCharacterCommandSpec s => new CastCharacterCommand(s),
            SetPortraitPoseCommandSpecCharR s => new SetPortraitPoseCommandCharR(s),

            // Layout / Base State
            SetAnchorCommandSpecCharR s => new SetAnchorCommandCharR(s, _roleTuningDb),
            ApplyTrackOffsetCommandSpecCharR s => new ApplyTrackOffsetCommandCharR(s),
            MirrorCharacterCommandSpecCharR s => new MirrorCharacterCommandCharR(s),

            SetCharacterSiblingOrderCommandSpecCharR s => new SetCharacterSiblingOrderCommandCharR(s),
            MoveCharacterRigToStageLayerCommandSpecCharR s => new MoveCharacterRigToStageLayerCommandCharR(s, _rigSlotResolver),
            
            SetDepthCommandSpecCharR s => new SetDepthCommandCharR(s, _characterDepthTuning, _characterFocusTuningDb, _stageProvider),

            // Visibility / Root Layers
            FadeInCommandSpecCharR s => new FadeInCommandCharR(s),
            FadeOutCommandSpecCharR s => new FadeOutCommandCharR(s),

            // Basic Transform
            MoveByCommandSpecCharR s => new MoveByCommandCharR(s),
            ScaleToCommandSpecCharR s => new ScaleToCommandCharR(s),
            RotateToCommandSpecCharR s => new RotateToCommandCharR(s),
            RotateByCommandSpecCharR s => new RotateByCommandCharR(s),
            GestureCommandSpecCharR s => new GestureCommandCharR(s),
            SlideInCommandSpecCharR s => new SlideInCommandCharR(s),
            SlideOutCommandSpecCharR s => new SlideOutCommandCharR(s),
            SetPortraitSpriteCommandSpecCharR s => new SetPortraitSpriteCommandCharR(s, _portraitResolver),
            SetEmotionPortraitWipeCommandSpecCharR s => new SetEmotionPortraitWipeCommandCharR(s, _portraitResolver),
            
            // Composition / Focus-aware Placement
            PlaceCharacterFocusCommandSpecCharR s => new PlaceCharacterFocusCommandCharR(s, _characterFocusTuningDb, _stageProvider),

            // Visual State
            SetSpriteCommandSpecCharR s => new SetSpriteCommandCharR(s),
            
            CharVisualFocusCommandSpecCharR s => new CharVisualFocusCommandCharR(s, _characterVisualFocusPresetDb),
            DepthFocusCommandSpecCharR s => new DepthFocusCommandCharR(s),

            _ => null
        };

        return command != null;
    }
}