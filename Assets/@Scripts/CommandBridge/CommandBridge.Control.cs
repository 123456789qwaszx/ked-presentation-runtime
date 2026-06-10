using System;
using UnityEngine;

public sealed partial class YarnCommandBridge
{
    private void EnqueueWaitSpec(float duration = 0.18f)
        => Collect(new WaitCommandSpec() { duration = duration });

    private void EnqueueUIPatchSpec(string themeId = "default")
        => Collect(new UIPatchCommandSpec { themeId = themeId });


    private void SetNamedLineBoxKind(string key)
    {
        Enum.TryParse(key, true, out DialogueBoxKind kind);
        _dialogueBoxPresentation.SetNamedLineBoxKind(kind);
    }

    private void SetProtagonistLineBoxKind(string key)
    {
        Enum.TryParse(key, true, out DialogueBoxKind kind);
        _dialogueBoxPresentation.SetProtagonistLineBoxKind(kind);
    }

    private void ResetDefaultLineBoxKinds()
        => _dialogueBoxPresentation.ResetDefaultLineBoxKinds();


    private void LogImmediate(string message)
    {
        Debug.Log($"[YarnCommandBridge] {message}");
    }

    private void EnqueueAttachCharRigToBackgroundObjectSlotSpec(
        string charRigKey,
        string backgroundRigKey,
        string parentTarget = "Background_ObjectSlotRoot")
    {
        if (!Enum.TryParse(parentTarget, out BackgroundRigTarget parsedTarget))
        {
            Debug.LogWarning(
                $"[YarnCommandBridge] Invalid BackgroundRigTarget '{parentTarget}'. " +
                "Fallback to Background_ObjectSlotRoot.");

            parsedTarget = BackgroundRigTarget.Background_ObjectSlotRoot;
        }

        var spec = new AttachCharRigToBackgroundObjectSlotCommandSpec
        {
            charRigKey = charRigKey,
            backgroundRigKey = backgroundRigKey,
            parentTarget = parsedTarget,
            worldPositionStays = false,
            setAsLastSibling = true,
            wait = true
        };

        var spec2 = new ScaleToCommandSpecBgR
        {
            rigKey = backgroundRigKey,
            target = BackgroundRigTarget.Background_CastTransform,
            toScale = new Vector2(0.5f, 0.5f),
            duration = 0
        };

        var spec4 = new ScaleToCommandSpecBgR
        {
            rigKey = backgroundRigKey,
            target = BackgroundRigTarget.Background_ObjectSlotRoot,
            toScale = new Vector2(2f, 2f),
            duration = 0
        };

        var spec5 = new MoveByCommandSpecBgR
        {
            rigKey = backgroundRigKey,
            target = BackgroundRigTarget.Background_ObjectSlotRoot,
            delta = new Vector2(0, -380),
            duration = 0
        };

        var spec6 = new SetBackgroundSpriteCommandSpecBgR
        {
            rigKey = backgroundRigKey,
            spriteKey = "slot3bg",
            target = BackgroundRigTarget.Background_LayerRoot
        };

        var spec7 = new SetBackgroundSpriteCommandSpecBgR
        {
            rigKey = backgroundRigKey,
            spriteKey = "slot3bg2",
            target = BackgroundRigTarget.Background_BackLayer_Image
        };

        // CutInSlot layer visibility setup:
        // front hide, root hide, object show.
        var spec8 = new HideRootLayersCommandSpecBgR
        {
            rigKey = backgroundRigKey,
            targetMask = BackgroundRigRootMask.Background_FrontLayer_Root,
            wait = false
        };

        var spec9 = new HideRootLayersCommandSpecBgR
        {
            rigKey = backgroundRigKey,
            targetMask = BackgroundRigRootMask.Background_Root,
            wait = false
        };

        var spec10 = new ShowRootLayersCommandSpecBgR
        {
            rigKey = backgroundRigKey,
            targetMask = BackgroundRigRootMask.Background_ObjectSlotRoot,
            wait = false
        };

        Collect(spec);
        Collect(spec2);
        Collect(spec4);
        Collect(spec5);
        Collect(spec6);
        Collect(spec7);
        Collect(spec8);
        Collect(spec9);
        Collect(spec10);
    }
}