using System;
using UnityEngine;

public sealed partial class YarnCommandBridge
{
    private void EnqueueWaitSpec(float duration = 0.18f)
    {
        var spec = new WaitCommandSpec()
        {
            duration = duration,
        };
        
        Collect(spec);
    }
    
    private void EnqueueUIPatchSpec(string themeId = "default")
    {
        var spec = new UIPatchCommandSpec
        {
            themeId = themeId,
        };

        Collect(spec);
    }
    
    private void SetNamedLineBoxKind(string key)
    {
        if (!TryParseDialogueBoxKind(key, out DialogueBoxKind kind))
        {
            Debug.LogWarning($"[YarnCommandBridge] Invalid named dialogue box kind. key={key}");
            return;
        }

        _dialogueBoxPresentation.SetNamedLineBoxKind(kind);
    }

    private void SetProtagonistLineBoxKind(string key)
    {
        if (!TryParseDialogueBoxKind(key, out DialogueBoxKind kind))
        {
            Debug.LogWarning($"[YarnCommandBridge] Invalid protagonist dialogue box kind. key={key}");
            return;
        }

        _dialogueBoxPresentation.SetProtagonistLineBoxKind(kind);
    }

    private void SetDefaultLineBoxKinds(string protagonistKey, string namedKey)
    {
        if (!TryParseDialogueBoxKind(protagonistKey, out DialogueBoxKind protagonistKind))
        {
            Debug.LogWarning($"[YarnCommandBridge] Invalid protagonist dialogue box kind. key={protagonistKey}");
            return;
        }

        if (!TryParseDialogueBoxKind(namedKey, out DialogueBoxKind namedKind))
        {
            Debug.LogWarning($"[YarnCommandBridge] Invalid named dialogue box kind. key={namedKey}");
            return;
        }

        _dialogueBoxPresentation.SetDefaultLineBoxKinds(protagonistKind, namedKind);
    }

    private void ResetDefaultLineBoxKinds()
    {
        _dialogueBoxPresentation.ResetDefaultLineBoxKinds();
    }

    private static bool TryParseDialogueBoxKind(string key, out DialogueBoxKind kind)
    {
        return Enum.TryParse(key, true, out kind);
    }
    
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
        
        Collect(spec);
        Collect(spec2);
        Collect(spec4);
        Collect(spec5);
        Collect(spec6);
        Collect(spec7);
    }
}