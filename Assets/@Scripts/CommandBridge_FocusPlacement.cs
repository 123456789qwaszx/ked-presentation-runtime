using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    private void RegisterFocusPlacementCommands(DialogueRunner runner)
    {
        RegisterFocusPlacementCommand(runner, "to_left", ScreenFocusPoint.Left);
        RegisterFocusPlacementCommand(runner, "to_center", ScreenFocusPoint.Center);
        RegisterFocusPlacementCommand(runner, "to_right", ScreenFocusPoint.Right);

        RegisterFocusPlacementCommand(runner, "to_top_left", ScreenFocusPoint.TopLeft);
        RegisterFocusPlacementCommand(runner, "to_top", ScreenFocusPoint.Top);
        RegisterFocusPlacementCommand(runner, "to_top_right", ScreenFocusPoint.TopRight);
        
        RegisterFocusPlacementCommand(runner, "to_bottom_left", ScreenFocusPoint.BottomLeft);
        RegisterFocusPlacementCommand(runner, "to_bottom", ScreenFocusPoint.Bottom);
        RegisterFocusPlacementCommand(runner, "to_bottom_right", ScreenFocusPoint.BottomRight);

        RegisterFocusPlacementCommand(runner, "to_inner_tl", ScreenFocusPoint.ThirdsUpperLeft);
        RegisterFocusPlacementCommand(runner, "to_inner_tr", ScreenFocusPoint.ThirdsUpperRight);

        RegisterFocusPlacementCommand(runner, "to_inner_bl", ScreenFocusPoint.ThirdsLowerLeft);
        RegisterFocusPlacementCommand(runner, "to_inner_br", ScreenFocusPoint.ThirdsLowerRight);
    }

    private void RegisterFocusPlacementCommand(
        DialogueRunner runner,
        string commandName,
        ScreenFocusPoint screenPoint)
    {
        runner.AddCommandHandler<string, string, string>(
            commandName,
            (roleKey, focus, durationToken) =>
            {
                EnqueuePlaceCharacterFocusToSpec(
                    roleKey,
                    focus,
                    screenPoint,
                    durationToken);
            });
    }

    private void EnqueuePlaceCharacterFocusToSpec(
        string roleKey,
        string focus,
        ScreenFocusPoint screenPoint,
        string durationToken)
    {
        float duration = YarnDurationParser.Parse(durationToken, 0f);

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
        CharacterFocusPreset focusPreset =
            CharacterFocusPresetParser.Parse(
                focus,
                CharacterFocusPreset.Face);

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