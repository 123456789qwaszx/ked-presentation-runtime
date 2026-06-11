using DG.Tweening;
using UnityEngine;

public sealed partial class YarnCommandBridge
{
    private void EnqueueRegisterBackgroundResponseBindingSpec(string rigKey)
    {
        var spec = new RegisterBackgroundResponseBindingCommandSpec
        {
            rigKey = rigKey,
            responseProfile = PresentationResponseProfile.Background
        };

        Collect(spec);
    }

    private void EnqueueRegisterCharacterResponseBindingSpec(string targetKey)
    {
        var spec = new RegisterCharacterResponseBindingCommandSpec
        {
            targetKey = targetKey,
            responseProfile = PresentationResponseProfile.CharacterSlot
        };

        Collect(spec);
    }
    
    private void EnqueueRemoveBackgroundResponseBindingSpec(string rigKey, string stageKey = "0")
    {
        var spec = new RemoveBackgroundResponseBindingCommandSpec
        {
            rigKey = rigKey
        };

        Collect(spec);
    }

    private void EnqueueRemoveCharacterResponseBindingSpec(string targetKey, string stageKey = "0")
    {
        var spec = new RemoveCharacterResponseBindingCommandSpec
        {
            targetKey = targetKey
        };

        Collect(spec);
    }
    
    private void EnqueueShotZoomFocusSpec(
        string roleKey,
        string focusName = "body",
        string screenPointName = "center",
        float zoom = 2.5f,
        float duration = 1.2f)
    {
        CharacterFocusPreset focusPreset;
        string customFocusKey = "";

        if (CharacterFocusPresetParser.TryParse(focusName, out CharacterFocusPreset parsedPreset))
        {
            focusPreset = parsedPreset;
        }
        else
        {
            focusPreset = CharacterFocusPreset.Custom;
            customFocusKey = focusName;
        }

        if (!ScreenFocusPointParser.TryParse(screenPointName, out ScreenFocusPoint screenPoint))
            screenPoint = ScreenFocusPoint.Center;

        var spec = new ShotZoomFocusCommandSpec
        {
            focusRoleKey = roleKey,
            focusPreset = focusPreset,
            customFocusKey = customFocusKey,
            screenPoint = screenPoint,
            zoom = Mathf.Clamp(zoom, -10f, 10f),
            duration = duration,
            ease = Ease.OutCubic,
            wait = false
        };

        Collect(spec);
    }
    
    private void EnqueueShotResetSpec(float duration = 0.3f)
    {
        var spec = new ShotResetCommandSpec
        {
            duration = duration,
            ease = Ease.OutCubic,
            wait = false
        };
        
        Collect(spec);
    }
    
    private void EnqueueShotToSpec(float zoom = 1f, float x = 100f, float y = 0f, float duration = 0.45f)
    {
        var spec = new ShotToCommandSpec
        {
            zoom = Mathf.Clamp(zoom, -10f, 10f),
            pan = new Vector2(x, y),
            duration = duration,
            ease = Ease.OutCubic,
            wait = false
        };
        
        Collect(spec);
    }

    private void EnqueueShotZoomSpec(float zoom = 1f, float duration = 0.45f)
    {
        var spec = new ShotZoomCommandSpec
        {
            zoom = Mathf.Clamp(zoom, -10f, 10f),
            duration = duration,
            ease = Ease.OutCubic,
            wait = false
        };
        
        Collect(spec);
    }

    private void EnqueueShotTrackSpec(float x = 100f, float y = 0f, float duration = 0.35f)
    {
        var spec = new ShotTrackCommandSpec
        {
            pan = new Vector2(x, y),
            duration = duration,
            ease = Ease.OutCubic,
            wait = false
        };
        
        Collect(spec);
    }
}