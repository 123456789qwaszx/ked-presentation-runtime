using UnityEngine;

public sealed partial class YarnCommandBridge
{
    private void EnqueueFadeInSpec(string roleKey, string durationToken = "14fr")
        => Collect(new FadeInCommandSpecCharR
        {
            target = CharacterRigTarget.CharacterPortraitSprite_Root,
            slotKey = roleKey,
            duration = YarnDurationParser.Parse(durationToken)
        });

    private void EnqueueFadeOutSpec(string roleKey, string durationToken = "14fr")
        => Collect(new FadeOutCommandSpecCharR
        {
            target = CharacterRigTarget.CharacterPortraitSprite_Root,
            slotKey = roleKey,
            duration = YarnDurationParser.Parse(durationToken)
        });

    private void EnqueueSlideInSpec(
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

    private void EnqueueSlideOutSpec(
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

    private void EnqueueScaleSpec(
        string roleKey,
        float xyValue,
        string durationToken = "10fr",
        string easeToken = "")
    {
        EaseSelection ease = ResolveEase(easeToken);

        Collect(new ScaleToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterPortrait_ActingScale,
            duration = YarnDurationParser.Parse(durationToken),
            toScale = new Vector2(xyValue, xyValue),
            ease = ease.Ease,
            customCurveKeys = ease.CurveKeys
        });
    }
}