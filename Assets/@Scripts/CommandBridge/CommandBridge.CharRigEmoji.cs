using DG.Tweening;
using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge : InlineEventMarkupHandler.IInlineEmojiHost
{
    private const float EmojiDefaultRevealDuration = 0.8f;
    private const float EmojiDefaultHoldDuration = 0.5f;
    private const float EmojiDefaultFadeOutDuration = 0.4f;

    public void PlayEmojiCue(string cue)
    {
        string characterKey = _vnRuntimeStateProvider.CurrentCharacterKey;

        if (string.IsNullOrWhiteSpace(characterKey))
            return;

        if (string.IsNullOrWhiteSpace(cue))
        {
            EnqueueEmojiHideSpec(characterKey);
            return;
        }

        EnqueueEmojiPopSpec(characterKey, cue);
    }

    private void BindCharRigEmoji(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string>(
            "emoji", EnqueueEmojiPopSpec);
        
        runner.AddCommandHandler<string, string>(
            "emoji_place", EnqueueEmojiPlaceSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "emoji_place_offset", EnqueueEmojiPlaceOffsetSpec);

        runner.AddCommandHandler<string, float, string>(
            "emoji_reveal", EnqueueEmojiRevealToSpec);

        runner.AddCommandHandler<string, float, string>(
            "emoji_scale", EnqueueEmojiScaleToSpec);

        runner.AddCommandHandler<string, int, string>(
            "emoji_rotate", EnqueueEmojiRotateToSpec);
        
        
        runner.AddCommandHandler<string, string>(
            "emoji_set", EnqueueEmojiSetSpec);
        
        runner.AddCommandHandler<string>(
            "emoji_hide", EnqueueEmojiHideSpec);
    }

    // ---------------------------------------------------------------------
    // Presets
    // ---------------------------------------------------------------------

    // <<emoji igna 19>>
    private void EnqueueEmojiPopSpec(string roleKey, string emojiKey)
    {
        EnqueueEmojiPrepareSpec(
            roleKey,
            emojiKey,
            initialReveal: 0f,
            resetMotionAxes: true);

        Collect(BuildRevealEmojiSpec(
            roleKey,
            fromReveal: 0f,
            toReveal: 1f,
            duration: EmojiDefaultRevealDuration,
            ease: Ease.OutCubic,
            wait: false));

        Collect(BuildEmojiScaleToSpec(
            roleKey,
            xyScale: 1.18f,
            duration: 0.28f,
            ease: Ease.OutBack,
            wait: true));

        Collect(BuildEmojiScaleToSpec(
            roleKey,
            xyScale: 1.0f,
            duration: 0.52f,
            ease: Ease.OutCubic,
            wait: true));

        EnqueueEmojiAutoHideSpec(
            roleKey,
            holdDuration: EmojiDefaultHoldDuration,
            fadeDuration: EmojiDefaultFadeOutDuration);
    }

    // <<emoji_drop igna 19>>
    private void EnqueueEmojiDropSpec(string roleKey, string emojiKey)
    {
        EnqueueEmojiPrepareSpec(
            roleKey,
            emojiKey,
            initialReveal: 0f,
            resetMotionAxes: true);

        Collect(BuildRevealEmojiSpec(
            roleKey,
            fromReveal: 0f,
            toReveal: 1f,
            duration: 0.8f,
            ease: Ease.OutCubic,
            wait: false));

        Collect(BuildEmojiSlideInSpec(
            roleKey,
            direction: CharRigDirection.Up,
            distance: 90f,
            duration: 0.8f,
            ease: Ease.OutBack,
            punch: 0f,
            wait: true));
    }

    // <<emoji_shock igna 19>>
    private void EnqueueEmojiShockSpec(string roleKey, string emojiKey)
    {
        EnqueueEmojiPrepareSpec(
            roleKey,
            emojiKey,
            initialReveal: 0f,
            resetMotionAxes: true);

        Collect(BuildRevealEmojiSpec(
            roleKey,
            fromReveal: 0f,
            toReveal: 1f,
            duration: 0.7f,
            ease: Ease.OutCubic,
            wait: false));

        Collect(BuildEmojiJoltSpec(
            roleKey,
            strength: 20f,
            duration: 0.75f,
            wait: false));

        Collect(BuildEmojiScaleToSpec(
            roleKey,
            xyScale: 1.28f,
            duration: 0.24f,
            ease: Ease.OutBack,
            wait: true));

        Collect(BuildEmojiScaleToSpec(
            roleKey,
            xyScale: 1.0f,
            duration: 0.46f,
            ease: Ease.OutCubic,
            wait: true));
    }

    // <<emoji_hop igna 19>>
    private void EnqueueEmojiHopSpec(string roleKey, string emojiKey)
    {
        EnqueueEmojiPrepareSpec(
            roleKey,
            emojiKey,
            initialReveal: 0f,
            resetMotionAxes: true);

        Collect(BuildRevealEmojiSpec(
            roleKey,
            fromReveal: 0f,
            toReveal: 1f,
            duration: 0.8f,
            ease: Ease.OutCubic,
            wait: false));

        Collect(BuildEmojiHopSpec(
            roleKey,
            height: 54f,
            duration: 0.8f,
            wait: true));
    }

    // <<emoji_sway igna 19>>
    private void EnqueueEmojiSwaySpec(string roleKey, string emojiKey)
    {
        EnqueueEmojiPrepareSpec(
            roleKey,
            emojiKey,
            initialReveal: 0f,
            resetMotionAxes: true);

        Collect(BuildRevealEmojiSpec(
            roleKey,
            fromReveal: 0f,
            toReveal: 1f,
            duration: 0.8f,
            ease: Ease.OutCubic,
            wait: false));

        Collect(BuildEmojiSwaySpec(
            roleKey,
            strength: 9f,
            duration: 0.8f,
            cycles: 1,
            wait: true));
    }

    // <<emoji_tremble igna 19>>
    private void EnqueueEmojiTrembleSpec(string roleKey, string emojiKey)
    {
        EnqueueEmojiPrepareSpec(
            roleKey,
            emojiKey,
            initialReveal: 0f,
            resetMotionAxes: true);

        Collect(BuildRevealEmojiSpec(
            roleKey,
            fromReveal: 0f,
            toReveal: 1f,
            duration: 0.75f,
            ease: Ease.OutCubic,
            wait: false));

        Collect(BuildEmojiTrembleSpec(
            roleKey,
            strength: 6f,
            duration: 0.8f,
            frequency: 20f,
            wait: true));
    }

    // ---------------------------------------------------------------------
    // Low level DSL / composition-ready commands
    // ---------------------------------------------------------------------

    // <<emoji_set igna 19>>
    private void EnqueueEmojiSetSpec(string roleKey, string emojiKey)
    {
        EnqueueEmojiPrepareSpec(
            roleKey,
            emojiKey,
            initialReveal: 1f,
            resetMotionAxes: true);
    }

    // <<emoji_hide igna>>
    private void EnqueueEmojiHideSpec(string roleKey)
    {
        Collect(new FadeOutCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterEmojiSlot00_Root,
            duration = 0.16f,
            ease = Ease.OutCubic,
            wait = false
        });
    }

    // Optional command hook candidate:
    // <<emoji_place igna 19>>
    private void EnqueueEmojiPlaceSpec(string roleKey, string emojiKey)
    {
        Collect(BuildPlaceEmojiSpec(
            roleKey,
            emojiKey));
    }

    // Optional command hook candidate:
    // <<emoji_place_offset igna 19 1u 2u>>
    private void EnqueueEmojiPlaceOffsetSpec(
        string roleKey,
        string emojiKey,
        string xToken,
        string yToken)
    {
        Collect(BuildPlaceEmojiSpec(
            roleKey,
            emojiKey));
    }

    // Optional command hook candidate:
    // <<emoji_reveal igna 1 8fr>>
    private void EnqueueEmojiRevealToSpec(
        string roleKey,
        float toReveal = 1f,
        string durationToken = "8fr")
    {
        Collect(new FadeInCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterEmojiSlot00_Root,
            duration = -1f,
            ease = Ease.OutCubic,
            wait = false
        });
        
        Collect(BuildRevealEmojiSpec(
            roleKey,
            fromReveal: 0f,
            toReveal: Mathf.Clamp01(toReveal),
            duration: YarnDurationParser.Parse(durationToken),
            ease: Ease.OutCubic,
            wait: false));
    }

    // Optional command hook candidate:
    // <<emoji_scale igna 1.15 8fr>>
    private void EnqueueEmojiScaleToSpec(
        string roleKey,
        float xyScale,
        string durationToken = "8fr")
    {
        Collect(BuildEmojiScaleToSpec(
            roleKey,
            xyScale,
            YarnDurationParser.Parse(durationToken),
            Ease.OutCubic,
            wait: false));
    }

    // Optional command hook candidate:
    // <<emoji_rotate igna 12 8fr>>
    private void EnqueueEmojiRotateToSpec(
        string roleKey,
        int angle,
        string durationToken = "8fr")
    {
        Collect(BuildEmojiRotateToSpec(
            roleKey,
            angle,
            YarnDurationParser.Parse(durationToken),
            Ease.OutCubic,
            wait: false));
    }

    // ---------------------------------------------------------------------
    // Shared preset building blocks
    // ---------------------------------------------------------------------

    private void EnqueueEmojiPrepareSpec(
        string roleKey,
        string emojiKey,
        float initialReveal,
        bool resetMotionAxes)
    {
        Collect(new ShowRootLayersCommandSpecCharR
        {
            slotKey = roleKey,
            targetMask = CharRigRootMask.CharacterEmoji_Root
        });

        Collect(BuildSetEmojiImageSpec(roleKey, emojiKey));
        Collect(BuildSetEmojiMaterialSpec(roleKey, initialReveal: 0f));
        Collect(BuildRevealEmojiSpec(roleKey, 0f, 1f, 0.8f, Ease.OutCubic, false));

        Collect(BuildPlaceEmojiSpec(
            roleKey,
            emojiKey));

        if (resetMotionAxes)
            EnqueueEmojiResetMotionAxesSpec(roleKey);
    }

    private void EnqueueEmojiAutoHideSpec(
        string roleKey,
        float holdDuration,
        float fadeDuration)
    {
        if (holdDuration > 0f)
        {
            Collect(new WaitCommandSpec
            {
                duration = holdDuration
            });
        }

        Collect(new FadeOutCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterEmojiSlot00_Root,
            duration = fadeDuration,
            ease = Ease.OutCubic,
            wait = false
        });
    }

    private void EnqueueEmojiResetMotionAxesSpec(string roleKey)
    {
        Collect(BuildEmojiMoveToOriginSpec(roleKey, CharacterRigTarget.EmojiSlot00_Track_Move));
        Collect(BuildEmojiMoveToOriginSpec(roleKey, CharacterRigTarget.EmojiSlot00_Track_X));
        Collect(BuildEmojiMoveToOriginSpec(roleKey, CharacterRigTarget.EmojiSlot00_Track_Y));

        Collect(BuildEmojiScaleToSpec(
            roleKey,
            xyScale: 1f,
            duration: 0f,
            ease: Ease.Linear,
            wait: false));

        Collect(BuildEmojiRotateToSpec(
            roleKey,
            angle: 0f,
            duration: 0f,
            ease: Ease.Linear,
            wait: false));
    }

    private SetCharacterEmojiCommandSpecCharR BuildSetEmojiImageSpec(
        string roleKey,
        string emojiKey)
    {
        return new SetCharacterEmojiCommandSpecCharR
        {
            slotKey = roleKey,
            emojiKey = emojiKey,

            imageTarget = CharacterRigTarget.EmojiSlot00_Image,

            preserveAspect = true,
            setNativeSize = false,

            wait = false
        };
    }
    
    private SetCharacterEmojiMaterialCommandSpecCharR BuildSetEmojiMaterialSpec(
        string roleKey,
        float initialReveal)
    {
        return new SetCharacterEmojiMaterialCommandSpecCharR
        {
            slotKey = roleKey,

            imageTarget = CharacterRigTarget.EmojiSlot00_Image,

            initialReveal = initialReveal,

            wait = false
        };
    }

    private PlaceCharacterEmojiCommandSpecCharR BuildPlaceEmojiSpec(
        string roleKey,
        string emojiKey)
    {
        return new PlaceCharacterEmojiCommandSpecCharR
        {
            slotKey = roleKey,
            emojiKey = emojiKey,

            castTarget = CharacterRigTarget.CharacterEmojiSlot00_CastTransform,
        };
    }

    private RevealCharacterEmojiCommandSpecCharR BuildRevealEmojiSpec(
        string roleKey,
        float fromReveal,
        float toReveal,
        float duration,
        Ease ease,
        bool wait)
    {
        return new RevealCharacterEmojiCommandSpecCharR
        {
            slotKey = roleKey,
            imageTarget = CharacterRigTarget.EmojiSlot00_Image,

            usePresetReveal = false,
            fromReveal = fromReveal,
            toReveal = toReveal,
            duration = duration,
            ease = ease,

            wait = wait
        };
    }

    private MoveByCommandSpecCharR BuildEmojiMoveToOriginSpec(
        string roleKey,
        CharacterRigTarget target)
    {
        return new MoveByCommandSpecCharR
        {
            slotKey = roleKey,
            target = target,
            useAbsolutePosition = true,
            delta = Vector2.zero,
            duration = 0f,
            ease = Ease.Linear,
            wait = false
        };
    }

    private ScaleToCommandSpecCharR BuildEmojiScaleToSpec(
        string roleKey,
        float xyScale,
        float duration,
        Ease ease,
        bool wait)
    {
        return new ScaleToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Scale,

            toScale = new Vector2(xyScale, xyScale),
            duration = duration,
            ease = ease,

            wait = wait,
        };
    }

    private RotateToCommandSpecCharR BuildEmojiRotateToSpec(
        string roleKey,
        float angle,
        float duration,
        Ease ease,
        bool wait)
    {
        return new RotateToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Rotation,

            toEuler = new Vector3(0f, 0f, angle),
            duration = duration,

            wait = wait,
        };
    }

    private SlideInCommandSpecCharR BuildEmojiSlideInSpec(
        string roleKey,
        CharRigDirection direction,
        float distance,
        float duration,
        Ease ease,
        float punch,
        bool wait)
    {
        return new SlideInCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Track_Move,

            direction = direction,
            distance = distance,
            duration = duration,
            ease = ease,
            punch = punch,

            wait = wait,
        };
    }

    private HopCommandSpecCharR BuildEmojiHopSpec(
        string roleKey,
        float height,
        float duration,
        bool wait)
    {
        return new HopCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Track_Y,

            duration = duration,
            ease = Ease.OutCubic,

            hopCount = 1,
            height = height,
            airWidth = 0.85f,
            lastArcHeight = -1f,
            lastAirWidth = -1f,

            wait = wait
        };
    }

    private JoltCommandSpec BuildEmojiJoltSpec(
        string roleKey,
        float strength,
        float duration,
        bool wait)
    {
        return new JoltCommandSpec
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Track_Move,

            strength = strength,
            direction = CharRigDirection.Right,
            duration = duration,

            taps = 2,
            damping = 6f,
            anticipation = 0f,

            wait = wait,
        };
    }

    private SwayCommandSpecCharR BuildEmojiSwaySpec(
        string roleKey,
        float strength,
        float duration,
        int cycles,
        bool wait)
    {
        return new SwayCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Rotation,

            strength = strength,
            duration = duration,
            cycles = cycles,

            damping = 2.1f,
            speed = 1.0f,
            finalOvershoot = 0.2f,

            anticipation = 0f,
            startPositive = true,

            wait = wait
        };
    }

    private TrembleCommandSpecCharR BuildEmojiTrembleSpec(
        string roleKey,
        float strength,
        float duration,
        float frequency,
        bool wait)
    {
        return new TrembleCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Track_Move,

            strength = strength,
            direction = CharRigDirection.Right,
            duration = duration,
            frequency = frequency,

            crossAxisRatio = 0.35f,
            noiseRatio = 0.2f,

            usePulse = false,
            pulseInterval = 1.0f,
            pulseDuration = 0.16f,

            blendIn = 0.04f,
            blendOut = 0.08f,

            wait = wait
        };
    }
}
