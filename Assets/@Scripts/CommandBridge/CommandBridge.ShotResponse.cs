using DG.Tweening;
using UnityEngine;

public sealed partial class YarnCommandBridge
{
    private void EnqueueRegisterBackgroundResponseBindingSpec(string rigKey, string stageKey = "0")
    {
        var spec = new RegisterBackgroundResponseBindingCommandSpec
        {
            rigKey = rigKey,
            responseProfile = PresentationResponseProfile.Background
        };

        Collect(spec);
    }

    private void EnqueueRegisterCharacterResponseBindingSpec(string targetKey, string stageKey = "0")
    {
        var spec = new RegisterCharacterResponseBindingCommandSpec
        {
            targetKey = targetKey,
            responseProfile = PresentationResponseProfile.CharacterSlot
        };

        Collect(spec);
    }
    
    private void EnqueueShotZoomFocusSpec(string roleKey, string anchorName, string screenPointName, float zoom, float duration)
    {
        CharacterFocusAnchorParser.TryParse(anchorName, out CharacterFocusAnchor anchor);
        ScreenFocusPointParser.TryParse(screenPointName, out ScreenFocusPoint point);

        var spec = new ShotZoomFocusCommandSpec
        {
            focusRoleKey = roleKey,
            focusAnchor = anchor,
            screenPoint = point,
            zoom = zoom,
            duration = duration
        };
        
        Collect(spec);
    }
    
    private void EnqueueShotResetSpec(float duration = 0.35f)
    {
        var spec = new ShotResetCommandSpec
        {
            duration = Mathf.Max(0f, duration),
            ease = Ease.OutCubic,
            wait = false
        };
        
        Collect(spec);
    }
    
    private void EnqueueShotToSpec(float x, float y, float zoom, float duration = 0.45f)
    {
        var spec = new ShotToCommandSpec
        {
            pan = new Vector2(x, y),
            zoom = zoom,
            duration = duration
        };
        
        Collect(spec);
    }

    private void EnqueueShotZoomSpec(float zoom, float duration = 0.45f)
    {
        var spec = new ShotZoomCommandSpec
        {
            zoom = Mathf.Clamp(zoom, -10f, 10f),
            duration = duration,
            ease = Ease.OutCubic,
            wait = false,
            killTween = true
        };
        
        Collect(spec);
    }

    private void EnqueueShotTrackSpec(float x, float y, float duration = 0.35f)
    {
        var spec = new ShotTrackCommandSpec
        {
            pan = new Vector2(x, y),
            relative = true,
            duration = duration,
            ease = Ease.OutCubic,
            wait = false,
            killTween = true
        };
        
        Collect(spec);
    }
}