public enum CharRDirection
{
    Left = 0,
    Right,
    Up,
    Down,
}

public sealed class CharRigCommandFactory : INodeCommandFactory
{
    private readonly CharacterRigAccess _access;
    private readonly PortraitResolver _portraitResolver;

    public CharRigCommandFactory(CharacterRigAccess access, PortraitResolver portraitResolver)
    {
        _access = access;
        _portraitResolver = portraitResolver;
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            null => null,
            
            SetupCharRigCommandSpec s         => new SetupCharRigCommand(_access, s),
            DestroyCommandSpec s            => new DestroyCommand(s),
            ClearCharRigRefsCommandSpec s => new ClearCharRigRefsCommand(s),
            
            SetColorCommandSpecCharR s      => new SetColorCommandCharR(s),
            ApplyTrackOffsetCommandSpecCharR s  => new ApplyTrackOffsetCommandCharR(s),
            MoveByCommandSpecCharR s        => new MoveByCommandCharR(s),
            ScaleToCommandSpecCharR s   => new ScaleToCommandCharR(s),
            HideRootLayersCommandSpecCharR s     => new HideRootLayersCommandCharR(s),
            SetAnchorCommandSpecCharR s     => new SetAnchorCommandCharR(s),
            ShowRootLayersCommandSpecCharR s     => new ShowRootLayersCommandCharR(s),
            SetOriginSizeCommandSpecCharR s => new SetOriginSizeCommandCharR(s),
            FadeOutCommandSpecCharR s       => new FadeOutCommandCharR(s),
            FadeInCommandSpecCharR s        => new FadeInCommandCharR(s),
            SwayCommandSpecCharR s          => new SwayCommandCharR(s),
            PunchScaleCommandSpecCharR s    => new PunchScaleCommandCharR(s),
            RotateToCommandSpecCharR s  => new RotateToCommandCharR(s),
            
            ArcHopInCommandSpecCharR s    => new ArcHopInCommandCharR(s),
            
            DipInOutCommandSpecCharR s    => new DipInOutCommandCharR(s),
            SlideInCommandSpecCharR s    => new SlideInCommandCharR(s),
            SlideOutCommandSpecCharR s    => new SlideOutCommandCharR(s),
            JoltCommandSpec s    => new JoltCommand(s),
            
            SetEmotionPortraitWipeCommandSpec s    => new SetEmotionPortraitWipeCommand(s, _portraitResolver),
            SetPortraitSpriteCommandSpecCharR s    => new SetPortraitSpriteCommandCharR(s, _portraitResolver),
            PivotRotateToCommandSpecCharR s => new PivotRotateToCommandCharR(s),
            SetPortraitCrossfadeCommandSpecCharR s => new SetPortraitCrossfadeCommandCharR(s, _portraitResolver),
            
            ShowEmojiCommandSpecCharR s    => new ShowEmojiCommandCharR(s),
            SetSpriteCommandSpecCharR s     => new SetSpriteCommandCharR(s),
            
            CastCharacterCommandSpec s => new CastCharacterCommand(s),
            UncastCharacterCommandSpec s => new UncastCharacterCommand(s),
            
            _ => null
        };

        return command != null;
    }
}