using UnityEngine;

public sealed partial class YarnCommandBridge
{
    private const string DefaultFocusPlacementFocusToken = "face";
    private const string DefaultFocusPlacementDurationToken = "0fr";
    
    private const string DefaultFocusDepthPresetToken = "bust";
    private const string DefaultFocusDepthDurationToken = "10fr";
    
    #region CharFocusPlacement
    private void EnqueueFocusToLeftSpec(
        string roleKey,
        string focus = DefaultFocusPlacementFocusToken,
        string durationToken = DefaultFocusPlacementDurationToken,
        string easeToken = "")
        => EnqueuePlaceCharacterFocusToSpec(roleKey, focus, ScreenFocusPoint.Left, durationToken, easeToken);

    private void EnqueueFocusToCenterSpec(
        string roleKey,
        string focus = DefaultFocusPlacementFocusToken,
        string durationToken = DefaultFocusPlacementDurationToken,
        string easeToken = "")
        => EnqueuePlaceCharacterFocusToSpec(roleKey, focus, ScreenFocusPoint.Center, durationToken, easeToken);

    private void EnqueueFocusToRightSpec(
        string roleKey,
        string focus = DefaultFocusPlacementFocusToken,
        string durationToken = DefaultFocusPlacementDurationToken,
        string easeToken = "")
        => EnqueuePlaceCharacterFocusToSpec(roleKey, focus, ScreenFocusPoint.Right, durationToken, easeToken);

    private void EnqueueFocusToTopLeftSpec(
        string roleKey,
        string focus = DefaultFocusPlacementFocusToken,
        string durationToken = DefaultFocusPlacementDurationToken,
        string easeToken = "")
        => EnqueuePlaceCharacterFocusToSpec(roleKey, focus, ScreenFocusPoint.TopLeft, durationToken, easeToken);

    private void EnqueueFocusToTopSpec(
        string roleKey,
        string focus = DefaultFocusPlacementFocusToken,
        string durationToken = DefaultFocusPlacementDurationToken,
        string easeToken = "")
        => EnqueuePlaceCharacterFocusToSpec(roleKey, focus, ScreenFocusPoint.Top, durationToken, easeToken);

    private void EnqueueFocusToTopRightSpec(
        string roleKey,
        string focus = DefaultFocusPlacementFocusToken,
        string durationToken = DefaultFocusPlacementDurationToken,
        string easeToken = "")
        => EnqueuePlaceCharacterFocusToSpec(roleKey, focus, ScreenFocusPoint.TopRight, durationToken, easeToken);

    private void EnqueueFocusToBottomLeftSpec(
        string roleKey,
        string focus = DefaultFocusPlacementFocusToken,
        string durationToken = DefaultFocusPlacementDurationToken,
        string easeToken = "")
        => EnqueuePlaceCharacterFocusToSpec(roleKey, focus, ScreenFocusPoint.BottomLeft, durationToken, easeToken);

    private void EnqueueFocusToBottomSpec(
        string roleKey,
        string focus = DefaultFocusPlacementFocusToken,
        string durationToken = DefaultFocusPlacementDurationToken,
        string easeToken = "")
        => EnqueuePlaceCharacterFocusToSpec(roleKey, focus, ScreenFocusPoint.Bottom, durationToken, easeToken);

    private void EnqueueFocusToBottomRightSpec(
        string roleKey,
        string focus = DefaultFocusPlacementFocusToken,
        string durationToken = DefaultFocusPlacementDurationToken,
        string easeToken = "")
        => EnqueuePlaceCharacterFocusToSpec(roleKey, focus, ScreenFocusPoint.BottomRight, durationToken, easeToken);

    private void EnqueueFocusToInnerTopLeftSpec(
        string roleKey,
        string focus = DefaultFocusPlacementFocusToken,
        string durationToken = DefaultFocusPlacementDurationToken,
        string easeToken = "")
        => EnqueuePlaceCharacterFocusToSpec(roleKey, focus, ScreenFocusPoint.ThirdsUpperLeft, durationToken, easeToken);

    private void EnqueueFocusToInnerTopRightSpec(
        string roleKey,
        string focus = DefaultFocusPlacementFocusToken,
        string durationToken = DefaultFocusPlacementDurationToken,
        string easeToken = "")
        => EnqueuePlaceCharacterFocusToSpec(roleKey, focus, ScreenFocusPoint.ThirdsUpperRight, durationToken, easeToken);

    private void EnqueueFocusToInnerBottomLeftSpec(
        string roleKey,
        string focus = DefaultFocusPlacementFocusToken,
        string durationToken = DefaultFocusPlacementDurationToken,
        string easeToken = "")
        => EnqueuePlaceCharacterFocusToSpec(roleKey, focus, ScreenFocusPoint.ThirdsLowerLeft, durationToken, easeToken);

    private void EnqueueFocusToInnerBottomRightSpec(
        string roleKey,
        string focus = DefaultFocusPlacementFocusToken,
        string durationToken = DefaultFocusPlacementDurationToken,
        string easeToken = "")
        => EnqueuePlaceCharacterFocusToSpec(roleKey, focus, ScreenFocusPoint.ThirdsLowerRight, durationToken, easeToken);

