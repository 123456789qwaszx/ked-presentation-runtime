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

    private void EnqueueSetEmotionPortraitWipeDslSpec(
        string targetKey,
        string emotion,
        string durationToken = "10fr")
        => Collect(new SetEmotionPortraitWipeCommandSpec
        {
            slotKey = targetKey,
            portrait = new PortraitIdentity { emotion = emotion },
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

    private void EnqueueSlideInDslSpec(
        string roleKey,
        string direction = "left",
        string distanceToken = "12u",
        string durationToken = "10fr")
        => Collect(new SlideInCommandSpecCharR
        {
            slotKey = roleKey,
            direction = CharRigDirectionParser.ParseSlideDirection(direction),
            distance = YarnUnitParser.Parse(distanceToken),
            duration = YarnDurationParser.Parse(durationToken)
        });

    private void EnqueueSlideOutDslSpec(
        string roleKey,
        string direction = "right",
        string distanceToken = "12u",
        string durationToken = "10fr")
        => Collect(new SlideOutCommandSpecCharR
        {
            slotKey = roleKey,
            to = CharRigDirectionParser.ParseSlideDirection(direction),
            distance = YarnUnitParser.Parse(distanceToken),
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

    private void EnqueuePivotRotateToDslSpec(
        string roleKey,
        int angle,
        string durationToken = "10fr")
        => Collect(new PivotRotateToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterPortrait_SwayPivot,
            degree = angle,
            duration = YarnDurationParser.Parse(durationToken)
        });

    private void EnqueueFlipHorizontalDslSpec(
        string roleKey,
        int angle,
        string durationToken = "6fr")
        => Collect(new RotateToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterPortrait_Rotation,
            toEuler = new Vector3(0f, angle, 0f),
            duration = YarnDurationParser.Parse(durationToken)
        });

    private void EnqueueFlipVerticalDslSpec(
        string roleKey,
        int angle,
        string durationToken = "6fr")
        => Collect(new RotateToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterPortrait_Rotation,
            toEuler = new Vector3(angle, 0f, 0f),
            duration = YarnDurationParser.Parse(durationToken)
        });
}