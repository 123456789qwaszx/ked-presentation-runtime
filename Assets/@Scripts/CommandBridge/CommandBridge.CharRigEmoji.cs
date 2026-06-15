using DG.Tweening;
using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge : InlineEventMarkupHandler.IInlineEmojiHost
{
    #region InlineEventMarkupHandler
    
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
    
    #endregion

    private void BindCharRigEmoji(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string>(
            "emoji", EnqueueEmojiPopSpec);
        
        runner.AddCommandHandler<string>(
            "emoji_material", EnqueueEmojiSetMaterialSpec);

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

    private void EnqueueEmojiSetMaterialSpec(
        string roleKey)
        => Collect(new SetCharacterEmojiMaterialCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Image,
            initialReveal = 0f
        });
    
    private void EnqueueEmojiPlaceSpec(
        string roleKey,
        string emojiKey)
        => Collect(new PlaceCharacterEmojiCommandSpecCharR
        {
            slotKey = roleKey,
            emojiKey = emojiKey,
            castTarget = CharacterRigTarget.CharacterEmojiSlot00_CastTransform
        });

    private void EnqueueEmojiPlaceOffsetSpec(
        string roleKey,
        string emojiKey,
        string xToken,
        string yToken)
        => Collect(new PlaceCharacterEmojiCommandSpecCharR
        {
            slotKey = roleKey,
            emojiKey = emojiKey,
            castTarget = CharacterRigTarget.CharacterEmojiSlot00_CastTransform
        });
    

    private void EnqueueEmojiRevealToSpec(
        string roleKey,
        float toReveal = 1f,
        string durationToken = "8fr")
    {
        FadeInCommandSpecCharR spec0ShowEmojiRootSpec = new() {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterEmojiSlot00_Root,
            duration = 0f,
        };

        RevealCharacterEmojiCommandSpecCharR spec1RevealEmojiSpec = new() {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Image,
            fromReveal = 0f,
            toReveal = Mathf.Clamp01(toReveal),
            duration = YarnDurationParser.Parse(durationToken)
        };

        Collect(spec0ShowEmojiRootSpec);
        Collect(spec1RevealEmojiSpec);
    }

    private void EnqueueEmojiScaleToSpec(
        string roleKey,
        float xyScale,
        string durationToken = "8fr")
        => Collect(new ScaleToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Scale,
            toScale = new Vector2(xyScale, xyScale),
            duration = YarnDurationParser.Parse(durationToken),
        });

    private void EnqueueEmojiRotateToSpec(
        string roleKey,
        int angle,
        string durationToken = "8fr")
        => Collect(new RotateToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Rotation,
            toEuler = new Vector3(0f, 0f, angle),
            duration = YarnDurationParser.Parse(durationToken)
        });
    
    private void EnqueueEmojiPopSpec(string roleKey, string emojiKey)
    {
        var spec0ShowEmojiRootSpec = new ShowRootLayersCommandSpecCharR {
            slotKey = roleKey,
            targetMask = CharRigRootMask.CharacterEmoji_Root
        };

        var spec1SetEmojiImageSpec = new SetCharacterEmojiCommandSpecCharR {
            slotKey = roleKey,
            emojiKey = emojiKey,
            imageTarget = CharacterRigTarget.EmojiSlot00_Image
        };

        var spec2SetEmojiMaterialSpec = new SetCharacterEmojiMaterialCommandSpecCharR {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Image,
            initialReveal = 0f
        };

        var spec3PrepareRevealSpec = new RevealCharacterEmojiCommandSpecCharR {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Image,
            fromReveal = 0f,
            toReveal = 1f,
            duration = 0.8f,
            ease = Ease.OutCubic
        };

        var spec4PlaceEmojiSpec = new PlaceCharacterEmojiCommandSpecCharR {
            slotKey = roleKey,
            emojiKey = emojiKey,
            castTarget = CharacterRigTarget.CharacterEmojiSlot00_CastTransform
        };

        var spec5ResetTrackMoveSpec = new MoveByCommandSpecCharR {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Track_Move,
            useAbsolutePosition = true,
            delta = Vector2.zero,
            duration = 0f
        };

        var spec6ResetTrackXSpec = new MoveByCommandSpecCharR {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Track_X,
            useAbsolutePosition = true,
            delta = Vector2.zero,
            duration = 0f
        };

        var spec7ResetTrackYSpec = new MoveByCommandSpecCharR {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Track_Y,
            useAbsolutePosition = true,
            delta = Vector2.zero,
            duration = 0f
        };

        var spec8ResetScaleSpec = new ScaleToCommandSpecCharR {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Scale,
            toScale = Vector2.one,
            duration = 0f
        };

        var spec9ResetRotationSpec = new RotateToCommandSpecCharR {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Rotation,
            toEuler = Vector3.zero,
            duration = 0f
        };

        var spec10PopRevealSpec = new RevealCharacterEmojiCommandSpecCharR {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Image,
            fromReveal = 0f,
            toReveal = 1f,
            duration = 0.8f,
            ease = Ease.OutCubic
        };

        var spec11PopScaleUpSpec = new ScaleToCommandSpecCharR {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Scale,
            toScale = new Vector2(1.18f, 1.18f),
            duration = 0.28f,
            ease = Ease.OutBack,
            wait = true
        };

        var spec12PopScaleBackSpec = new ScaleToCommandSpecCharR {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Scale,
            toScale = Vector2.one,
            duration = 0.52f,
            ease = Ease.OutCubic,
            wait = true
        };

        var spec13HoldSpec = new WaitCommandSpec {
            duration = 0.5f
        };

        var spec14AutoFadeOutSpec = new FadeOutCommandSpecCharR {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterEmojiSlot00_Root,
            duration = 0.4f
        };

        Collect(spec0ShowEmojiRootSpec);
        Collect(spec1SetEmojiImageSpec);
        Collect(spec2SetEmojiMaterialSpec);
        Collect(spec3PrepareRevealSpec);
        Collect(spec4PlaceEmojiSpec);
        Collect(spec5ResetTrackMoveSpec);
        Collect(spec6ResetTrackXSpec);
        Collect(spec7ResetTrackYSpec);
        Collect(spec8ResetScaleSpec);
        Collect(spec9ResetRotationSpec);
        Collect(spec10PopRevealSpec);
        Collect(spec11PopScaleUpSpec);
        Collect(spec12PopScaleBackSpec);
        Collect(spec13HoldSpec);
        Collect(spec14AutoFadeOutSpec);
    }

    private void EnqueueEmojiSetSpec(string roleKey, string emojiKey)
    {
        var spec0ShowEmojiRootSpec = new ShowRootLayersCommandSpecCharR {
            slotKey = roleKey,
            targetMask = CharRigRootMask.CharacterEmoji_Root
        };

        var spec1SetEmojiImageSpec = new SetCharacterEmojiCommandSpecCharR {
            slotKey = roleKey,
            emojiKey = emojiKey,
            imageTarget = CharacterRigTarget.EmojiSlot00_Image
        };

        var spec2SetEmojiMaterialSpec = new SetCharacterEmojiMaterialCommandSpecCharR {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Image,
            initialReveal = 0f
        };

        var spec3RevealEmojiSpec = new RevealCharacterEmojiCommandSpecCharR {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Image,
            fromReveal = 0f,
            toReveal = 1f,
            duration = 0.8f
        };

        var spec4PlaceEmojiSpec = new PlaceCharacterEmojiCommandSpecCharR {
            slotKey = roleKey,
            emojiKey = emojiKey,
            castTarget = CharacterRigTarget.CharacterEmojiSlot00_CastTransform,
        };

        var spec5ResetTrackMoveSpec = new MoveByCommandSpecCharR {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Track_Move,
            useAbsolutePosition = true,
            delta = Vector2.zero,
            duration = 0f
        };

        var spec6ResetTrackXSpec = new MoveByCommandSpecCharR {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Track_X,
            useAbsolutePosition = true,
            delta = Vector2.zero,
            duration = 0f
        };

        var spec7ResetTrackYSpec = new MoveByCommandSpecCharR {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Track_Y,
            useAbsolutePosition = true,
            delta = Vector2.zero,
            duration = 0f,
        };

        var spec8ResetScaleSpec = new ScaleToCommandSpecCharR {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Scale,
            toScale = Vector2.one,
            duration = 0f,
        };

        var spec9ResetRotationSpec = new RotateToCommandSpecCharR{
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Rotation,
            toEuler = Vector3.zero,
            duration = 0f,
        };

        Collect(spec0ShowEmojiRootSpec);
        Collect(spec1SetEmojiImageSpec);
        Collect(spec2SetEmojiMaterialSpec);
        Collect(spec3RevealEmojiSpec);
        Collect(spec4PlaceEmojiSpec);
        Collect(spec5ResetTrackMoveSpec);
        Collect(spec6ResetTrackXSpec);
        Collect(spec7ResetTrackYSpec);
        Collect(spec8ResetScaleSpec);
        Collect(spec9ResetRotationSpec);
    }

    private void EnqueueEmojiHideSpec(
        string roleKey)
        => Collect(new FadeOutCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterEmojiSlot00_Root,
            duration = 0.16f
        });
}