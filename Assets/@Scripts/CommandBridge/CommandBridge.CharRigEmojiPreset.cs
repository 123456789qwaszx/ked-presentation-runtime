using DG.Tweening;
using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    private void BindCharRigEmote(DialogueRunner runner)
    {
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

    private void EnqueueEmojiDropSpec(string roleKey, string emojiKey)
    {
        var spec0_showEmojiRootSpec = new ShowRootLayersCommandSpecCharR
        {
            slotKey = roleKey,
            targetMask = CharRigRootMask.CharacterEmoji_Root
        };

        var spec1_setEmojiImageSpec = new SetCharacterEmojiCommandSpecCharR
        {
            slotKey = roleKey,
            emojiKey = emojiKey,
            imageTarget = CharacterRigTarget.EmojiSlot00_Image,
            preserveAspect = true,
            setNativeSize = false,
            wait = false
        };

        var spec2_setEmojiMaterialSpec = new SetCharacterEmojiMaterialCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Image,
            initialReveal = 0f,
            wait = false
        };

        var spec3_placeEmojiSpec = new PlaceCharacterEmojiCommandSpecCharR
        {
            slotKey = roleKey,
            emojiKey = emojiKey,
            castTarget = CharacterRigTarget.CharacterEmojiSlot00_CastTransform,
            wait = false
        };

        var spec4_resetTrackMoveSpec = new MoveByCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Track_Move,
            useAbsolutePosition = true,
            delta = Vector2.zero,
            duration = 0f,
            ease = Ease.Linear,
            wait = false
        };

        var spec5_resetTrackXSpec = new MoveByCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Track_X,
            useAbsolutePosition = true,
            delta = Vector2.zero,
            duration = 0f,
            ease = Ease.Linear,
            wait = false
        };

        var spec6_resetTrackYSpec = new MoveByCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Track_Y,
            useAbsolutePosition = true,
            delta = Vector2.zero,
            duration = 0f,
            ease = Ease.Linear,
            wait = false
        };

        var spec7_resetScaleSpec = new ScaleToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Scale,
            toScale = Vector2.one,
            duration = 0f,
            ease = Ease.Linear,
            wait = false
        };

        var spec8_resetRotationSpec = new RotateToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Rotation,
            toEuler = Vector3.zero,
            duration = 0f,
            ease = Ease.Linear,
            wait = false
        };

        var spec9_revealEmojiSpec = new RevealCharacterEmojiCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Image,
            fromReveal = 0f,
            toReveal = 1f,
            duration = 0.8f,
            ease = Ease.OutCubic,
            wait = false
        };

        var spec10_dropSlideInSpec = new SlideInCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Track_Move,
            direction = CharRigDirection.Up,
            distance = 90f,
            duration = 0.8f,
            ease = Ease.OutBack,
            punch = 0f,
            wait = true
        };

        Collect(spec0_showEmojiRootSpec);
        Collect(spec1_setEmojiImageSpec);
        Collect(spec2_setEmojiMaterialSpec);
        Collect(spec3_placeEmojiSpec);
        Collect(spec4_resetTrackMoveSpec);
        Collect(spec5_resetTrackXSpec);
        Collect(spec6_resetTrackYSpec);
        Collect(spec7_resetScaleSpec);
        Collect(spec8_resetRotationSpec);
        Collect(spec9_revealEmojiSpec);
        Collect(spec10_dropSlideInSpec);
    }

    private void EnqueueEmojiShockSpec(string roleKey, string emojiKey)
    {
        var spec0_showEmojiRootSpec = new ShowRootLayersCommandSpecCharR
        {
            slotKey = roleKey,
            targetMask = CharRigRootMask.CharacterEmoji_Root
        };

        var spec1_setEmojiImageSpec = new SetCharacterEmojiCommandSpecCharR
        {
            slotKey = roleKey,
            emojiKey = emojiKey,
            imageTarget = CharacterRigTarget.EmojiSlot00_Image,
            preserveAspect = true,
            setNativeSize = false,
            wait = false
        };

        var spec2_setEmojiMaterialSpec = new SetCharacterEmojiMaterialCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Image,
            initialReveal = 0f,
            wait = false
        };

        var spec3_placeEmojiSpec = new PlaceCharacterEmojiCommandSpecCharR
        {
            slotKey = roleKey,
            emojiKey = emojiKey,
            castTarget = CharacterRigTarget.CharacterEmojiSlot00_CastTransform,
            wait = false
        };

        var spec4_resetTrackMoveSpec = new MoveByCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Track_Move,
            useAbsolutePosition = true,
            delta = Vector2.zero,
            duration = 0f,
            ease = Ease.Linear,
            wait = false
        };

        var spec5_resetTrackXSpec = new MoveByCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Track_X,
            useAbsolutePosition = true,
            delta = Vector2.zero,
            duration = 0f,
            ease = Ease.Linear,
            wait = false
        };

        var spec6_resetTrackYSpec = new MoveByCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Track_Y,
            useAbsolutePosition = true,
            delta = Vector2.zero,
            duration = 0f,
            ease = Ease.Linear,
            wait = false
        };

        var spec7_resetScaleSpec = new ScaleToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Scale,
            toScale = Vector2.one,
            duration = 0f,
            ease = Ease.Linear,
            wait = false
        };

        var spec8_resetRotationSpec = new RotateToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Rotation,
            toEuler = Vector3.zero,
            duration = 0f,
            ease = Ease.Linear,
            wait = false
        };

        var spec9_revealEmojiSpec = new RevealCharacterEmojiCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Image,
            fromReveal = 0f,
            toReveal = 1f,
            duration = 0.7f,
            ease = Ease.OutCubic,
            wait = false
        };

        var spec10_shockJoltSpec = new JoltCommandSpec
        {
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

        var spec11_shockScaleUpSpec = new ScaleToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Scale,
            toScale = new Vector2(1.28f, 1.28f),
            duration = 0.24f,
            ease = Ease.OutBack,
            wait = true
        };

        var spec12_shockScaleBackSpec = new ScaleToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Scale,
            toScale = Vector2.one,
            duration = 0.46f,
            ease = Ease.OutCubic,
            wait = true
        };

        Collect(spec0_showEmojiRootSpec);
        Collect(spec1_setEmojiImageSpec);
        Collect(spec2_setEmojiMaterialSpec);
        Collect(spec3_placeEmojiSpec);
        Collect(spec4_resetTrackMoveSpec);
        Collect(spec5_resetTrackXSpec);
        Collect(spec6_resetTrackYSpec);
        Collect(spec7_resetScaleSpec);
        Collect(spec8_resetRotationSpec);
        Collect(spec9_revealEmojiSpec);
        Collect(spec10_shockJoltSpec);
        Collect(spec11_shockScaleUpSpec);
        Collect(spec12_shockScaleBackSpec);
    }

    private void EnqueueEmojiHopSpec(string roleKey, string emojiKey)
    {
        var spec0_showEmojiRootSpec = new ShowRootLayersCommandSpecCharR
        {
            slotKey = roleKey,
            targetMask = CharRigRootMask.CharacterEmoji_Root
        };

        var spec1_setEmojiImageSpec = new SetCharacterEmojiCommandSpecCharR
        {
            slotKey = roleKey,
            emojiKey = emojiKey,
            imageTarget = CharacterRigTarget.EmojiSlot00_Image,
            preserveAspect = true,
            setNativeSize = false,
            wait = false
        };

        var spec2_setEmojiMaterialSpec = new SetCharacterEmojiMaterialCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Image,
            initialReveal = 0f,
            wait = false
        };

        var spec3_placeEmojiSpec = new PlaceCharacterEmojiCommandSpecCharR
        {
            slotKey = roleKey,
            emojiKey = emojiKey,
            castTarget = CharacterRigTarget.CharacterEmojiSlot00_CastTransform,
            wait = false
        };

        var spec4_resetTrackMoveSpec = new MoveByCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Track_Move,
            useAbsolutePosition = true,
            delta = Vector2.zero,
            duration = 0f,
            ease = Ease.Linear,
            wait = false
        };

        var spec5_resetTrackXSpec = new MoveByCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Track_X,
            useAbsolutePosition = true,
            delta = Vector2.zero,
            duration = 0f,
            ease = Ease.Linear,
            wait = false
        };

        var spec6_resetTrackYSpec = new MoveByCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Track_Y,
            useAbsolutePosition = true,
            delta = Vector2.zero,
            duration = 0f,
            ease = Ease.Linear,
            wait = false
        };

        var spec7_resetScaleSpec = new ScaleToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Scale,
            toScale = Vector2.one,
            duration = 0f,
            ease = Ease.Linear,
            wait = false
        };

        var spec8_resetRotationSpec = new RotateToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Rotation,
            toEuler = Vector3.zero,
            duration = 0f,
            ease = Ease.Linear,
            wait = false
        };

        var spec9_revealEmojiSpec = new RevealCharacterEmojiCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Image,
            fromReveal = 0f,
            toReveal = 1f,
            duration = 0.8f,
            ease = Ease.OutCubic,
            wait = false
        };

        var spec10_hopSpec = new HopCommandSpecCharR
        {
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

        Collect(spec0_showEmojiRootSpec);
        Collect(spec1_setEmojiImageSpec);
        Collect(spec2_setEmojiMaterialSpec);
        Collect(spec3_placeEmojiSpec);
        Collect(spec4_resetTrackMoveSpec);
        Collect(spec5_resetTrackXSpec);
        Collect(spec6_resetTrackYSpec);
        Collect(spec7_resetScaleSpec);
        Collect(spec8_resetRotationSpec);
        Collect(spec9_revealEmojiSpec);
        Collect(spec10_hopSpec);
    }

    private void EnqueueEmojiSwaySpec(string roleKey, string emojiKey)
    {
        var spec0_showEmojiRootSpec = new ShowRootLayersCommandSpecCharR
        {
            slotKey = roleKey,
            targetMask = CharRigRootMask.CharacterEmoji_Root
        };

        var spec1_setEmojiImageSpec = new SetCharacterEmojiCommandSpecCharR
        {
            slotKey = roleKey,
            emojiKey = emojiKey,
            imageTarget = CharacterRigTarget.EmojiSlot00_Image,
            preserveAspect = true,
            setNativeSize = false,
            wait = false
        };

        var spec2_setEmojiMaterialSpec = new SetCharacterEmojiMaterialCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Image,
            initialReveal = 0f,
            wait = false
        };

        var spec3_placeEmojiSpec = new PlaceCharacterEmojiCommandSpecCharR
        {
            slotKey = roleKey,
            emojiKey = emojiKey,
            castTarget = CharacterRigTarget.CharacterEmojiSlot00_CastTransform,
            wait = false
        };

        var spec4_resetTrackMoveSpec = new MoveByCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Track_Move,
            useAbsolutePosition = true,
            delta = Vector2.zero,
            duration = 0f,
            ease = Ease.Linear,
            wait = false
        };

        var spec5_resetTrackXSpec = new MoveByCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Track_X,
            useAbsolutePosition = true,
            delta = Vector2.zero,
            duration = 0f,
            ease = Ease.Linear,
            wait = false
        };

        var spec6_resetTrackYSpec = new MoveByCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Track_Y,
            useAbsolutePosition = true,
            delta = Vector2.zero,
            duration = 0f,
            ease = Ease.Linear,
            wait = false
        };

        var spec7_resetScaleSpec = new ScaleToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Scale,
            toScale = Vector2.one,
            duration = 0f,
            ease = Ease.Linear,
            wait = false
        };

        var spec8_resetRotationSpec = new RotateToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Rotation,
            toEuler = Vector3.zero,
            duration = 0f,
            ease = Ease.Linear,
            wait = false
        };

        var spec9_revealEmojiSpec = new RevealCharacterEmojiCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Image,
            fromReveal = 0f,
            toReveal = 1f,
            duration = 0.8f,
            ease = Ease.OutCubic,
            wait = false
        };

        var spec10_swaySpec = new SwayCommandSpecCharR
        {
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

        Collect(spec0_showEmojiRootSpec);
        Collect(spec1_setEmojiImageSpec);
        Collect(spec2_setEmojiMaterialSpec);
        Collect(spec3_placeEmojiSpec);
        Collect(spec4_resetTrackMoveSpec);
        Collect(spec5_resetTrackXSpec);
        Collect(spec6_resetTrackYSpec);
        Collect(spec7_resetScaleSpec);
        Collect(spec8_resetRotationSpec);
        Collect(spec9_revealEmojiSpec);
        Collect(spec10_swaySpec);
    }

    private void EnqueueEmojiTrembleSpec(string roleKey, string emojiKey)
    {
        var spec0_showEmojiRootSpec = new ShowRootLayersCommandSpecCharR
        {
            slotKey = roleKey,
            targetMask = CharRigRootMask.CharacterEmoji_Root
        };

        var spec1_setEmojiImageSpec = new SetCharacterEmojiCommandSpecCharR
        {
            slotKey = roleKey,
            emojiKey = emojiKey,
            imageTarget = CharacterRigTarget.EmojiSlot00_Image,
            preserveAspect = true,
            setNativeSize = false,
            wait = false
        };

        var spec2_setEmojiMaterialSpec = new SetCharacterEmojiMaterialCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Image,
            initialReveal = 0f,
            wait = false
        };

        var spec3_placeEmojiSpec = new PlaceCharacterEmojiCommandSpecCharR
        {
            slotKey = roleKey,
            emojiKey = emojiKey,
            castTarget = CharacterRigTarget.CharacterEmojiSlot00_CastTransform,
            wait = false
        };

        var spec4_resetTrackMoveSpec = new MoveByCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Track_Move,
            useAbsolutePosition = true,
            delta = Vector2.zero,
            duration = 0f,
            ease = Ease.Linear,
            wait = false
        };

        var spec5_resetTrackXSpec = new MoveByCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Track_X,
            useAbsolutePosition = true,
            delta = Vector2.zero,
            duration = 0f,
            ease = Ease.Linear,
            wait = false
        };

        var spec6_resetTrackYSpec = new MoveByCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Track_Y,
            useAbsolutePosition = true,
            delta = Vector2.zero,
            duration = 0f,
            ease = Ease.Linear,
            wait = false
        };

        var spec7_resetScaleSpec = new ScaleToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Scale,
            toScale = Vector2.one,
            duration = 0f,
            ease = Ease.Linear,
            wait = false
        };

        var spec8_resetRotationSpec = new RotateToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Rotation,
            toEuler = Vector3.zero,
            duration = 0f,
            ease = Ease.Linear,
            wait = false
        };

        var spec9_revealEmojiSpec = new RevealCharacterEmojiCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Image,
            fromReveal = 0f,
            toReveal = 1f,
            duration = 0.75f,
            ease = Ease.OutCubic,
            wait = false
        };

        var spec10_trembleSpec = new TrembleCommandSpecCharR
        {
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

        Collect(spec0_showEmojiRootSpec);
        Collect(spec1_setEmojiImageSpec);
        Collect(spec2_setEmojiMaterialSpec);
        Collect(spec3_placeEmojiSpec);
        Collect(spec4_resetTrackMoveSpec);
        Collect(spec5_resetTrackXSpec);
        Collect(spec6_resetTrackYSpec);
        Collect(spec7_resetScaleSpec);
        Collect(spec8_resetRotationSpec);
        Collect(spec9_revealEmojiSpec);
        Collect(spec10_trembleSpec);
    }
}