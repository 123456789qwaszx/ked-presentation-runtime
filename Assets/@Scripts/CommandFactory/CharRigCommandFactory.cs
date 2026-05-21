using UnityEngine;

public enum CharRigDirection
{
    Left = 0,
    Right,
    Up,
    Down,
}

public sealed class CharRigCommandFactory : INodeCommandFactory
{
    private readonly CharRigSlotResolver _rigSlotResolver;
    private readonly CharacterRigBuilder _rigBuilder;
    private readonly PortraitResolver _portraitResolver;
    private readonly CharacterEmojiResolver _emojiResolver;
    
    private readonly  CharStageTuningSO _globalTuning;
    private readonly  RoleAnchorTuningDBSO _roleTuningDb;

    public CharRigCommandFactory(
        CharRigSlotResolver charRigSlotResolver,
        CharacterRigBuilder charRigBuilder,
        PortraitResolver portraitResolver,
        CharacterEmojiResolver emojiResolver,
        CharStageTuningSO globalTuning,
        RoleAnchorTuningDBSO roleTuningDb
        )
    {
        _rigSlotResolver = charRigSlotResolver;
        _rigBuilder = charRigBuilder;
        _portraitResolver = portraitResolver;
        _emojiResolver = emojiResolver;
        _globalTuning = globalTuning;
        _roleTuningDb = roleTuningDb;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            null => null,
            
            SetupCharRigCommandSpec s         => new SetupCharRigCommand(_rigSlotResolver, _rigBuilder, s),
            
            CastCharacterCommandSpec s => new CastCharacterCommand(s),
            UncastCharacterCommandSpec s => new UncastCharacterCommand(s),
            
            SetAnchorCommandSpecCharR s     => new SetAnchorCommandCharR(s, _globalTuning, _roleTuningDb),
            
            
            DestroyCommandSpec s            => new DestroyCommand(s),
            ClearCharRigRefsCommandSpec s => new ClearCharRigRefsCommand(s),
            
            SetColorCommandSpecCharR s      => new SetColorCommandCharR(s),
            ApplyTrackOffsetCommandSpecCharR s  => new ApplyTrackOffsetCommandCharR(s),
            MoveByCommandSpecCharR s        => new MoveByCommandCharR(s),
            ScaleToCommandSpecCharR s   => new ScaleToCommandCharR(s),
            HideRootLayersCommandSpecCharR s     => new HideRootLayersCommandCharR(s),
            ShowRootLayersCommandSpecCharR s     => new ShowRootLayersCommandCharR(s),
            SetOriginSizeCommandSpecCharR s => new SetOriginSizeCommandCharR(s, _globalTuning, _roleTuningDb),
            FadeOutCommandSpecCharR s       => new FadeOutCommandCharR(s),
            FadeInCommandSpecCharR s        => new FadeInCommandCharR(s),
            SwayCommandSpecCharR s          => new SwayCommandCharR(s),
            PunchScaleCommandSpecCharR s    => new PunchScaleCommandCharR(s),
            RotateToCommandSpecCharR s  => new RotateToCommandCharR(s),
            
            HopCommandSpecCharR s    => new HopCommandCharR(s),
            WalkInPlaceCommandSpecCharR s => new WalkInPlaceCommandCharR(s),
            BounceInPlaceCommandSpecCharR s => new BounceInPlaceCommandCharR(s),
            BreathInPlaceCommandSpecCharR s => new BreathInPlaceCommandCharR(s),
            
            DipInOutCommandSpecCharR s    => new DipInOutCommandCharR(s),
            SlideInCommandSpecCharR s    => new SlideInCommandCharR(s),
            SlideOutCommandSpecCharR s    => new SlideOutCommandCharR(s),
            JoltCommandSpec s    => new JoltCommand(s),
            TrembleCommandSpecCharR s => new TrembleCommandCharR(s),
            
            
            SetEmotionPortraitWipeCommandSpec s    => new SetEmotionPortraitWipeCommand(s, _portraitResolver),
            SetPortraitSpriteCommandSpecCharR s    => new SetPortraitSpriteCommandCharR(s, _portraitResolver),
            PivotRotateToCommandSpecCharR s => new PivotRotateToCommandCharR(s),
            SetPortraitCrossfadeCommandSpecCharR s => new SetPortraitCrossfadeCommandCharR(s, _portraitResolver),
            
            //ShowEmojiCommandSpecCharR s    => new ShowEmojiCommandCharR(s),
            SetSpriteCommandSpecCharR s     => new SetSpriteCommandCharR(s),
            
            
            
            // 새 커맨드
            SetCharacterEmojiCommandSpecCharR s => new SetCharacterEmojiCommandCharR(s, _emojiResolver),

            
            _ => null
        };

        return command != null;
    }
}