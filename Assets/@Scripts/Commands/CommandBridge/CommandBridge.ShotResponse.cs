using UnityEngine;

public sealed partial class YarnCommandBridge
{
    private void EnqueueShotZoomFocusSpec(
        string roleKey,
        string focusName = "body",
        string screenPointName = "center",
        float zoom = 2.5f,
        float duration = 1.2f)
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
            duration = duration,
        };

        Collect(spec);
    }
    
    private void EnqueueShotToSpec(float zoom = 1f, float x = 100f, float y = 0f, float duration = 0.45f)
        => Collect(new ShotToCommandSpec
        {
            zoom = zoom,
            pan = new Vector2(x, y),
            duration = duration,
        });

    private void EnqueueShotZoomSpec(float zoom = 1f, float duration = 0.45f)
        => Collect(new ShotZoomCommandSpec
        {
            zoom = zoom,
            duration = duration,
        });

    private void EnqueueShotTrackSpec(float x = 100f, float y = 0f, float duration = 0.35f)
        => Collect(new ShotTrackCommandSpec
        {
            pan = new Vector2(x, y),
            duration = duration,
        });
    
    private void EnqueueShotResetSpec(float duration = 0.3f)
        => Collect(new ShotResetCommandSpec
        {
            duration = duration,
        });
}