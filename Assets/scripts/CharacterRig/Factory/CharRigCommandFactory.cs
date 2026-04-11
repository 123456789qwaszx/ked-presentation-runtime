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
            SetColorCommandSpecCharR s      => new SetColorCommandCharR(s),
            ApplyTrackOffsetCommandSpecCharR s  => new ApplyTrackOffsetCommandCharR(s),
            MoveByCommandSpecCharR s        => new MoveByCommandCharR(s),
            ScaleToCommandSpecCharR s   => new ScaleToCommandCharR(s),
            HideRootLayersCommandSpecCharR s     => new HideRootLayersCommandCharR(s),
            SetAnchorCommandSpecCharR s     => new SetAnchorCommandCharR(s),
            ShowRootLayersCommandSpecCharR s     => new ShowRootLayersCommandCharR(s),
            DestroyCommandSpec s            => new DestroyCommand(s),
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
            JoltCommandSpecCharR s    => new JoltCommandCharR(s),
            
            SetEmotionPortraitWipeCommandSpecCharR s    => new SetEmotionPortraitWipeCommandCharR(s, _portraitResolver),
            ShowEmojiCommandSpecCharR s    => new ShowEmojiCommandCharR(s),
            SetPortraitSpriteCommandSpecCharR s    => new SetPortraitSpriteCommandCharR(s, _portraitResolver),
            PivotRotateToCommandSpecCharR s => new PivotRotateToCommandCharR(s),
            SetPortraitCrossfadeCommandSpecCharR s => new SetPortraitCrossfadeCommandCharR(s, _portraitResolver),
            
            
            _ => null
        };

        return command != null;
    }
}