using System;
using DG.Tweening;
using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    public void RegisterShotCommands()
    {
        _dialogueRunner.AddCommandHandler("shot_reset", (Action<float>)EnqueueShotResetSpec);
        
        _dialogueRunner.AddCommandHandler("shot_zoom", (Action<string, float, float>)EnqueueShotZoomFocusSpec);
        
        _dialogueRunner.AddCommandHandler("shot_track", (Action<string, float, float, float>)EnqueueShotTrackToSpec);
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