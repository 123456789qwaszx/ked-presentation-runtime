using System;
using DG.Tweening;
using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    public void RegisterShotCommands()
    {
        _dialogueRunner.AddCommandHandler("shot_reset", (Action<float>)EnqueueShotResetSpec);
        
        _dialogueRunner.AddCommandHandler<string, string, string, float, float>("shot_zoom_focus", EnqueueShotZoomFocusSpec);
        
        _dialogueRunner.AddCommandHandler<float, float, float, float>("shot_to", EnqueueShotToSpec);
        
        _dialogueRunner.AddCommandHandler<float, float>("shot_zoom", EnqueueShotZoomSpec);
        _dialogueRunner.AddCommandHandler<float, float, float>("shot_track", EnqueueShotTrackSpec);
        _dialogueRunner.AddCommandHandler<float, float, float>("shot_track_to", EnqueueShotTrackToSpec);
        
        _dialogueRunner.AddCommandHandler<string, float, float>(
            "focus_offset",
            EnqueueSetCharRigCamFocusSpec);

        _dialogueRunner.AddCommandHandler<string, float, float>(
            "focus_offset_by",
            EnqueueAddCharRigCamFocusSpec);
    }
    
    private void EnqueueShotToSpec(float x, float y, float zoom, float duration = 0.45f)
    {
        var spec = new ShotToCommandSpec
        {
            pan = new Vector2(x, y),
            zoom = Mathf.Clamp(zoom, -10f, 10f),
            duration = duration,
            ease = Ease.OutCubic,
            wait = false,
            killTween = true
        };

        Collect(spec);
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
            fallbackTarget = CharacterRigTarget.CharSlot_FramingTransform,
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

    private void EnqueueShotTrackToSpec(float x, float y, float duration = 0.35f)
    {
        var spec = new ShotTrackCommandSpec
        {
            pan = new Vector2(x, y),
            relative = false,
            duration = duration,
            ease = Ease.OutCubic,
            wait = false,
            killTween = true
        };

        Collect(spec);
    }
    
    private void EnqueueSetCharRigCamFocusSpec(string roleKey, float x, float y)
    {
        if (string.IsNullOrWhiteSpace(roleKey))
        {
            Debug.LogError("[YarnCommandBridge] cam_focus: roleKey is null or empty.");
            return;
        }

        var spec = new SetCharRigCamFocusCommandSpec
        {
            slotKey = roleKey.Trim(),
            mode = CharRigCamFocusMoveMode.Set,
            position = new Vector2(x, y),
        };

        Collect(spec);
    }

    private void EnqueueAddCharRigCamFocusSpec(string roleKey, float x, float y)
    {
        if (string.IsNullOrWhiteSpace(roleKey))
        {
            Debug.LogError("[YarnCommandBridge] cam_focus_add: roleKey is null or empty.");
            return;
        }

        var spec = new SetCharRigCamFocusCommandSpec
        {
            slotKey = roleKey.Trim(),
            mode = CharRigCamFocusMoveMode.Add,
            position = new Vector2(x, y),
        };

        Collect(spec);
    }
}