using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    private void BindCharRigAppearance(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string>(
            "fade_in", EnqueueFadeInDslSpec);

        runner.AddCommandHandler<string, string>(
            "fade_out", EnqueueFadeOutDslSpec);

        runner.AddCommandHandler<string, string, string>(
            "face_swap", EnqueueSetEmotionPortraitWipeDslSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "face_crossfade", EnqueueSetPortraitCrossfadeDslSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "slide_in", EnqueueSlideInDslSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "slide_out", EnqueueSlideOutDslSpec);
        
        runner.AddCommandHandler<string, string, string, string>(
            "char_move_to", EnqueueMoveByUnitCharSpec);
        
        runner.AddCommandHandler<string, float, string>(
            "char_scale_to", EnqueueScaleToDslSpec);
        
        runner.AddCommandHandler<string, int, string>(
            "char_rotate_to", EnqueuePivotRotateToDslSpec);
        
        runner.AddCommandHandler<string, int, string>(
            "char_flip_horizontal", EnqueueFlipHorizontalDslSpec);
        
        runner.AddCommandHandler<string, int, string>(
            "char_flip_vertical", EnqueueFlipVerticalDslSpec);
    }

    private void EnqueueFadeInDslSpec(
        string roleKey,
        string durationToken = "14fr")
    {
        float duration = YarnDurationParser.Parse(durationToken);

        var spec = new FadeInCommandSpecCharR
        {
            target = CharacterRigTarget.CharacterPortraitSprite_Root,
            slotKey = roleKey,
            duration = duration
        };

        Collect(spec);
    }

    private void EnqueueFadeOutDslSpec(
        string roleKey,
        string durationToken = "14fr")
    {
        float duration = YarnDurationParser.Parse(durationToken);

        var spec = new FadeOutCommandSpecCharR
        {
            target = CharacterRigTarget.CharacterPortraitSprite_Root,
            slotKey = roleKey,
            duration = duration
        };

        Collect(spec);
    }

    private void EnqueueSetEmotionPortraitWipeDslSpec(
        string targetKey,
        string emotion,
        string durationToken = "10fr")
    {
        float duration = YarnDurationParser.Parse(durationToken);

        var spec = new SetEmotionPortraitWipeCommandSpec
        {
            slotKey = targetKey,
            portrait = new PortraitIdentity
            {
                emotion = emotion,
            },
            duration = duration
        };

        Collect(spec);
    }

    private void EnqueueSetPortraitCrossfadeDslSpec(
        string roleKey,
        string character,
        string emotionKey,
        string durationToken = "10fr")
    {
        float duration = YarnDurationParser.Parse(durationToken);

        var portraitIdentity = new PortraitIdentity
        {
            character = character,
            emotion = emotionKey
        };

        var spec = new SetPortraitCrossfadeCommandSpecCharR
        {
            slotKey = roleKey,
            portrait = portraitIdentity,
            duration = duration
        };

        Collect(spec);
    }

    private void EnqueueSlideInDslSpec(
        string roleKey,
        string direction = "left",
        string distanceToken = "12u",
        string durationToken = "10fr")
    {
        CharRigDirection from = CharRigDirectionParser.ParseSlideDirection(direction);
        float distance = YarnUnitParser.Parse(distanceToken);
        float duration = YarnDurationParser.Parse(durationToken);

        var spec = new SlideInCommandSpecCharR
        {
            slotKey = roleKey,
            direction = from,
            distance = distance,
            duration = duration
        };

        Collect(spec);
    }

    private void EnqueueSlideOutDslSpec(
        string roleKey,
        string direction = "right",
        string distanceToken = "12u",
        string durationToken = "10fr")
    {
        CharRigDirection to = CharRigDirectionParser.ParseSlideDirection(direction);
        float distance = YarnUnitParser.Parse(distanceToken);
        float duration = YarnDurationParser.Parse(durationToken);

        var spec = new SlideOutCommandSpecCharR
        {
            slotKey = roleKey,
            to = to,
            distance = distance,
            duration = duration
        };

        Collect(spec);
    }

    private void EnqueueMoveByUnitCharSpec(
        string roleKey,
        string xToken,
        string yToken,
        string durationToken = "10fr")
    {
        
        float x = YarnUnitParser.Parse(xToken);
        float y = YarnUnitParser.Parse(yToken);
        float duration = YarnDurationParser.Parse(durationToken);

        var spec = new MoveByCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterPortrait_Track,
            delta = new Vector2(x, y),
            duration = duration,
        };

        Collect(spec);
    }

    private void EnqueueScaleToDslSpec(
        string roleKey,
        float xyValue,
        string durationToken = "10fr")
    {
        float duration = YarnDurationParser.Parse(durationToken);

        var spec = new ScaleToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterPortrait_ActingScale,
            duration = duration,
            toScale = new Vector2(xyValue, xyValue)
        };

        Collect(spec);
    }

    private void EnqueuePivotRotateToDslSpec(
        string roleKey,
        int angle,
        string durationToken = "10fr")
    {
        float duration = YarnDurationParser.Parse(durationToken);

        var spec = new PivotRotateToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterPortrait_SwayPivot,
            degree = angle,
            duration = duration
        };

        Collect(spec);
    }

    private void EnqueueFlipHorizontalDslSpec(
        string roleKey,
        int angle,
        string durationToken = "6fr")
    {
        float duration = YarnDurationParser.Parse(durationToken);

        var spec = new RotateToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterPortrait_Rotation,
            toEuler = new Vector3(0f, angle, 0f),
            duration = duration
        };

        Collect(spec);
    }

    private void EnqueueFlipVerticalDslSpec(
        string roleKey,
        int angle,
        string durationToken = "6fr")
    {
        float duration = YarnDurationParser.Parse(durationToken);

        var spec = new RotateToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterPortrait_Rotation,
            toEuler = new Vector3(angle, 0f, 0f),
            duration = duration
        };

        Collect(spec);
    }
}