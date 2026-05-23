using UnityEngine;

public sealed partial class YarnCommandBridge
{
    private void EnqueueSpawnBackgroundRigSpec(
        string rigKey, string parentSlotKey = "stage00",
        string spriteKey = "green", string layerKey = "back",
        string scaleArg = "1.3",
        float x = 0f, float y = 0f, float rotationZ = 0f
        )
    {
        EnqueueSetupBackgroundRigSpec(rigKey, parentSlotKey);
        EnqueueSetBackgroundSpriteSpec(rigKey, spriteKey, layerKey);
        EnqueueSetBackgroundAnchorSpec(rigKey, x, y, rotationZ);
        EnqueueSetBackgroundOriginSizeSpec(rigKey, scaleArg);
    }

    private void EnqueueSetupBackgroundRigSpec(string rigKey, string parentSlotKey)
    {
        var spec = new SetupBackgroundRigCommandSpec
        {
            rigKey = rigKey,
            parentSlot = BackgroundRigSlotParser.Parse(parentSlotKey, BackgroundRigSlot.Stage00BackgroundSlot)
        };

        Collect(spec);
    }

    private void EnqueueSetBackgroundAnchorSpec(string rigKey, 
        float x = 0f, float y = 0f,
        float rotationZ = 0f)
    {
        var spec = new SetAnchorCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_CastTransform,
            anchoredPosition = new Vector2(x, y),
            rotationZ = rotationZ
        };

        Collect(spec);
    }

    private void EnqueueSetBackgroundSpriteSpec(string rigKey, string spriteKey = "", string layerKey = "back")
    {
        var spec = new SetBackgroundSpriteCommandSpecBgR
        {
            rigKey = rigKey,
            spriteKey = spriteKey,
            target = BackgroundRigLayerParser.ParseImageTarget(layerKey)
        };

        Collect(spec);
    }

    private void EnqueueSetBackgroundOriginSizeSpec(string rigKey, string scaleArg = "1")
    {
        if (!YarnNumberParser.TryParseFloat(scaleArg, out float absoluteScale))
            absoluteScale = 1f;

        var spec = new SetOriginSizeCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_CastTransform,

            overrideScale = true,
            scaleOverride = new Vector3(absoluteScale, absoluteScale, absoluteScale)
        };

        Collect(spec);
    }


    private void EnqueueFadeInBackgroundSpec(string rigKey, string targetKey = "root", float duration = 0.47f)
    {
        var spec = new FadeInCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTargetParser.ParseFadeTarget(targetKey),
            duration = duration,
        };

        Collect(spec);
    }

    private void EnqueueFadeOutBackgroundSpec(string rigKey, string targetKey = "root", float duration = 0.38f)
    {
        var spec = new FadeOutCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTargetParser.ParseFadeTarget(targetKey),
            duration = duration,
        };

        Collect(spec);
    }

    private void EnqueueMoveBackgroundSpec(string rigKey, float x, float y, float duration = 0.4f)
    {
        var spec = new MoveByCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Track,
            delta = new Vector2(x, y),
            duration = duration
        };

        Collect(spec);
    }

    private void EnqueueScaleBackgroundSpec(string rigKey, float scale, float duration = 0.4f)
    {
        var spec = new ScaleToCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_ActingScale,
            toScale = new Vector2(scale, scale),
            duration = duration
        };

        Collect(spec);
    }

    private void EnqueueSlideInBackgroundSpec(string rigKey, string directionKey = "left", float distance = 480f, float duration = 0.55f)
    {
        var spec = new SlideInCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Track,
            direction = BgRigDirectionParser.Parse(directionKey, CharRigDirection.Left),
            distance = distance,
            duration = duration
        };

        Collect(spec);
    }

    private void EnqueueSlideOutBackgroundSpec(string rigKey, string directionKey = "right", float distance = 480f, float duration = 0.45f)
    {
        var spec = new SlideOutCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Track,
            to = BgRigDirectionParser.Parse(directionKey, CharRigDirection.Right),
            distance = distance,
            duration = duration
        };

        Collect(spec);
    }

    private void EnqueueJoltBackgroundSpec(string rigKey, string directionKey = "right", float strength = 22f, float duration = 0.88f)
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
            anticipation = 3f
        };

        Collect(spec);
    }

    private void EnqueueTrembleBackgroundSpec(string rigKey, string directionKey = "right", float strength = 8f, float duration = 1.2f)
    {
        var spec = new TrembleCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Shake,
            direction = BgRigDirectionParser.Parse(directionKey, CharRigDirection.Right),
            strength = strength,
            duration = duration
        };

        Collect(spec);
    }

    private void EnqueueBreathBackgroundSpec(string rigKey, float duration = 99f, float height = 6f, float breathsPerSecond = 0.2f)
    {
        var spec = new BreathInPlaceCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_Track,
            duration = duration,
            breathsPerSecond = breathsPerSecond,
            height = height
        };

        Collect(spec);
    }
}