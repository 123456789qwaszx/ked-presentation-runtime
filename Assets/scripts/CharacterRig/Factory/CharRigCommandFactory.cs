public class CharRigCommandSpecBase : CommandSpecBase { }

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
            FadeCommandSpecCharR s          => new FadeCommandCharR(s),
            SetSpriteCommandSpecCharR s     => new SetSpriteCommandCharR(s),
            MoveToCommandSpecCharR s        => new MoveToCommandCharR(s),
            SetColorCommandSpecCharR s      => new SetColorCommandCharR(s),
            ShakeCommandSpecCharR s         => new ShakeCommandCharR(s),
            SetPosOffsetCommandSpecCharR s  => new SetPosOffsetCommandCharR(s),
            SetRotationCommandSpecCharR s   => new SetRotationCommandCharR(s),
            SetScaleCommandSpecCharR s      => new SetScaleCommandCharR(s),
            MoveByCommandSpecCharR s        => new MoveByCommandCharR(s),
            ScaleFromToCommandSpecCharR s   => new ScaleFromToCommandCharR(s),
            HideRootsCommandSpecCharR s     => new HideRootsCommandCharR(s),
            SetAnchorCommandSpecCharR s     => new SetAnchorCommandCharR(s),
            ShowRootsCommandSpecCharR s     => new ShowRootsCommandCharR(s),
            DestroyCommandSpec s            => new DestroyCommand(s),
            SetOriginSizeCommandSpecCharR s => new SetOriginSizeCommandCharR(s),
            FadeOutCommandSpecCharR s       => new FadeOutCommandCharR(s),
            FadeInCommandSpecCharR s        => new FadeInCommandCharR(s),
            SlideInCommandSpecCharR s       => new SlideInCommandCharR(s),
            BouncySlideInCommandSpecCharR s => new BouncySlideInCommandCharR(s),
            SlideOutCommandSpecCharR s      => new SlideOutCommandCharR(s),
            SwayCommandSpecCharR s          => new SwayCommandCharR(s),
            PunchScaleCommandSpecCharR s    => new PunchScaleCommandCharR(s),
            RotateFromToCommandSpecCharR s  => new RotateFromToCommandCharR(s),
            
            RichSlideInCommandSpecCharR s    => new RichSlideInCommandCharR(s),
            RichSlideOutCommandSpecCharR s    => new RichSlideOutCommandCharR(s),
            BounceArcInCommandSpecCharR s    => new BounceArcInCommandCharR(s),
            
            BounceArcInLiteCommandSpecCharR s    => new BounceArcInLiteCommandCharR(s),
            DipInOutCommandSpecCharR s    => new DipInOutCommandCharR(s),
            JuicySlideInCommandSpecCharR s    => new JuicySlideInCommandCharR(s),
            JuicySlideOutCommandSpecCharR s    => new JuicySlideOutCommandCharR(s),
            MoveInOutCommandSpecCharR s    => new MoveInOutCommandCharR(s),
            NudgeTapCommandSpecCharR s    => new NudgeTapCommandCharR(s),
            TapEaseCommandSpecCharR s    => new TapEaseCommandCharR(s),
            
            SetEmotionPortraitWipeCommandSpecCharR s    => new SetEmotionPortraitWipeCommandCharR(s, _portraitResolver),
            ShowEmojiCommandSpecCharR s    => new ShowEmojiCommandCharR(s),
            SetPortraitSpriteCommandSpecCharR s    => new SetPortraitSpriteCommandCharR(s, _portraitResolver),
            
            
            _ => null
        };

        return command != null;
    }
}