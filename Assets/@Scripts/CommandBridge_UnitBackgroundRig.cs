using DG.Tweening;
using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    private void BindBackgroundRigDsl(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string, string>(
            "bg_spawn",
            EnqueueSpawnBackgroundRigSpec);

        runner.AddCommandHandler<string, string, string, float>(
            "bg_place",
            EnqueueSetBackgroundAnchorDslSpec);

        runner.AddCommandHandler<string, string, string>(
            "bg_sprite",
            EnqueueSetBackgroundSpriteSpec);

        runner.AddCommandHandler<string, string>(
            "bg_size",
            EnqueueSetBackgroundOriginSizeSpec);

        runner.AddCommandHandler<string, string>(
            "bg_fade_in",
            EnqueueFadeInBackgroundDslSpec);

        runner.AddCommandHandler<string, string>(
            "bg_fade_out",
            EnqueueFadeOutBackgroundDslSpec);

        runner.AddCommandHandler<string, string>(
            "bg_hide_layers",
            EnqueueHideBackgroundRootLayersSpec);

        runner.AddCommandHandler<string, string>(
            "bg_show_layers",
            EnqueueShowBackgroundRootLayersSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "bg_move",
            EnqueueMoveBackgroundDslSpec);

        runner.AddCommandHandler<string, float, string>(
            "bg_scale",
            EnqueueScaleBackgroundDslSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "bg_slide_in",
            EnqueueSlideInBackgroundDslSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "bg_slide_out",
            EnqueueSlideOutBackgroundDslSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "bg_jolt",
            EnqueueJoltBackgroundDslSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "bg_idle_tremble",
            EnqueueTrembleBackgroundDslSpec);

        runner.AddCommandHandler<string, string, string, float>(
            "bg_idle_breath",
            EnqueueBreathBackgroundDslSpec);

        runner.AddCommandHandler<string, float, string>(
            "bg_defocus",
            EnqueueBackgroundDefocusDslSpec);

        runner.AddCommandHandler<string, float, float, int, string, string>(
            "bg_defocus_custom",
            EnqueueBackgroundDefocusCustomDslSpec);

        runner.AddCommandHandler<string, string>(
            "bg_defocus_clear",
            EnqueueBackgroundDefocusClearDslSpec);
    }

    private void EnqueueSetBackgroundAnchorDslSpec(
        string rigKey,
        string xToken = "0u",
        string yToken = "0u",
        float rotationZ = 0f)
    {
        var spec = new SetAnchorCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_CastTransform,
            anchoredPosition = new Vector2(
                ParseSignedUnit(xToken, 0f),
                ParseSignedUnit(yToken, 0f)),
            rotationZ = rotationZ
        };

        Collect(spec);
    }

    private void EnqueueFadeInBackgroundDslSpec(
        string rigKey,
        string durationToken = "10fr")
    {
        var spec = new FadeInCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Root,
            duration = YarnDurationParser.Parse(durationToken, 0.4f),
        };

        Collect(spec);
    }

    private void EnqueueFadeOutBackgroundDslSpec(
        string rigKey,
        string durationToken = "10fr")
    {
        var spec = new FadeOutCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Root,
            duration = YarnDurationParser.Parse(durationToken, 0.4f),
        };

        Collect(spec);
    }

    private void EnqueueMoveBackgroundDslSpec(
        string rigKey,
        string xToken,
        string yToken,
        string durationToken = "10fr")
    {
        var spec = new MoveByCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Track,
            delta = new Vector2(
                ParseSignedUnit(xToken, 0f),
                ParseSignedUnit(yToken, 0f)),
            duration = YarnDurationParser.Parse(durationToken, 0.4f),
            ease = Ease.OutCubic
        };

        Collect(spec);
    }

    private void EnqueueScaleBackgroundDslSpec(
        string rigKey,
        float scale,
        string durationToken = "10fr")
    {
        var spec = new ScaleToCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_ActingScale,
            toScale = new Vector2(scale, scale),
            duration = YarnDurationParser.Parse(durationToken, 0.4f)
        };

        Collect(spec);
    }

    private void EnqueueSlideInBackgroundDslSpec(
        string rigKey,
        string directionKey = "left",
        string distanceToken = "12u",
        string durationToken = "13fr")
    {
        var spec = new SlideInCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Track,
            direction = BgRigDirectionParser.Parse(directionKey, CharRigDirection.Left),
            distance = YarnUnitParser.Parse(distanceToken, 12f),
            duration = YarnDurationParser.Parse(durationToken, 0.55f)
        };

        Collect(spec);
    }

    private void EnqueueSlideOutBackgroundDslSpec(
        string rigKey,
        string directionKey = "right",
        string distanceToken = "12u",
        string durationToken = "11fr")
    {
        var spec = new SlideOutCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Track,
            to = BgRigDirectionParser.Parse(directionKey, CharRigDirection.Right),
            distance = YarnUnitParser.Parse(distanceToken, 12f),
            duration = YarnDurationParser.Parse(durationToken, 0.45f)
        };

        Collect(spec);
    }

    private void EnqueueJoltBackgroundDslSpec(
        string rigKey,
        string directionKey = "right",
        string strengthToken = "0.55u",
        string durationToken = "21fr")
    {
        var spec = new JoltCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Track_Y,
            direction = BgRigDirectionParser.Parse(directionKey, CharRigDirection.Right),
            strength = YarnUnitParser.Parse(strengthToken, 0.55f),
            duration = YarnDurationParser.Parse(durationToken, 0.88f),
            taps = 3,
            damping = 6f,
            anticipation = 3f
        };

        Collect(spec);
    }

    private void EnqueueTrembleBackgroundDslSpec(
        string rigKey,
        string directionKey = "right",
        string strengthToken = "0.2u",
        string durationToken = "29fr")
    {
        var spec = new TrembleCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Shake,
            direction = BgRigDirectionParser.Parse(directionKey, CharRigDirection.Right),
            strength = YarnUnitParser.Parse(strengthToken, 0.2f),
            duration = YarnDurationParser.Parse(durationToken, 1.2f)
        };

        Collect(spec);
    }

    private void EnqueueBreathBackgroundDslSpec(
        string rigKey,
        string durationToken = "99s",
        string heightToken = "0.15u",
        float breathsPerSecond = 0.2f)
    {
        var spec = new BreathInPlaceCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Track,
            duration = YarnDurationParser.Parse(durationToken, 99f),
            height = YarnUnitParser.Parse(heightToken, 0.15f),
            breathsPerSecond = breathsPerSecond
        };

        Collect(spec);
    }

    private void EnqueueBackgroundDefocusDslSpec(
        string rigKey,
        float alpha,
        string durationToken)
    {
        Collect(new BackgroundDefocusCommandSpecBgR
        {
            rigKey = rigKey,
            alpha = alpha,
            duration = YarnDurationParser.Parse(durationToken, 0.4f),

            blurRadius = 0.5f,
            iterations = 1,
            downsample = UIStageBlurDownsample.Quarter
        });
    }

    private void EnqueueBackgroundDefocusCustomDslSpec(
        string rigKey,
        float alpha,
        float blurRadius,
        int iterations,
        string downsample,
        string durationToken)
    {
        Collect(new BackgroundDefocusCommandSpecBgR
        {
            rigKey = rigKey,
            alpha = alpha,
            duration = YarnDurationParser.Parse(durationToken, 0.4f),
            blurRadius = blurRadius,
            iterations = iterations,
            downsample = ParseBlurDownsample(downsample)
        });
    }

    private void EnqueueBackgroundDefocusClearDslSpec(
        string rigKey,
        string durationToken = "10fr")
    {
        Collect(new BackgroundDefocusClearCommandSpecBgR
        {
            rigKey = rigKey,
            duration = YarnDurationParser.Parse(durationToken, 0.4f)
        });
    }

    private static float ParseSignedUnit(
        string token,
        float fallbackUnits)
    {
        if (string.IsNullOrWhiteSpace(token))
            return YarnUnitParser.Parse(token, fallbackUnits);

        string trimmed = token.Trim();

        if (trimmed.StartsWith("-", System.StringComparison.Ordinal))
            return -YarnUnitParser.Parse(trimmed[1..], Mathf.Abs(fallbackUnits));

        if (trimmed.StartsWith("+", System.StringComparison.Ordinal))
            return YarnUnitParser.Parse(trimmed[1..], Mathf.Abs(fallbackUnits));

        return YarnUnitParser.Parse(trimmed, fallbackUnits);
    }
}