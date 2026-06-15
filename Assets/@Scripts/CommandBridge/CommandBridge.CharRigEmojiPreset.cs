using DG.Tweening;
using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    private void BindCharRigEmote(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string>(
            "emoji", EnqueueEmojiPopSpec);
        
        runner.AddCommandHandler<string, string>(
            "emoji_drop", EnqueueEmojiDropSpec);

        runner.AddCommandHandler<string, string>(
            "emoji_shock", EnqueueEmojiShockSpec);

        runner.AddCommandHandler<string, string>(
            "emoji_hop", EnqueueEmojiHopSpec);

        runner.AddCommandHandler<string, string>(
            "emoji_sway", EnqueueEmojiSwaySpec);

        runner.AddCommandHandler<string, string>(
            "emoji_tremble", EnqueueEmojiTrembleSpec);
    }
    
    private void EnqueueEmojiPopSpec(string roleKey, string emojiKey)
    {
        var spec0InitEmojiSpec = new InitCharacterEmojiCommandSpecCharR {
            slotKey = roleKey,
            emojiKey = emojiKey,
            rootTarget = CharacterRigTarget.CharacterEmojiSlot00_Root,
            castTarget = CharacterRigTarget.CharacterEmojiSlot00_CastTransform,
            imageTarget = CharacterRigTarget.EmojiSlot00_Image,
            initialReveal = 0f,
            resetMotionAxes = true
        };

        var spec1PopRevealSpec = new RevealCharacterEmojiCommandSpecCharR {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Image,
            fromReveal = 0f,
            toReveal = 1f,
            duration = 0.8f,
            ease = Ease.OutCubic
        };

        var spec2PopScaleUpSpec = new ScaleToCommandSpecCharR {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Scale,
            toScale = new Vector2(1.18f, 1.18f),
            duration = 0.28f,
            ease = Ease.OutBack,
            wait = true
        };

        var spec3PopScaleBackSpec = new ScaleToCommandSpecCharR {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Scale,
            toScale = Vector2.one,
            duration = 0.52f,
            ease = Ease.OutCubic,
            wait = true
        };

        var spec4HoldSpec = new WaitCommandSpec {
            duration = 0.5f
        };

        var spec5AutoFadeOutSpec = new FadeOutCommandSpecCharR {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterEmojiSlot00_Root,
            duration = 0.4f
        };

        Collect(spec0InitEmojiSpec);
        Collect(spec1PopRevealSpec);
        Collect(spec2PopScaleUpSpec);
        Collect(spec3PopScaleBackSpec);
        Collect(spec4HoldSpec);
        Collect(spec5AutoFadeOutSpec);
    }

    private void EnqueueEmojiDropSpec(string roleKey, string emojiKey)
    {
        var spec0InitEmojiSpec = new InitCharacterEmojiCommandSpecCharR {
            slotKey = roleKey,
            emojiKey = emojiKey,
            rootTarget = CharacterRigTarget.CharacterEmojiSlot00_Root,
            castTarget = CharacterRigTarget.CharacterEmojiSlot00_CastTransform,
            imageTarget = CharacterRigTarget.EmojiSlot00_Image,
            initialReveal = 0f,
            resetMotionAxes = true
        };

        var spec1RevealEmojiSpec = new RevealCharacterEmojiCommandSpecCharR {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Image,
            fromReveal = 0f,
            toReveal = 1f,
            duration = 0.8f,
            ease = Ease.OutCubic,
            wait = false
        };

        var spec2DropSlideInSpec = new SlideInCommandSpecCharR {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Track_Move,
            direction = CharRigDirection.Up,
            distance = 90f,
            duration = 0.8f,
            ease = Ease.OutBack,
            punch = 0f,
            wait = true
        };

        Collect(spec0InitEmojiSpec);
        Collect(spec1RevealEmojiSpec);
        Collect(spec2DropSlideInSpec);
    }

    private void EnqueueEmojiShockSpec(string roleKey, string emojiKey)
    {
        var spec0InitEmojiSpec = new InitCharacterEmojiCommandSpecCharR {
            slotKey = roleKey,
            emojiKey = emojiKey,
            rootTarget = CharacterRigTarget.CharacterEmojiSlot00_Root,
            castTarget = CharacterRigTarget.CharacterEmojiSlot00_CastTransform,
            imageTarget = CharacterRigTarget.EmojiSlot00_Image,
            initialReveal = 0f,
            resetMotionAxes = true
        };

        var spec1RevealEmojiSpec = new RevealCharacterEmojiCommandSpecCharR {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Image,
            fromReveal = 0f,
            toReveal = 1f,
            duration = 0.7f,
            ease = Ease.OutCubic,
            wait = false
        };

        var spec2ShockJoltSpec = new JoltCommandSpec {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Track_Move,
            strength = 20f,
            direction = CharRigDirection.Right,
            duration = 0.75f,
            taps = 2,
            damping = 6f,
            anticipation = 0f,
            wait = false
        };

        var spec3ShockScaleUpSpec = new ScaleToCommandSpecCharR {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Scale,
            toScale = new Vector2(1.28f, 1.28f),
            duration = 0.24f,
            ease = Ease.OutBack,
            wait = true
        };

        var spec4ShockScaleBackSpec = new ScaleToCommandSpecCharR {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Scale,
            toScale = Vector2.one,
            duration = 0.46f,
            ease = Ease.OutCubic,
            wait = true
        };

        Collect(spec0InitEmojiSpec);
        Collect(spec1RevealEmojiSpec);
        Collect(spec2ShockJoltSpec);
        Collect(spec3ShockScaleUpSpec);
        Collect(spec4ShockScaleBackSpec);
    }

    private void EnqueueEmojiHopSpec(string roleKey, string emojiKey)
    {
        var spec0InitEmojiSpec = new InitCharacterEmojiCommandSpecCharR {
            slotKey = roleKey,
            emojiKey = emojiKey,
            rootTarget = CharacterRigTarget.CharacterEmojiSlot00_Root,
            castTarget = CharacterRigTarget.CharacterEmojiSlot00_CastTransform,
            imageTarget = CharacterRigTarget.EmojiSlot00_Image,
            initialReveal = 0f,
            resetMotionAxes = true
        };

        var spec1RevealEmojiSpec = new RevealCharacterEmojiCommandSpecCharR {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Image,
            fromReveal = 0f,
            toReveal = 1f,
            duration = 0.8f,
            ease = Ease.OutCubic,
            wait = false
        };

        var spec2HopSpec = new HopCommandSpecCharR {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Track_Y,
            duration = 0.8f,
            ease = Ease.OutCubic,
            hopCount = 1,
            height = 54f,
            airWidth = 0.85f,
            lastArcHeight = -1f,
            lastAirWidth = -1f,
            wait = true
        };

        Collect(spec0InitEmojiSpec);
        Collect(spec1RevealEmojiSpec);
        Collect(spec2HopSpec);
    }

    private void EnqueueEmojiSwaySpec(string roleKey, string emojiKey)
    {
        var spec0InitEmojiSpec = new InitCharacterEmojiCommandSpecCharR {
            slotKey = roleKey,
            emojiKey = emojiKey,
            rootTarget = CharacterRigTarget.CharacterEmojiSlot00_Root,
            castTarget = CharacterRigTarget.CharacterEmojiSlot00_CastTransform,
            imageTarget = CharacterRigTarget.EmojiSlot00_Image,
            initialReveal = 0f,
            resetMotionAxes = true
        };

        var spec1RevealEmojiSpec = new RevealCharacterEmojiCommandSpecCharR {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Image,
            fromReveal = 0f,
            toReveal = 1f,
            duration = 0.8f,
            ease = Ease.OutCubic,
            wait = false
        };

        var spec2SwaySpec = new SwayCommandSpecCharR {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Rotation,
            strength = 9f,
            duration = 0.8f,
            cycles = 1,
            damping = 2.1f,
            speed = 1.0f,
            finalOvershoot = 0.2f,
            anticipation = 0f,
            startPositive = true,
            wait = true
        };

        Collect(spec0InitEmojiSpec);
        Collect(spec1RevealEmojiSpec);
        Collect(spec2SwaySpec);
    }

    private void EnqueueEmojiTrembleSpec(string roleKey, string emojiKey)
    {
        var spec0InitEmojiSpec = new InitCharacterEmojiCommandSpecCharR {
            slotKey = roleKey,
            emojiKey = emojiKey,
            rootTarget = CharacterRigTarget.CharacterEmojiSlot00_Root,
            castTarget = CharacterRigTarget.CharacterEmojiSlot00_CastTransform,
            imageTarget = CharacterRigTarget.EmojiSlot00_Image,
            initialReveal = 0f,
            resetMotionAxes = true
        };

        var spec1RevealEmojiSpec = new RevealCharacterEmojiCommandSpecCharR {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Image,
            fromReveal = 0f,
            toReveal = 1f,
            duration = 0.75f,
            ease = Ease.OutCubic,
            wait = false
        };

        var spec2TrembleSpec = new TrembleCommandSpecCharR {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Track_Move,
            strength = 6f,
            direction = CharRigDirection.Right,
            duration = 0.8f,
            frequency = 20f,
            crossAxisRatio = 0.35f,
            noiseRatio = 0.2f,
            usePulse = false,
            pulseInterval = 1.0f,
            pulseDuration = 0.16f,
            blendIn = 0.04f,
            blendOut = 0.08f,
            wait = true
        };

        Collect(spec0InitEmojiSpec);
        Collect(spec1RevealEmojiSpec);
        Collect(spec2TrembleSpec);
    }
}