using System;
using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    private const float PauseFramesPerSecond = 24f;
    private const int FramePauseAliasMaxFrame = 48;

    
    // Animator-style frame wait aliases.
    // 24fps basis: <<24fr>> = 1 second.
    // Registered up to <<48fr>> = 2 seconds.
    // Longer or precise waits should use <<pause seconds>>.
    private void BindFramePauseAliases(DialogueRunner runner)
    {
        for (int i = 1; i <= FramePauseAliasMaxFrame; i++)
        {
            int frame = i;
            float seconds = frame / PauseFramesPerSecond;

            runner.AddCommandHandler(
                $"{frame}fr",
                () => EnqueueWaitSpec(seconds));
        }
    }
    
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
        var spec = new AttachCharRigToBackgroundObjectSlotCommandSpec
        {
            charRigKey = charRigKey,
            backgroundRigKey = backgroundRigKey,
            parentTarget = ParseBackgroundRigTargetOrDefault(parentTarget),
            worldPositionStays = false,
            setAsLastSibling = true,
            wait = true
        };

        Collect(spec);
    }
    
    private void EnqueueBackgroundCutInSpec(
        string backgroundRigKey,
        string parentTarget = "Background_ObjectSlotRoot")
    {
        Collect(new ScaleToCommandSpecBgR
        {
            rigKey = backgroundRigKey,
            target = BackgroundRigTarget.Background_CastTransform,
            toScale = new Vector2(0.5f, 0.5f),
            duration = 0
        });

        Collect(new ScaleToCommandSpecBgR
        {
            rigKey = backgroundRigKey,
            target = BackgroundRigTarget.Background_ObjectSlotRoot,
            toScale = new Vector2(2f, 2f),
            duration = 0
        });

        Collect(new MoveByCommandSpecBgR
        {
            rigKey = backgroundRigKey,
            target = BackgroundRigTarget.Background_ObjectSlotRoot,
            delta = new Vector2(0, -380),
            duration = 0
        });

        Collect(new SetBackgroundSpriteCommandSpecBgR
        {
            rigKey = backgroundRigKey,
            spriteKey = "slot3bg",
            target = BackgroundRigTarget.Background_LayerRoot
        });

        Collect(new SetBackgroundSpriteCommandSpecBgR
        {
            rigKey = backgroundRigKey,
            spriteKey = "slot3bg2",
            target = BackgroundRigTarget.Background_BackLayer_Image
        });

        // Cut-in slot visibility:
        // hide front/root layers, show object slot layer.
        Collect(new HideRootLayersCommandSpecBgR
        {
            rigKey = backgroundRigKey,
            targetMask = BackgroundRigRootMask.Background_FrontLayer_Root,
            wait = false
        });

        Collect(new HideRootLayersCommandSpecBgR
        {
            rigKey = backgroundRigKey,
            targetMask = BackgroundRigRootMask.Background_Root,
            wait = false
        });

        Collect(new ShowRootLayersCommandSpecBgR
        {
            rigKey = backgroundRigKey,
            targetMask = BackgroundRigRootMask.Background_ObjectSlotRoot,
            wait = false
        });
    }
    
    private void SetPresentationActor(string aliasOrActor, string actorKey = null)
    {
        if (string.IsNullOrEmpty(actorKey))
        {
            // 1-arg: <<pres_actor c1>> → 기본 alias '@' → c1
            _playbackDriver.SetPresentationActor(aliasOrActor);
            return;
        }

        // 2-arg: <<pres_actor @2 c2>> → alias '@2' → c2
        _playbackDriver.RegisterPresentationActorAlias(aliasOrActor, actorKey);
    }
    
    private BackgroundRigTarget ParseBackgroundRigTargetOrDefault(string parentTarget)
    {
        if (Enum.TryParse(parentTarget, out BackgroundRigTarget parsedTarget))
            return parsedTarget;

        Debug.LogWarning(
            $"[YarnCommandBridge] Invalid BackgroundRigTarget '{parentTarget}'. " +
            "Fallback to Background_ObjectSlotRoot.");

        return BackgroundRigTarget.Background_ObjectSlotRoot;
    }
}