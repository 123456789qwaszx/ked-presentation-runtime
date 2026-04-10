public enum SlideFromCharR
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
            
            SetCharRigCommandSpec s         => new SetCharRigCommand(_access, s),
            SetColorCommandSpecCharR s      => new SetColorCommandCharR(s),
            ResetTrackOffsetsCommandSpec s  => new ResetTrackOffsetsCommand(s),
            SetRotationCommandSpecCharR s   => new SetRotationCommandCharR(s),
            MoveByCommandSpecCharR s        => new MoveByCommandCharR(s),
            ScaleFromToCommandSpecCharR s   => new ScaleFromToCommandCharR(s),
            HideRootsCommandSpecCharR s     => new HideRootsCommandCharR(s),
            SetAnchorCommandSpecCharR s     => new SetAnchorCommandCharR(s),
            ShowRootsCommandSpecCharR s     => new ShowRootsCommandCharR(s),
            DestroyCommandSpec s            => new DestroyCommand(s),
            SetOriginSizeCommandSpecCharR s => new SetOriginSizeCommandCharR(s),
            FadeOutCommandSpecCharR s       => new FadeOutCommandCharR(s),
            FadeInCommandSpecCharR s        => new FadeInCommandCharR(s),
            SwayCommandSpecCharR s          => new SwayCommandCharR(s),
            PunchScaleCommandSpecCharR s    => new PunchScaleCommandCharR(s),
            RotateFromToCommandSpecCharR s  => new RotateFromToCommandCharR(s),
            
            BounceArcInCommandSpecCharR s    => new BounceArcInCommandCharR(s),
            
            DipInOutCommandSpecCharR s    => new DipInOutCommandCharR(s),
            JuicySlideInCommandSpecCharR s    => new JuicySlideInCommandCharR(s),
            JuicySlideOutCommandSpecCharR s    => new JuicySlideOutCommandCharR(s),
            NudgeTapCommandSpecCharR s    => new NudgeTapCommandCharR(s),
            
            SetEmotionPortraitWipeCommandSpecCharR s    => new SetEmotionPortraitWipeCommandCharR(s, _portraitResolver),
            ShowEmojiCommandSpecCharR s    => new ShowEmojiCommandCharR(s),
            SetPortraitSpriteCommandSpecCharR s    => new SetPortraitSpriteCommandCharR(s, _portraitResolver),
            SwayRotateToCommandSpecCharR s => new SwayRotateToCommandCharR(s),
            
            
            _ => null
        };

        return command != null;
    }
}