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

    private static bool IsEmptySpriteKey(string spriteKey)
    {
        string normalized = (spriteKey ?? "").Trim().ToLowerInvariant();

        return string.IsNullOrEmpty(normalized)
               || normalized == "-"
               || normalized == "none"
               || normalized == "null";
    }
}