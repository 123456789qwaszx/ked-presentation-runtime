using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    public void RegisterBackgroundRigCommands()
    {
        _dialogueRunner.AddCommandHandler<string, string, string, string, float, float, string, float>(
            "spawn_bg",
            EnqueueSpawnBackgroundRigSpec);

        _dialogueRunner.AddCommandHandler<string, float, float, float, float, float>(
            "bg_place",
            EnqueueSetBackgroundAnchorSpec);

        _dialogueRunner.AddCommandHandler<string, string, string>(
            "bg_sprite",
            EnqueueSetBackgroundSpriteSpec);

        _dialogueRunner.AddCommandHandler<string, string>(
            "bg_size",
            EnqueueSetBackgroundOriginSizeSpec);

        _dialogueRunner.AddCommandHandler<string, string, float>(
            "bg_fade_in",
            EnqueueFadeInBackgroundSpec);

        _dialogueRunner.AddCommandHandler<string, string, float>(
            "bg_fade_out",
            EnqueueFadeOutBackgroundSpec);

        _dialogueRunner.AddCommandHandler<string, float, float, float>(
            "bg_move",
            EnqueueMoveBackgroundSpec);

        _dialogueRunner.AddCommandHandler<string, float, float>(
            "bg_scale",
            EnqueueScaleBackgroundSpec);

        _dialogueRunner.AddCommandHandler<string, string, float, float>(
            "bg_slide_in",
            EnqueueSlideInBackgroundSpec);

        _dialogueRunner.AddCommandHandler<string, string, float, float>(
            "bg_slide_out",
            EnqueueSlideOutBackgroundSpec);

        _dialogueRunner.AddCommandHandler<string, string, float, float>(
            "bg_jolt",
            EnqueueJoltBackgroundSpec);

        _dialogueRunner.AddCommandHandler<string, string, float, float>(
            "bg_tremble",
            EnqueueTrembleBackgroundSpec);

        _dialogueRunner.AddCommandHandler<string, float, float, float>(
            "bg_breath",
            EnqueueBreathBackgroundSpec);
    }

    private void EnqueueSpawnBackgroundRigSpec(
        string rigKey, string parentSlotKey = "stage00",
        string spriteKey = "green", string layerKey = "back",
        float x = 0f, float y = 0f, string scaleArg = "1.3", float rotationZ = 0f)
    {
        EnqueueSetupBackgroundRigSpec(rigKey, parentSlotKey);

        if (!IsEmptySpriteKey(spriteKey))
            EnqueueSetBackgroundSpriteSpec(rigKey, spriteKey, layerKey);

        EnqueueSetBackgroundAnchorSpec(
            rigKey,
            x,
            y,
            1f,
            1f,
            rotationZ);

        EnqueueSetBackgroundOriginSizeSpec(rigKey, scaleArg);
    }

    private void EnqueueSetupBackgroundRigSpec(string rigKey, string parentSlotKey)
    {
        var spec = new SetupBackgroundRigCommandSpec
        {
            rigKey = rigKey,
            parentSlot = BackgroundRigSlotParser.Parse(parentSlotKey, BackgroundRigSlot.Stage00BackgroundSlot),
            rigRootName = "BackgroundRig",
            rigPrefab = null
        };

        Collect(spec);
    }

    private void EnqueueSetBackgroundAnchorSpec(
        string rigKey,
        float x = 0f,
        float y = 0f,
        float scaleX = 1f,
        float scaleY = 1f,
        float rotationZ = 0f)
    {
        var spec = new SetAnchorCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_CastTransform,
            anchoredPosition = new Vector2(x, y),
            scale = new Vector2(scaleX, scaleY),
            rotationZ = rotationZ,
            resetFraming = false,
            resetActing = true
        };

        Collect(spec);
    }

    private void EnqueueSetBackgroundSpriteSpec(
        string rigKey,
        string spriteKey,
        string layerKey = "back")
    {
        if (IsEmptySpriteKey(spriteKey))
            return;

        var spec = new SetBackgroundSpriteCommandSpecBgR
        {
            rigKey = rigKey,
            spriteKey = spriteKey,
            target = BackgroundRigLayerParser.ParseImageTarget(
                layerKey,
                BackgroundRigTarget.Background_BackLayer_Image),
            sizingMode = CharRigImageSizingMode.HeightFitPreserveAspect,
            horizontalAlign = CharRigImageSizingPolicy.HorizontalAlign.Center
        };

        Collect(spec);
    }

    private void EnqueueSetBackgroundOriginSizeSpec(
        string rigKey,
        string scaleArg = "1")
    {
        if (!YarnNumberParser.TryParseFloat(scaleArg, out float absoluteScale))
        {
            Debug.LogWarning(
                $"[YarnCommandBridge] Unknown background scale '{scaleArg}'. " +
                $"Fallback to '1'. rigKey='{rigKey}'.");

            absoluteScale = 1f;
        }

        var spec = new SetOriginSizeCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_CastTransform,

            overrideScale = true,
            scaleOverride = new Vector3(absoluteScale, absoluteScale, absoluteScale),

            scale = absoluteScale,
            uniformScale = true
        };

        Collect(spec);
    }


    private void EnqueueFadeInBackgroundSpec(
        string rigKey,
        string targetKey = "root",
        float duration = 0.47f)
    {
        var spec = new FadeInCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTargetParser.ParseFadeTarget(
                targetKey,
                BackgroundRigTarget.Background_Root),
            duration = duration,
            ease = DG.Tweening.Ease.OutCubic,
            enableInteraction = false
        };

        Collect(spec);
    }

    private void EnqueueFadeOutBackgroundSpec(
        string rigKey,
        string targetKey = "root",
        float duration = 0.38f)
    {
        var spec = new FadeOutCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTargetParser.ParseFadeTarget(
                targetKey,
                BackgroundRigTarget.Background_Root),
            duration = duration,
            ease = DG.Tweening.Ease.OutCubic,
            disableInteraction = true
        };

        Collect(spec);
    }

    private void EnqueueMoveBackgroundSpec(
        string rigKey,
        float x,
        float y,
        float duration = 0.4f)
    {
        var spec = new MoveByCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Track,
            delta = new Vector2(x, y),
            duration = duration,
            ease = DG.Tweening.Ease.OutCubic,
            killTween = true
        };

        Collect(spec);
    }

    private void EnqueueScaleBackgroundSpec(
        string rigKey,
        float scale,
        float duration = 0.4f)
    {
        var spec = new ScaleToCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_ActingScale,
            toScale = new Vector2(scale, scale),
            duration = duration,
            ease = DG.Tweening.Ease.OutCubic,
            killTween = true
        };

        Collect(spec);
    }

    private void EnqueueSlideInBackgroundSpec(
        string rigKey,
        string directionKey = "left",
        float distance = 480f,
        float duration = 0.55f)
    {
        var spec = new SlideInCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Track,
            direction = BgRigDirectionParser.Parse(directionKey, CharRigDirection.Left),
            distance = distance,
            duration = duration,
            ease = DG.Tweening.Ease.OutCubic,
            punch = 24f,
            killTween = true
        };

        Collect(spec);
    }

    private void EnqueueSlideOutBackgroundSpec(
        string rigKey,
        string directionKey = "right",
        float distance = 480f,
        float duration = 0.45f)
    {
        var spec = new SlideOutCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Track,
            to = BgRigDirectionParser.Parse(directionKey, CharRigDirection.Right),
            distance = distance,
            duration = duration,
            ease = DG.Tweening.Ease.InCubic,
            punch = 14f,
            killTween = true
        };

        Collect(spec);
    }

    private void EnqueueJoltBackgroundSpec(
        string rigKey,
        string directionKey = "right",
        float strength = 22f,
        float duration = 0.88f)
    {
        var spec = new JoltCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Track_Y,
            direction = BgRigDirectionParser.Parse(directionKey, CharRigDirection.Right),
            strength = strength,
            duration = duration,
            taps = 3,
            damping = 6f,
            anticipation = 3f,
            killTween = true
        };

        Collect(spec);
    }

    private void EnqueueTrembleBackgroundSpec(
        string rigKey,
        string directionKey = "right",
        float strength = 8f,
        float duration = 1.2f)
    {
        var spec = new TrembleCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Shake,
            direction = BgRigDirectionParser.Parse(directionKey, CharRigDirection.Right),
            strength = strength,
            duration = duration,
            frequency = 24f,
            crossAxisRatio = 0.35f,
            noiseRatio = 0.25f,
            usePulse = false,
            pulseInterval = 1.0f,
            pulseDuration = 0.16f,
            blendIn = 0.04f,
            blendOut = 0.08f,
            killTween = true
        };

        Collect(spec);
    }

    private void EnqueueBreathBackgroundSpec(
        string rigKey,
        float duration = 99f,
        float height = 6f,
        float breathsPerSecond = 0.2f)
    {
        var spec = new BreathInPlaceCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Track,
            duration = duration,
            breathsPerSecond = breathsPerSecond,
            height = height,
            sideSway = 0f,
            useScalePulse = false,
            scaleAmount = 0.005f,
            ease = DG.Tweening.Ease.InOutSine,
            phaseOffset = 0f,
            blendIn = 0.25f,
            blendOut = 0.25f,
            killTween = true
        };

        Collect(spec);
    }

    private static bool IsEmptySpriteKey(string spriteKey)
    {
        string normalized = (spriteKey ?? "").Trim().ToLowerInvariant();

        return string.IsNullOrEmpty(normalized)
               || normalized == "-"
               || normalized == "none"
               || normalized == "null";
    }
}