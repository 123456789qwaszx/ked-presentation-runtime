using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    private void BindScreenEffects(DialogueRunner runner)
    {
        runner.AddCommandHandler<float, float>("screen_flash", EnqueueScreenFlashSpec);
        runner.AddCommandHandler<float, float, float, float, float>("screen_flash_rgb", EnqueueScreenFlashRgbSpec);
        runner.AddCommandHandler("screen_flash_hit", EnqueueScreenFlashHitSpec);
    }

    private void EnqueueScreenFlashSpec(float amount = 1f, float duration = 0.16f)
    {
        var spec = new ScreenFlashCommandSpec
        {
            color = Color.white,
            amount = amount,
            attackDuration = 0.02f,
            holdDuration = 0.01f,
            releaseDuration = Mathf.Max(0f, duration),
            wait = false
        };

        Collect(spec);
    }

    private void EnqueueScreenFlashRgbSpec(
        float r,
        float g,
        float b,
        float amount = 1f,
        float duration = 0.16f)
    {
        var spec = new ScreenFlashCommandSpec
        {
            color = new Color(r, g, b, 1f),
            amount = amount,
            attackDuration = 0.02f,
            holdDuration = 0.01f,
            releaseDuration = Mathf.Max(0f, duration),
            wait = false
        };

        Collect(spec);
    }

    private void EnqueueScreenFlashHitSpec()
    {
        var spec = new ScreenFlashCommandSpec
        {
            color = new Color(1f, 0.16f, 0.10f, 1f),
            amount = 0.45f,
            attackDuration = 0.015f,
            holdDuration = 0.015f,
            releaseDuration = 0.18f,
            wait = false
        };

        Collect(spec);
    }
}