using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    private void BindCharRigEmoji(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string>(
            "emoji_show", EnqueueEmojiSetSpec);

        runner.AddCommandHandler<string>(
            "emoji_hide", EnqueueEmojiHideSpec);
        
        runner.AddCommandHandler<string, string>(
            "emoji_place", EnqueueEmojiPlaceSpec);

        runner.AddCommandHandler<string, float, string>(
            "emoji_reveal", EnqueueEmojiRevealToSpec);

        runner.AddCommandHandler<string, float, string>(
            "emoji_scale", EnqueueEmojiScaleToSpec);

        runner.AddCommandHandler<string, int, string>(
            "emoji_rotate", EnqueueEmojiRotateToSpec);
    }
    
    private void EnqueueEmojiSetSpec(
        string roleKey,
        string emojiKey)
        => Collect(new InitCharacterEmojiCommandSpecCharR {
            slotKey = roleKey,
            emojiKey = emojiKey,
            rootTarget = CharacterRigTarget.CharacterEmojiSlot00_Root,
            castTarget = CharacterRigTarget.CharacterEmojiSlot00_CastTransform,
            imageTarget = CharacterRigTarget.EmojiSlot00_Image,
            initialReveal = 1f,
            resetMotionAxes = true });
    

    private void EnqueueEmojiHideSpec(
        string roleKey)
        => Collect(new FadeOutCommandSpecCharR {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterEmojiSlot00_Root,
            duration = 0.16f });
    
    private void EnqueueEmojiPlaceSpec(
        string roleKey,
        string emojiKey)
        => Collect(new PlaceCharacterEmojiCommandSpecCharR {
            slotKey = roleKey,
            emojiKey = emojiKey,
            castTarget = CharacterRigTarget.CharacterEmojiSlot00_CastTransform });
    
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
        => Collect(new ScaleToCommandSpecCharR {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Scale,
            toScale = new Vector2(xyScale, xyScale),
            duration = YarnDurationParser.Parse(durationToken), });

    private void EnqueueEmojiRotateToSpec(
        string roleKey,
        int angle,
        string durationToken = "8fr")
        => Collect(new RotateToCommandSpecCharR {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Rotation,
            toEuler = new Vector3(0f, 0f, angle),
            duration = YarnDurationParser.Parse(durationToken) });
}