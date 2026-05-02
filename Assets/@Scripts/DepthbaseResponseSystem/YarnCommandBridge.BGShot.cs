using System;
using DG.Tweening;
using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    public void RegisterShotCommands()
    {
        _dialogueRunner.AddCommandHandler(
            "shot_reset",
            (Action<float>)EnqueueShotResetSpec);

        _dialogueRunner.AddCommandHandler(
            "shot_pan",
            (Action<float, float, float>)EnqueueShotPanToSpec);

        _dialogueRunner.AddCommandHandler(
            "shot_pan_delta",
            (Action<float, float, float>)EnqueueShotPanDeltaSpec);

        _dialogueRunner.AddCommandHandler(
            "shot_zoom",
            (Action<float, float>)EnqueueShotZoomSpec);

        _dialogueRunner.AddCommandHandler(
            "shot_zoom_focus",
            (Action<string, float, float>)EnqueueShotZoomFocusSpec);

        _dialogueRunner.AddCommandHandler(
            "shot_track",
            (Action<string, float>)EnqueueShotTrackSpec);

        _dialogueRunner.AddCommandHandler(
            "shot_track_to",
            (Action<string, float, float, float>)EnqueueShotTrackToSpec);
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

    private void EnqueueShotPanToSpec(float panX, float panY, float duration = 0.35f)
    {
        var spec = new ShotPanToCommandSpec
        {
            panX = Mathf.Clamp(panX, -10f, 10f),
            panY = Mathf.Clamp(panY, -10f, 10f),
            absolutePan = true,
            duration = Mathf.Max(0f, duration),
            ease = Ease.OutCubic,
            wait = false
        };

        Collect(spec);
    }

    private void EnqueueShotPanDeltaSpec(float panX, float panY, float duration = 0.35f)
    {
        var spec = new ShotPanToCommandSpec
        {
            panX = Mathf.Clamp(panX, -10f, 10f),
            panY = Mathf.Clamp(panY, -10f, 10f),
            absolutePan = false,
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
            focusRoleKey = "",
            reframeToFocus = false,

            zoom = Mathf.Clamp(zoom, -10f, 10f),
            panX = 0f,
            panY = 0f,

            absoluteZoom = true,
            absolutePan = false,

            duration = Mathf.Max(0f, duration),
            ease = Ease.OutCubic,
            wait = false
        };

        Collect(spec);
    }

    private void EnqueueShotZoomFocusSpec(string roleKey, float zoom, float duration = 0.45f)
    {
        if (string.IsNullOrWhiteSpace(roleKey))
        {
            Debug.LogError("[YarnCommandBridge] shot_zoom_focus: roleKey is null or empty.");
            return;
        }

        var spec = new ShotZoomCommandSpec
        {
            focusRoleKey = roleKey.Trim(),
            focusTarget = CharacterRigTarget.CharacterPortrait_Root,
            focusLocalOffset = Vector2.zero,

            reframeToFocus = true,
            desiredFramingPoint = Vector2.zero,

            zoom = Mathf.Clamp(zoom, -10f, 10f),
            panX = 0f,
            panY = 0f,

            absoluteZoom = true,
            absolutePan = false,

            duration = Mathf.Max(0f, duration),
            ease = Ease.OutCubic,
            wait = false
        };

        Collect(spec);
    }

    private void EnqueueShotTrackSpec(string roleKey, float duration = 0.35f)
    {
        EnqueueShotTrackToSpec(roleKey, 0f, 0f, duration);
    }

    private void EnqueueShotTrackToSpec(string roleKey, float frameX, float frameY, float duration = 0.35f)
    {
        if (string.IsNullOrWhiteSpace(roleKey))
        {
            Debug.LogError("[YarnCommandBridge] shot_track: roleKey is null or empty.");
            return;
        }

        var spec = new ShotTrackCommandSpec
        {
            focusRoleKey = roleKey.Trim(),
            focusTarget = CharacterRigTarget.CharacterPortrait_Root,
            focusLocalOffset = Vector2.zero,
            desiredFramingPoint = new Vector2(frameX, frameY),

            duration = Mathf.Max(0f, duration),
            ease = Ease.OutCubic,
            wait = false
        };

        Collect(spec);
    }
}