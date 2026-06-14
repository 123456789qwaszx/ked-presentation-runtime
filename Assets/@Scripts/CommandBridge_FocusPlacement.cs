using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    private const string DefaultFocusPlacementFocusToken = "face";
    private const string DefaultFocusPlacementDurationToken = "0fr";

    private void RegisterFocusPlacementCommands(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string, string>(
            "to_left", EnqueueFocusToLeftSpec);

        runner.AddCommandHandler<string, string, string>(
            "to_center", EnqueueFocusToCenterSpec);

        runner.AddCommandHandler<string, string, string>(
            "to_right", EnqueueFocusToRightSpec);

        runner.AddCommandHandler<string, string, string>(
            "to_tl", EnqueueFocusToTopLeftSpec);

        runner.AddCommandHandler<string, string, string>(
            "to_top", EnqueueFocusToTopSpec);

        runner.AddCommandHandler<string, string, string>(
            "to_tr", EnqueueFocusToTopRightSpec);

        runner.AddCommandHandler<string, string, string>(
            "to_bl", EnqueueFocusToBottomLeftSpec);

        runner.AddCommandHandler<string, string, string>(
            "to_bottom", EnqueueFocusToBottomSpec);

        runner.AddCommandHandler<string, string, string>(
            "to_br", EnqueueFocusToBottomRightSpec);

        runner.AddCommandHandler<string, string, string>(
            "to_inner_tl", EnqueueFocusToInnerTopLeftSpec);

        runner.AddCommandHandler<string, string, string>(
            "to_inner_tr", EnqueueFocusToInnerTopRightSpec);

        runner.AddCommandHandler<string, string, string>(
            "to_inner_bl", EnqueueFocusToInnerBottomLeftSpec);

        runner.AddCommandHandler<string, string, string>(
            "to_inner_br", EnqueueFocusToInnerBottomRightSpec);
    }

    private void EnqueueFocusToLeftSpec(
        string roleKey,
        string focus = DefaultFocusPlacementFocusToken,
        string durationToken = DefaultFocusPlacementDurationToken)
    {
        EnqueuePlaceCharacterFocusToSpec(
            roleKey,
            focus,
            ScreenFocusPoint.Left,
            durationToken);
    }

    private void EnqueueFocusToCenterSpec(
        string roleKey,
        string focus = DefaultFocusPlacementFocusToken,
        string durationToken = DefaultFocusPlacementDurationToken)
    {
        EnqueuePlaceCharacterFocusToSpec(
            roleKey,
            focus,
            ScreenFocusPoint.Center,
            durationToken);
    }

    private void EnqueueFocusToRightSpec(
        string roleKey,
        string focus = DefaultFocusPlacementFocusToken,
        string durationToken = DefaultFocusPlacementDurationToken)
    {
        EnqueuePlaceCharacterFocusToSpec(
            roleKey,
            focus,
            ScreenFocusPoint.Right,
            durationToken);
    }

    private void EnqueueFocusToTopLeftSpec(
        string roleKey,
        string focus = DefaultFocusPlacementFocusToken,
        string durationToken = DefaultFocusPlacementDurationToken)
    {
        EnqueuePlaceCharacterFocusToSpec(
            roleKey,
            focus,
            ScreenFocusPoint.TopLeft,
            durationToken);
    }

    private void EnqueueFocusToTopSpec(
        string roleKey,
        string focus = DefaultFocusPlacementFocusToken,
        string durationToken = DefaultFocusPlacementDurationToken)
    {
        EnqueuePlaceCharacterFocusToSpec(
            roleKey,
            focus,
            ScreenFocusPoint.Top,
            durationToken);
    }

    private void EnqueueFocusToTopRightSpec(
        string roleKey,
        string focus = DefaultFocusPlacementFocusToken,
        string durationToken = DefaultFocusPlacementDurationToken)
    {
        EnqueuePlaceCharacterFocusToSpec(
            roleKey,
            focus,
            ScreenFocusPoint.TopRight,
            durationToken);
    }

    private void EnqueueFocusToBottomLeftSpec(
        string roleKey,
        string focus = DefaultFocusPlacementFocusToken,
        string durationToken = DefaultFocusPlacementDurationToken)
    {
        EnqueuePlaceCharacterFocusToSpec(
            roleKey,
            focus,
            ScreenFocusPoint.BottomLeft,
            durationToken);
    }

    private void EnqueueFocusToBottomSpec(
        string roleKey,
        string focus = DefaultFocusPlacementFocusToken,
        string durationToken = DefaultFocusPlacementDurationToken)
    {
        EnqueuePlaceCharacterFocusToSpec(
            roleKey,
            focus,
            ScreenFocusPoint.Bottom,
            durationToken);
    }

    private void EnqueueFocusToBottomRightSpec(
        string roleKey,
        string focus = DefaultFocusPlacementFocusToken,
        string durationToken = DefaultFocusPlacementDurationToken)
    {
        EnqueuePlaceCharacterFocusToSpec(
            roleKey,
            focus,
            ScreenFocusPoint.BottomRight,
            durationToken);
    }

    private void EnqueueFocusToInnerTopLeftSpec(
        string roleKey,
        string focus = DefaultFocusPlacementFocusToken,
        string durationToken = DefaultFocusPlacementDurationToken)
    {
        EnqueuePlaceCharacterFocusToSpec(
            roleKey,
            focus,
            ScreenFocusPoint.ThirdsUpperLeft,
            durationToken);
    }

    private void EnqueueFocusToInnerTopRightSpec(
        string roleKey,
        string focus = DefaultFocusPlacementFocusToken,
        string durationToken = DefaultFocusPlacementDurationToken)
    {
        EnqueuePlaceCharacterFocusToSpec(
            roleKey,
            focus,
            ScreenFocusPoint.ThirdsUpperRight,
            durationToken);
    }

    private void EnqueueFocusToInnerBottomLeftSpec(
        string roleKey,
        string focus = DefaultFocusPlacementFocusToken,
        string durationToken = DefaultFocusPlacementDurationToken)
    {
        EnqueuePlaceCharacterFocusToSpec(
            roleKey,
            focus,
            ScreenFocusPoint.ThirdsLowerLeft,
            durationToken);
    }

    private void EnqueueFocusToInnerBottomRightSpec(
        string roleKey,
        string focus = DefaultFocusPlacementFocusToken,
        string durationToken = DefaultFocusPlacementDurationToken)
    {
        EnqueuePlaceCharacterFocusToSpec(
            roleKey,
            focus,
            ScreenFocusPoint.ThirdsLowerRight,
            durationToken);
    }

    private void EnqueuePlaceCharacterFocusToSpec(
        string roleKey,
        string focus,
        ScreenFocusPoint screenPoint,
        string durationToken)
    {
        float duration = YarnDurationParser.Parse(durationToken);

        EnqueuePlaceCharacterFocusToSpec(
            roleKey,
            focus,
            screenPoint,
            duration);
    }

    private void EnqueuePlaceCharacterFocusToSpec(
        string roleKey,
        string focus,
        ScreenFocusPoint screenPoint,
        float duration)
    {
        CharacterFocusPreset focusPreset = CharacterFocusPresetParser.Parse(focus);

        var spec = new PlaceCharacterFocusCommandSpecCharR
        {
            slotKey = roleKey,
            focusPreset = focusPreset,
            screenPoint = screenPoint,
            moveTarget = CharacterRigTarget.CharSlot_Track_Focus,
            duration = duration,
            wait = false
        };

        Collect(spec);
    }
}