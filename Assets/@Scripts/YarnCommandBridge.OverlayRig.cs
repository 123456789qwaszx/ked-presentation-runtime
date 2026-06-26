using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    private void BindOverlayRig(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string>(
            "overlay_rig", EnqueueSetupOverlayRigSpec);

        runner.AddCommandHandler<string>(
            "sprite_rig", EnqueueSetupSpriteOverlayRigSpec);

        runner.AddCommandHandler<string>(
            "text_rig", EnqueueSetupTextOverlayRigSpec);
        
        
        runner.AddCommandHandler<string, float, float, string>(
            "overlay_move", EnqueueOverlayMoveToSpec);

        runner.AddCommandHandler<string, float, float, string>(
            "overlay_move_by", EnqueueOverlayMoveBySpec);

        runner.AddCommandHandler<string, float, float, string>(
            "overlay_size", EnqueueOverlaySizeSpec);

        runner.AddCommandHandler<string, float, float, string>(
            "overlay_size_by", EnqueueOverlaySizeBySpec);

        runner.AddCommandHandler<string, float, float, string>(
            "overlay_scale", EnqueueOverlayScaleSpec);

        runner.AddCommandHandler<string, float, float, string>(
            "overlay_scale_by", EnqueueOverlayScaleBySpec);

        runner.AddCommandHandler<string, string>(
            "overlay_show", EnqueueOverlayShowSpec);

        runner.AddCommandHandler<string, string>(
            "overlay_hide", EnqueueOverlayHideSpec);
        

        runner.AddCommandHandler<string, string, string>(
            "overlay_sprite", EnqueueOverlaySpriteSpec);

        runner.AddCommandHandler<string, string>(
            "overlay_text", EnqueueOverlayTextSpec);
    }

    private void EnqueueSetupOverlayRigSpec(
        string overlayKey,
        string rootKindToken = "sprite")
    {
        var spec = new SetupOverlayRigCommandSpec
        {
            overlayKey = overlayKey,
            rootKind = StageOverlayRigRootKindParser.Parse(
                rootKindToken,
                StageOverlayRigRootKind.Sprite),
            prefab = _overlayRigPrefab,
            rootName = "OverlayRig"
        };

        Collect(spec);
    }

    private void EnqueueSetupSpriteOverlayRigSpec(string overlayKey)
    {
        var spec = new SetupOverlayRigCommandSpec
        {
            overlayKey = overlayKey,
            rootKind = StageOverlayRigRootKind.Sprite,
            prefab = _overlayRigPrefab,
            rootName = "SpriteOverlayRig"
        };

        Collect(spec);
    }

    private void EnqueueSetupTextOverlayRigSpec(string overlayKey)
    {
        var spec = new SetupOverlayRigCommandSpec
        {
            overlayKey = overlayKey,
            rootKind = StageOverlayRigRootKind.Text,
            prefab = _overlayRigPrefab,
            rootName = "TextOverlayRig"
        };

        Collect(spec);
    }


    private void EnqueueOverlayMoveToSpec(
        string rigKey,
        float x,
        float y,
        string durationToken = "0s")
    {
        Collect(new OverlayMoveCommandSpec
        {
            rigKey = rigKey,
            target = OverlayRigTarget.Overlay_Track,
            useAbsolutePosition = true,
            delta = new Vector2(x, y),
            duration = YarnDurationParser.Parse(durationToken, 0f),
        });
    }

    private void EnqueueOverlayMoveBySpec(
        string rigKey,
        float x,
        float y,
        string durationToken = "8fr")
    {
        Collect(new OverlayMoveCommandSpec
        {
            rigKey = rigKey,
            target = OverlayRigTarget.Overlay_Track,
            useAbsolutePosition = false,
            delta = new Vector2(x, y),
            duration = YarnDurationParser.Parse(durationToken, 0.35f),
        });
    }

    private void EnqueueOverlaySizeSpec(
        string rigKey,
        float width,
        float height,
        string durationToken = "0s")
    {
        Collect(new OverlaySizeCommandSpec
        {
            rigKey = rigKey,
            target = OverlayRigTarget.Overlay_Size,
            relativeToCurrent = false,
            sizeDelta = new Vector2(width, height),
            duration = YarnDurationParser.Parse(durationToken, 0f),
        });
    }

    private void EnqueueOverlaySizeBySpec(
        string rigKey,
        float widthDelta,
        float heightDelta,
        string durationToken = "8fr")
    {
        Collect(new OverlaySizeCommandSpec
        {
            rigKey = rigKey,
            target = OverlayRigTarget.Overlay_Size,
            relativeToCurrent = true,
            sizeDelta = new Vector2(widthDelta, heightDelta),
            duration = YarnDurationParser.Parse(durationToken, 0.35f),
        });
    }

    private void EnqueueOverlayScaleSpec(
        string rigKey,
        float x,
        float y,
        string durationToken = "0s")
    {
        Collect(new OverlayScaleCommandSpec
        {
            rigKey = rigKey,
            target = OverlayRigTarget.Overlay_Scale,
            relativeToCurrent = false,
            scale = new Vector2(x, y),
            duration = YarnDurationParser.Parse(durationToken, 0f),
        });
    }

    private void EnqueueOverlayScaleBySpec(
        string rigKey,
        float x,
        float y,
        string durationToken = "8fr")
    {
        Collect(new OverlayScaleCommandSpec
        {
            rigKey = rigKey,
            target = OverlayRigTarget.Overlay_Scale,
            relativeToCurrent = true,
            scale = new Vector2(x, y),
            duration = YarnDurationParser.Parse(durationToken, 0.35f),
        });
    }

    private void EnqueueOverlayShowSpec(
        string rigKey,
        string durationToken = "8fr")
    {
        Collect(new OverlayShowCommandSpec
        {
            rigKey = rigKey,
            duration = YarnDurationParser.Parse(durationToken, 0.15f),
        });
    }

    private void EnqueueOverlayHideSpec(
        string rigKey,
        string durationToken = "8fr")
    {
        Collect(new OverlayHideCommandSpec
        {
            rigKey = rigKey,
            duration = YarnDurationParser.Parse(durationToken, 0.15f),
        });
    }

    private void EnqueueOverlaySpriteSpec(
        string rigKey,
        string resourcesPath,
        string setNativeSizeToken = "true")
    {
        Collect(new OverlaySpriteCommandSpec
        {
            rigKey = rigKey,
            resourcesPath = resourcesPath,
            setNativeSize = !string.Equals(
                setNativeSizeToken,
                "false",
                System.StringComparison.OrdinalIgnoreCase),
        });
    }

    private void EnqueueOverlayTextSpec(
        string rigKey,
        string text)
    {
        Collect(new OverlayTextCommandSpec
        {
            rigKey = rigKey,
            text = text,
        });
    }
}