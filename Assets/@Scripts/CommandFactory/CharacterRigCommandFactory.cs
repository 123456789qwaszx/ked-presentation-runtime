public sealed class CharacterRigCommandFactory : INodeCommandFactory
{
    private readonly CharRigSlotResolver _rigSlotResolver;
    private readonly CharacterRigBuilder _rigBuilder;
    private readonly PortraitResolver _portraitResolver;
    private readonly CharacterEmojiResolver _emojiResolver;

    private readonly CharStageTuningSO _globalTuning;
    private readonly RoleAnchorTuningDBSO _roleTuningDb;
    
    private readonly CharacterFocusTuningDBSO _characterFocusTuningDb;
    private readonly CharacterVisualFocusPresetDBSO _characterVisualFocusPresetDb;
    
    private readonly CharacterDepthTuningSO _characterDepthTuning;
    private readonly RoleDepthTuningDBSO _roleDepthTuningDb;
    private readonly CharacterEmojiVisualPresetSO _characterEmojiVisualPresetSo;
    
    

    public CharacterRigCommandFactory(
        CharRigSlotResolver charRigSlotResolver,
        CharacterRigBuilder charRigBuilder,
        PortraitResolver portraitResolver,
        CharacterEmojiResolver emojiResolver,
        CharStageTuningSO globalTuning,
        RoleAnchorTuningDBSO roleTuningDb,
        CharacterFocusTuningDBSO characterFocusTuningDb,
        CharacterVisualFocusPresetDBSO characterVisualFocusPresetDb,
        CharacterDepthTuningSO characterDepthTuning,
        RoleDepthTuningDBSO roleDepthTuningDb,
        CharacterEmojiVisualPresetSO characterEmojiVisualPresetSo)
    {
        _rigSlotResolver = charRigSlotResolver;
        _rigBuilder = charRigBuilder;
        _portraitResolver = portraitResolver;
        _emojiResolver = emojiResolver;
        _globalTuning = globalTuning;
        _roleTuningDb = roleTuningDb;
        _characterFocusTuningDb = characterFocusTuningDb;
        _characterVisualFocusPresetDb = characterVisualFocusPresetDb;
        _characterDepthTuning = characterDepthTuning;
        _roleDepthTuningDb = roleDepthTuningDb;
        _characterEmojiVisualPresetSo = characterEmojiVisualPresetSo;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            null => null,

            // Setup / Casting
            SetupCharRigCommandSpec s => new SetupCharRigCommand(_rigSlotResolver, _rigBuilder, s),

            CastCharacterCommandSpec s => new CastCharacterCommand(s),
            SetPortraitPoseCommandSpecCharR s => new SetPortraitPoseCommandCharR(s, _portraitResolver),

            // Layout / Base State
            SetAnchorCommandSpecCharR s => new SetAnchorCommandCharR(s, _globalTuning, _roleTuningDb),
            PlaceToCommandSpecCharR s => new PlaceToCommandCharR(s, _globalTuning, _roleTuningDb),
            SetOriginSizeCommandSpecCharR s => new SetOriginSizeCommandCharR(s, _globalTuning, _roleTuningDb),
            ApplyTrackOffsetCommandSpecCharR s => new ApplyTrackOffsetCommandCharR(s),
            
            SetDepthCommandSpecCharR s => new SetDepthCommandCharR(
                s,
                _characterDepthTuning,
                _roleDepthTuningDb,
                _characterFocusTuningDb),

            // Visibility / Root Layers
            FadeInCommandSpecCharR s => new FadeInCommandCharR(s),
            FadeOutCommandSpecCharR s => new FadeOutCommandCharR(s),
            HideRootLayersCommandSpecCharR s => new HideRootLayersCommandCharR(s),
            ShowRootLayersCommandSpecCharR s => new ShowRootLayersCommandCharR(s),

            // Basic Transform
            MoveByCommandSpecCharR s => new MoveByCommandCharR(s),
            ScaleToCommandSpecCharR s => new ScaleToCommandCharR(s),
            RotateToCommandSpecCharR s => new RotateToCommandCharR(s),
            PivotRotateToCommandSpecCharR s => new PivotRotateToCommandCharR(s),
            
            // Composition / Focus-aware Placement
            PlaceCharacterFocusCommandSpecCharR s => new PlaceCharacterFocusCommandCharR(s, _characterFocusTuningDb),

            // Visual State
            ColorToCommandSpecCharR s => new ColorToCommandCharR(s),
            SetSpriteCommandSpecCharR s => new SetSpriteCommandCharR(s),
            
            CharVisualFocusCommandSpecCharR s => new CharVisualFocusCommandCharR(s, _characterVisualFocusPresetDb),
            
            SetPortraitSpriteCommandSpecCharR s => new SetPortraitSpriteCommandCharR(s, _portraitResolver),
            SetEmotionPortraitWipeCommandSpec s => new SetEmotionPortraitWipeCommand(s, _portraitResolver),
            SetPortraitCrossfadeCommandSpecCharR s => new SetPortraitCrossfadeCommandCharR(s, _portraitResolver),

            // Idle / Loop Acting
            WalkInPlaceCommandSpecCharR s => new WalkInPlaceCommandCharR(s),
            BounceInPlaceCommandSpecCharR s => new BounceInPlaceCommandCharR(s),
            BreathInPlaceCommandSpecCharR s => new BreathInPlaceCommandCharR(s),

            // Reaction / Motion Acting
            HopCommandSpecCharR s => new HopCommandCharR(s),
            DipInOutCommandSpecCharR s => new DipInOutCommandCharR(s),
            SlideInCommandSpecCharR s => new SlideInCommandCharR(s),
            SlideOutCommandSpecCharR s => new SlideOutCommandCharR(s),
            SwayCommandSpecCharR s => new SwayCommandCharR(s),
            PunchScaleCommandSpecCharR s => new PunchScaleCommandCharR(s),
            JoltCommandSpec s => new JoltCommand(s),
            TrembleCommandSpecCharR s => new TrembleCommandCharR(s),

            // Emoji
            SetCharacterEmojiCommandSpecCharR s => new SetCharacterEmojiCommandCharR(s, _emojiResolver),
            PlaceCharacterEmojiCommandSpecCharR s => new PlaceCharacterEmojiCommandCharR(s, _emojiResolver, _characterFocusTuningDb),
            RevealCharacterEmojiCommandSpecCharR s => new RevealCharacterEmojiCommandCharR(s),
            
            SetCharacterEmojiMaterialCommandSpecCharR s => new SetCharacterEmojiMaterialCommandCharR(s, _characterEmojiVisualPresetSo),
            
            // AttachCharRigToBackgroundObjectSlot
            AttachCharRigToBackgroundObjectSlotCommandSpec s => new AttachCharRigToBackgroundObjectSlotCommand(s),

            _ => null
        };

        return command != null;
    }
}