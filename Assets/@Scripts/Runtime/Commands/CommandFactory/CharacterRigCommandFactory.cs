public sealed class CharacterRigCommandFactory : INodeCommandFactory
{
    private readonly CharRigSlotResolver _rigSlotResolver;
    private readonly CharacterRigBuilder _rigBuilder;
    private readonly PortraitResolver _portraitResolver;
    private readonly CharacterEmojiResolver _emojiResolver;

    private readonly RoleAnchorTuningDBSO _roleTuningDb;
    
    private readonly CharacterFocusTuningDBSO _characterFocusTuningDb;
    private readonly CharacterVisualFocusPresetDBSO _characterVisualFocusPresetDb;
    
    private readonly CharacterDepthTuningSO _characterDepthTuning;
    private readonly CharacterEmojiVisualPresetSO _characterEmojiVisualPresetSo;
    private readonly IShotResponseStageProvider _stageProvider;

    public CharacterRigCommandFactory(
        CharRigSlotResolver charRigSlotResolver,
        CharacterRigBuilder charRigBuilder,
        PortraitResolver portraitResolver,
        CharacterEmojiResolver emojiResolver,
        RoleAnchorTuningDBSO roleTuningDb,
        CharacterFocusTuningDBSO characterFocusTuningDb,
        CharacterVisualFocusPresetDBSO characterVisualFocusPresetDb,
        CharacterDepthTuningSO characterDepthTuning,
        CharacterEmojiVisualPresetSO characterEmojiVisualPresetSo,
        IShotResponseStageProvider stageProvider)
    {
        _rigSlotResolver = charRigSlotResolver;
        _rigBuilder = charRigBuilder;
        _portraitResolver = portraitResolver;
        _emojiResolver = emojiResolver;
        _roleTuningDb = roleTuningDb;
        _characterFocusTuningDb = characterFocusTuningDb;
        _characterVisualFocusPresetDb = characterVisualFocusPresetDb;
        _characterDepthTuning = characterDepthTuning;
        _characterEmojiVisualPresetSo = characterEmojiVisualPresetSo;
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
            
            SetDepthCommandSpecCharR s => new SetDepthCommandCharR(
                s,
                _characterDepthTuning,
                _characterFocusTuningDb,
                _stageProvider),

            // Visibility / Root Layers
            FadeInCommandSpecCharR s => new FadeInCommandCharR(s),
            FadeOutCommandSpecCharR s => new FadeOutCommandCharR(s),

            // Basic Transform
            MoveByCommandSpecCharR s => new MoveByCommandCharR(s),
            ScaleToCommandSpecCharR s => new ScaleToCommandCharR(s),
            RotateToCommandSpecCharR s => new RotateToCommandCharR(s),
            RotateByCommandSpecCharR s => new RotateByCommandCharR(s),
            PivotRotateToCommandSpecCharR s => new PivotRotateToCommandCharR(s),
            
            // Composition / Focus-aware Placement
            PlaceCharacterFocusCommandSpecCharR s => new PlaceCharacterFocusCommandCharR(s, _characterFocusTuningDb, _stageProvider),

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
            EmojiMoveByCommandSpecCharR s => new EmojiMoveByCommandCharR(s, _emojiResolver),
            EmojiRotateToCommandSpecCharR s => new EmojiRotateToCommandCharR(s, _emojiResolver),
            EmojiRotateByCommandSpecCharR s => new EmojiRotateByCommandCharR(s, _emojiResolver),
            EmojiSwayCommandSpecCharR s => new EmojiSwayCommandCharR(s, _emojiResolver),
            EmojiJoltCommandSpec s => new EmojiJoltCommand(s, _emojiResolver),
            EmojiTrembleCommandSpecCharR s => new EmojiTrembleCommandCharR(s, _emojiResolver),
            EmojiSlideInCommandSpecCharR s => new EmojiSlideInCommandCharR(s, _emojiResolver),

            SetCharacterEmojiCommandSpecCharR s => new SetCharacterEmojiCommandCharR(s, _emojiResolver),
            PlaceCharacterEmojiCommandSpecCharR s => new PlaceCharacterEmojiCommandCharR(s, _emojiResolver, _characterFocusTuningDb, _stageProvider),
            RevealCharacterEmojiCommandSpecCharR s => new RevealCharacterEmojiCommandCharR(s),
            
            InitCharacterEmojiCommandSpecCharR s => new InitCharacterEmojiCommandCharR(s, _emojiResolver, _characterEmojiVisualPresetSo, _characterFocusTuningDb, _stageProvider),
            
            AnimateCharacterEmojiFramesCommandSpecCharR s => new AnimateCharacterEmojiFramesCommandCharR(s, _emojiResolver),
            SpringAppearCommandSpecCharR s => new SpringAppearCommandCharR(s, _emojiResolver),
            EmojiHeartPaperPlaneCommandSpecCharR s => new EmojiHeartPaperPlaneCommandCharR(s, _emojiResolver),
            EmojiChatterWiggleCommandSpecCharR s => new EmojiChatterWiggleCommandCharR(s, _emojiResolver),
            EmojiIdleDoublePulseCommandSpecCharR s => new EmojiIdleDoublePulseCommandCharR(s),
            
            // AttachCharRigToBackgroundObjectSlot
            AttachCharRigToBackgroundObjectSlotCommandSpec s => new AttachCharRigToBackgroundObjectSlotCommand(s),

            _ => null
        };

        return command != null;
    }
}