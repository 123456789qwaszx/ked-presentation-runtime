using UnityEngine;

public sealed partial class YarnCommandBridge
{
    private void EnqueueFadeInDslSpec(string roleKey, string durationToken = "14fr")
        => Collect(new FadeInCommandSpecCharR
        {
            target = CharacterRigTarget.CharacterPortraitSprite_Root,
            slotKey = roleKey,
            duration = YarnDurationParser.Parse(durationToken)
        });

    private void EnqueueFadeOutDslSpec(string roleKey, string durationToken = "14fr")
        => Collect(new FadeOutCommandSpecCharR
        {
            target = CharacterRigTarget.CharacterPortraitSprite_Root,
            slotKey = roleKey,
            duration = YarnDurationParser.Parse(durationToken)
        });

    private void EnqueueSetPortraitCrossfadeDslSpec(
        string roleKey,
        string character,
        string emotionKey,
        string durationToken = "10fr")
        => Collect(new SetPortraitCrossfadeCommandSpecCharR
        {
            slotKey = roleKey,
            portrait = new PortraitIdentity { character = character, emotion = emotionKey },
            duration = YarnDurationParser.Parse(durationToken)
        });

    private void EnqueueMoveByUnitCharSpec(
        string roleKey,
        string xToken,
        string yToken,
        string durationToken = "10fr")
        => Collect(new MoveByCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterPortrait_Track,
            delta = new Vector2(YarnUnitParser.Parse(xToken), YarnUnitParser.Parse(yToken)),
            duration = YarnDurationParser.Parse(durationToken),
        });

    private void EnqueueScaleToDslSpec(
        string roleKey,
        float xyValue,
        string durationToken = "10fr")
        => Collect(new ScaleToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterPortrait_ActingScale,
            duration = YarnDurationParser.Parse(durationToken),
            toScale = new Vector2(xyValue, xyValue)
        });
}