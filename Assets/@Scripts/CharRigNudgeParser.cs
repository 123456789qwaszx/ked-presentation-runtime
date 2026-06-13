using System.Globalization;
using UnityEngine;

public enum CharRigNudgeTargetSpace
{
    SlotTrack = 0,
    PortraitTrack = 10
}

public readonly struct CharRigNudgeResult
{
    public readonly bool IsValid;
    public readonly CharacterRigTarget Target;
    public readonly Vector2 Delta;

    public CharRigNudgeResult(
        CharacterRigTarget target,
        Vector2 delta,
        bool isValid = true)
    {
        Target = target;
        Delta = delta;
        IsValid = isValid;
    }

    public static CharRigNudgeResult Invalid =>
        new(CharacterRigTarget.CharSlot_Track_X, Vector2.zero, false);
}

public static class CharRigNudgeParser
{
    // Authoring movement unit.
    // 1 unit = 1/48 of the stage width.
    // On a 1920px reference stage, this becomes 40px.
    // This keeps authored movement consistent across resolutions.
    private const float UnitPixels = 40f;

    public static CharRigNudgeResult Parse(
        string token,
        CharRigNudgeTargetSpace targetSpace)
    {
        if (string.IsNullOrWhiteSpace(token))
            return CharRigNudgeResult.Invalid;

        token = token.Trim().ToLowerInvariant();

        char direction = token[0];
        string amountText = token.Length > 1 ? token.Substring(1) : "1";

        if (!float.TryParse(
                amountText,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float amount))
        {
            amount = 1f;
        }

        float pixels = amount * UnitPixels;

        return direction switch
        {
            'l' => new CharRigNudgeResult(
                ResolveHorizontalTarget(targetSpace),
                new Vector2(-pixels, 0f)),

            'r' => new CharRigNudgeResult(
                ResolveHorizontalTarget(targetSpace),
                new Vector2(pixels, 0f)),

            'u' => new CharRigNudgeResult(
                ResolveVerticalTarget(targetSpace),
                new Vector2(0f, pixels)),

            'd' => new CharRigNudgeResult(
                ResolveVerticalTarget(targetSpace),
                new Vector2(0f, -pixels)),

            _ => CharRigNudgeResult.Invalid
        };
    }

    private static CharacterRigTarget ResolveHorizontalTarget(
        CharRigNudgeTargetSpace targetSpace)
    {
        return targetSpace switch
        {
            CharRigNudgeTargetSpace.SlotTrack =>
                CharacterRigTarget.CharSlot_Track_X,

            CharRigNudgeTargetSpace.PortraitTrack =>
                CharacterRigTarget.CharacterPortrait_Track_X,

            _ => CharacterRigTarget.CharSlot_Track_X
        };
    }

    private static CharacterRigTarget ResolveVerticalTarget(
        CharRigNudgeTargetSpace targetSpace)
    {
        return targetSpace switch
        {
            CharRigNudgeTargetSpace.SlotTrack =>
                CharacterRigTarget.CharSlot_Track_Y,

            CharRigNudgeTargetSpace.PortraitTrack =>
                CharacterRigTarget.CharacterPortrait_Track_Y,

            _ => CharacterRigTarget.CharSlot_Track_Y
        };
    }
}