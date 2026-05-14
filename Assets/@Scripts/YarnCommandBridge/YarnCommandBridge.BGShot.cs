using System;
using DG.Tweening;
using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    public void RegisterShotCommands()
    {
        _dialogueRunner.AddCommandHandler<string, string, string, float, float>("shot_zoom_focus", EnqueueShotZoomFocusSpec);
        
        _dialogueRunner.AddCommandHandler("shot_reset", (Action<float>)EnqueueShotResetSpec);
        
        _dialogueRunner.AddCommandHandler("shot_zoom", (Action<string, float, float>)EnqueueShotZoomFocusSpec);
        
        _dialogueRunner.AddCommandHandler("shot_track", (Action<string, float, float, float>)EnqueueShotTrackToSpec);
    }
    
    private void EnqueueShotZoomFocusSpec(
        string roleKey,
        string anchorName,
        string screenPointName,
        float zoom,
        float duration)
    {
        if (string.IsNullOrWhiteSpace(roleKey))
        {
            Debug.LogError("[YarnCommandBridge] shot_zoom_focus: roleKey is null or empty.");
            return;
        }

        if (!CharacterFocusAnchorParser.TryParse(anchorName, out CharacterFocusAnchor anchor))
        {
            Debug.LogError($"[YarnCommandBridge] shot_zoom_focus: Unknown focus anchor '{anchorName}'.");
            return;
        }

        if (!ScreenFocusPointParser.TryParse(screenPointName, out ScreenFocusPoint point))
        {
            Debug.LogError($"[YarnCommandBridge] shot_zoom_focus: Unknown screen focus point '{screenPointName}'.");
            return;
        }

        var spec = new ShotZoomFocusCommandSpec
        {
            focusRoleKey = roleKey.Trim(),
            focusAnchor = anchor,
            fallbackTarget = CharacterRigTarget.CharacterPortrait_Root,
            focusLocalOffset = Vector2.zero,
            screenPoint = point,
            screenOffset = Vector2.zero,
            zoom = zoom,
            duration = duration,
            ease = Ease.OutCubic,
            wait = false,
            killTween = true,
            strict = true
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

    private void EnqueueShotZoomFocusSpec(
        string roleKey,
        float zoom,
        float duration = 0.45f)
    {
        var spec = new ShotZoomCommandSpec
        {
            focusRoleKey = roleKey,
            zoom = Mathf.Clamp(zoom, -10f, 10f),
            duration = Mathf.Max(0f, duration),
        };

        Collect(spec);
    }

    private void EnqueueShotTrackToSpec(string roleKey, float frameX, float frameY, float duration = 0.35f)
    {
        var spec = new ShotTrackCommandSpec
        {
            focusRoleKey = roleKey,
            desiredFramingPoint = new Vector2(frameX, frameY),
            duration = Mathf.Max(0f, duration),
        };

        Collect(spec);
    }
}