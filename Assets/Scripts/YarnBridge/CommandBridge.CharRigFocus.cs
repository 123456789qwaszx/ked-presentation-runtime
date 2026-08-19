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
        string durationToken = DefaultFocusPlacementDurationToken)
        => EnqueuePlaceCharacterFocusToSpec(roleKey, focus, ScreenFocusPoint.Left, durationToken);

    private void EnqueueFocusToCenterSpec(
        string roleKey,
        string focus = DefaultFocusPlacementFocusToken,
        string durationToken = DefaultFocusPlacementDurationToken)
        => EnqueuePlaceCharacterFocusToSpec(roleKey, focus, ScreenFocusPoint.Center, durationToken);

    private void EnqueueFocusToRightSpec(
        string roleKey,
        string focus = DefaultFocusPlacementFocusToken,
        string durationToken = DefaultFocusPlacementDurationToken)
        => EnqueuePlaceCharacterFocusToSpec(roleKey, focus, ScreenFocusPoint.Right, durationToken);

    private void EnqueueFocusToTopLeftSpec(
        string roleKey,
        string focus = DefaultFocusPlacementFocusToken,
        string durationToken = DefaultFocusPlacementDurationToken)
        => EnqueuePlaceCharacterFocusToSpec(roleKey, focus, ScreenFocusPoint.TopLeft, durationToken);

    private void EnqueueFocusToTopSpec(
        string roleKey,
        string focus = DefaultFocusPlacementFocusToken,
        string durationToken = DefaultFocusPlacementDurationToken)
        => EnqueuePlaceCharacterFocusToSpec(roleKey, focus, ScreenFocusPoint.Top, durationToken);

    private void EnqueueFocusToTopRightSpec(
        string roleKey,
        string focus = DefaultFocusPlacementFocusToken,
        string durationToken = DefaultFocusPlacementDurationToken)
        => EnqueuePlaceCharacterFocusToSpec(roleKey, focus, ScreenFocusPoint.TopRight, durationToken);

    private void EnqueueFocusToBottomLeftSpec(
        string roleKey,
        string focus = DefaultFocusPlacementFocusToken,
        string durationToken = DefaultFocusPlacementDurationToken)
        => EnqueuePlaceCharacterFocusToSpec(roleKey, focus, ScreenFocusPoint.BottomLeft, durationToken);

    private void EnqueueFocusToBottomSpec(
        string roleKey,
        string focus = DefaultFocusPlacementFocusToken,
        string durationToken = DefaultFocusPlacementDurationToken)
        => EnqueuePlaceCharacterFocusToSpec(roleKey, focus, ScreenFocusPoint.Bottom, durationToken);

    private void EnqueueFocusToBottomRightSpec(
        string roleKey,
        string focus = DefaultFocusPlacementFocusToken,
        string durationToken = DefaultFocusPlacementDurationToken)
        => EnqueuePlaceCharacterFocusToSpec(roleKey, focus, ScreenFocusPoint.BottomRight, durationToken);

    private void EnqueueFocusToInnerTopLeftSpec(
        string roleKey,
        string focus = DefaultFocusPlacementFocusToken,
        string durationToken = DefaultFocusPlacementDurationToken)
        => EnqueuePlaceCharacterFocusToSpec(roleKey, focus, ScreenFocusPoint.ThirdsUpperLeft, durationToken);

    private void EnqueueFocusToInnerTopRightSpec(
        string roleKey,
        string focus = DefaultFocusPlacementFocusToken,
        string durationToken = DefaultFocusPlacementDurationToken)
        => EnqueuePlaceCharacterFocusToSpec(roleKey, focus, ScreenFocusPoint.ThirdsUpperRight, durationToken);

    private void EnqueueFocusToInnerBottomLeftSpec(
        string roleKey,
        string focus = DefaultFocusPlacementFocusToken,
        string durationToken = DefaultFocusPlacementDurationToken)
        => EnqueuePlaceCharacterFocusToSpec(roleKey, focus, ScreenFocusPoint.ThirdsLowerLeft, durationToken);

    private void EnqueueFocusToInnerBottomRightSpec(
        string roleKey,
        string focus = DefaultFocusPlacementFocusToken,
        string durationToken = DefaultFocusPlacementDurationToken)
        => EnqueuePlaceCharacterFocusToSpec(roleKey, focus, ScreenFocusPoint.ThirdsLowerRight, durationToken);

    private void EnqueuePlaceCharacterFocusToSpec(
        string roleKey,
        string focus,
        ScreenFocusPoint screenPoint,
        string durationToken)
        => Collect(new PlaceCharacterFocusCommandSpecCharR
        {
            slotKey = roleKey,
            focusPreset = CharacterFocusPresetParser.Parse(focus),
            screenPoint = screenPoint,
            moveTarget = CharacterRigTarget.CharSlot_Track_Focus,
            duration = YarnDurationParser.Parse(durationToken),
        });

    private void EnqueuePlaceCharacterFocusSpec(
        string roleKey,
        string focus = "bust",
        string screenPoint = "center",
        string durationToken = "0fr")
        => Collect(new PlaceCharacterFocusCommandSpecCharR
        {
            slotKey = roleKey,
            focusPreset = CharacterFocusPresetParser.Parse(focus),
            screenPoint = ScreenFocusPointParser.TryParse(screenPoint, out ScreenFocusPoint parsed)
                ? parsed
                : ScreenFocusPoint.Center,
            moveTarget = CharacterRigTarget.CharSlot_Track_Focus,
            duration = YarnDurationParser.Parse(durationToken),
        });
    #endregion
    
    #region CharFocusDepth
    private void EnqueueDepthAtPresetSpec(
        string roleKey,
        string depthArg,
        string preserveFocusArg = DefaultFocusDepthPresetToken,
        string durationToken = DefaultFocusDepthDurationToken)
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

        Collect(spec);
    }

    private void EnqueueDepthAtCloseSpec(
        string roleKey,
        string preserveFocusArg = DefaultFocusDepthPresetToken,
        string durationToken = DefaultFocusDepthDurationToken)
        => EnqueueDepthAtPresetSpec(roleKey, "close", preserveFocusArg, durationToken);

    private void EnqueueDepthAtFrontSpec(
        string roleKey,
        string preserveFocusArg = DefaultFocusDepthPresetToken,
        string durationToken = DefaultFocusDepthDurationToken)
        => EnqueueDepthAtPresetSpec(roleKey, "front", preserveFocusArg, durationToken);

    private void EnqueueDepthAtMidSpec(
        string roleKey,
        string preserveFocusArg = DefaultFocusDepthPresetToken,
        string durationToken = DefaultFocusDepthDurationToken)
        => EnqueueDepthAtPresetSpec(roleKey, "mid", preserveFocusArg, durationToken);

    private void EnqueueDepthAtBackSpec(
        string roleKey,
        string preserveFocusArg = DefaultFocusDepthPresetToken,
        string durationToken = DefaultFocusDepthDurationToken)
        => EnqueueDepthAtPresetSpec(roleKey, "back", preserveFocusArg, durationToken);

    private void EnqueueDepthAtFarSpec(
        string roleKey,
        string preserveFocusArg = DefaultFocusDepthPresetToken,
        string durationToken = DefaultFocusDepthDurationToken)
        => EnqueueDepthAtPresetSpec(roleKey, "far", preserveFocusArg, durationToken);
    
    private void EnqueueDepthResetSpec(string roleKey, float duration)
        => Collect(new SetDepthCommandSpecCharR
        {
            slotKey = roleKey,
            preset = CharacterDepthKey.Mid,
            useLevel = false,
            duration = duration,
        });
    #endregion
}