    private void EnqueuePlaceCharacterFocusToSpec(
        string roleKey,
        string focus,
        ScreenFocusPoint screenPoint,
        string durationToken,
        string easeToken = "")
    {
        EaseSelection ease = ResolveEase(easeToken);

        Collect(new PlaceCharacterFocusCommandSpecCharR
        {
            slotKey = roleKey,
            focusPreset = CharacterFocusPresetParser.Parse(focus),
            screenPoint = screenPoint,
            moveTarget = CharacterRigTarget.CharSlot_Track_Focus,
            duration = YarnDurationParser.Parse(durationToken),
            ease = ease.Ease,
            customCurveKeys = ease.CurveKeys
        });
    }

    private void EnqueuePlaceCharacterFocusSpec(
        string roleKey,
        string focus = "bust",
        string screenPoint = "center",
        string durationToken = "0fr",
        string easeToken = "")
    {
        EaseSelection ease = ResolveEase(easeToken);

        Collect(new PlaceCharacterFocusCommandSpecCharR
        {
            slotKey = roleKey,
            focusPreset = CharacterFocusPresetParser.Parse(focus),
            screenPoint = ScreenFocusPointParser.TryParse(screenPoint, out ScreenFocusPoint parsed)
                ? parsed
                : ScreenFocusPoint.Center,
            moveTarget = CharacterRigTarget.CharSlot_Track_Focus,
            duration = YarnDurationParser.Parse(durationToken),
            ease = ease.Ease,
            customCurveKeys = ease.CurveKeys
        });
    }
    #endregion
    
    #region CharFocusDepth
    private void EnqueueDepthAtPresetSpec(
        string roleKey,
        string depthArg,
        string preserveFocusArg = DefaultFocusDepthPresetToken,
        string durationToken = DefaultFocusDepthDurationToken,
        string easeToken = "")
    {
        var spec = new SetDepthCommandSpecCharR
        {
            slotKey = roleKey,
            duration = YarnDurationParser.Parse(durationToken),
        };

        if (CharacterDepthPresetParser.TryParseDepthLevel(depthArg, out float level))
        {
            spec.useLevel = true;
            spec.level = level;
        }
        else
        {
            spec.useLevel = false;

            if (!CharacterDepthPresetParser.TryParse(depthArg, out CharacterDepthKey preset))
            {
                Debug.LogWarning(
                    $"[YarnCommandBridge] Unknown depth preset '{depthArg}'. " +
                    $"Fallback to '{CharacterDepthKey.Mid}'.");

                preset = CharacterDepthKey.Mid;
            }

            spec.preset = preset;
        }

        if (!CharacterFocusPresetParser.TryParse(preserveFocusArg, out CharacterFocusPreset focusPreset))
        {
            Debug.LogWarning(
                $"[YarnCommandBridge] Unknown preserve focus preset '{preserveFocusArg}'. " +
                $"Fallback to '{CharacterFocusPreset.Bust}'.");

            focusPreset = CharacterFocusPreset.Bust;
        }

        spec.focusPreset = focusPreset;

        EaseSelection ease = ResolveEase(easeToken);
        spec.ease = ease.Ease;
        spec.customCurveKeys = ease.CurveKeys;

        Collect(spec);
    }

    private void EnqueueDepthAtCloseSpec(
        string roleKey,
        string preserveFocusArg = DefaultFocusDepthPresetToken,
        string durationToken = DefaultFocusDepthDurationToken,
        string easeToken = "")
        => EnqueueDepthAtPresetSpec(roleKey, "close", preserveFocusArg, durationToken, easeToken);

    private void EnqueueDepthAtFrontSpec(
        string roleKey,
        string preserveFocusArg = DefaultFocusDepthPresetToken,
        string durationToken = DefaultFocusDepthDurationToken,
        string easeToken = "")
        => EnqueueDepthAtPresetSpec(roleKey, "front", preserveFocusArg, durationToken, easeToken);

    private void EnqueueDepthAtMidSpec(
        string roleKey,
        string preserveFocusArg = DefaultFocusDepthPresetToken,
        string durationToken = DefaultFocusDepthDurationToken,
        string easeToken = "")
        => EnqueueDepthAtPresetSpec(roleKey, "mid", preserveFocusArg, durationToken, easeToken);

    private void EnqueueDepthAtBackSpec(
        string roleKey,
        string preserveFocusArg = DefaultFocusDepthPresetToken,
        string durationToken = DefaultFocusDepthDurationToken,
        string easeToken = "")
        => EnqueueDepthAtPresetSpec(roleKey, "back", preserveFocusArg, durationToken, easeToken);

    private void EnqueueDepthAtFarSpec(
        string roleKey,
        string preserveFocusArg = DefaultFocusDepthPresetToken,
        string durationToken = DefaultFocusDepthDurationToken,
        string easeToken = "")
        => EnqueueDepthAtPresetSpec(roleKey, "far", preserveFocusArg, durationToken, easeToken);
    
    private void EnqueueDepthResetSpec(string roleKey, float duration, string easeToken = "")
    {
        EaseSelection ease = ResolveEase(easeToken);

        Collect(new SetDepthCommandSpecCharR
        {
            slotKey = roleKey,
            preset = CharacterDepthKey.Mid,
            useLevel = false,
            duration = duration,
            ease = ease.Ease,
            customCurveKeys = ease.CurveKeys
        });
    }
    #endregion
}
