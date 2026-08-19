using UnityEngine;

public sealed partial class YarnCommandBridge
{
    private void EnqueueShotZoomFocusSpec(
        string roleKey,
        string focusName = "body",
        string screenPointName = "center",
        float zoom = 2.5f,
        string durationToken = "1.2s")
    {
        CharacterFocusPresetParser.TryParse(focusName, out CharacterFocusPreset focusPreset);
        
        if (!ScreenFocusPointParser.TryParse(screenPointName, out ScreenFocusPoint screenPoint))
            screenPoint = ScreenFocusPoint.Center;

        var spec = new ShotZoomFocusCommandSpec
        {
            focusRoleKey = roleKey,
            focusPreset = focusPreset,
            screenPoint = screenPoint,
            zoom = zoom,
            duration = YarnDurationParser.Parse(durationToken),
        };

        Collect(spec);
    }
    
    private void EnqueueShotToSpec(
        float zoom = 1f,
        string xToken = "2.5u",
        string yToken = "0u",
        string durationToken = "0.45s")
        => Collect(new ShotToCommandSpec
        {
            zoom = zoom,
            pan = new Vector2(ParseSignedUnit(xToken, 2.5f), ParseSignedUnit(yToken)),
            duration = YarnDurationParser.Parse(durationToken),
        });

    private void EnqueueShotZoomSpec(float zoom = 1f, string durationToken = "0.45s")
        => Collect(new ShotZoomCommandSpec
        {
            zoom = zoom,
            duration = YarnDurationParser.Parse(durationToken),
        });

    private void EnqueueShotTrackSpec(
        string xToken = "2.5u",
        string yToken = "0u",
        string durationToken = "0.35s")
        => Collect(new ShotTrackCommandSpec
        {
            pan = new Vector2(ParseSignedUnit(xToken, 2.5f), ParseSignedUnit(yToken)),
            duration = YarnDurationParser.Parse(durationToken),
        });
    
    private void EnqueueShotResetSpec(string durationToken = "0.3s")
        => Collect(new ShotResetCommandSpec
        {
            duration = YarnDurationParser.Parse(durationToken),
        });
}