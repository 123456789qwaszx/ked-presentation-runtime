using System;
using UnityEngine;

public sealed partial class YarnCommandBridge
{
    private void EnqueueWaitSpec(float duration)
    {
        var spec = new WaitCommandSpec()
        {
            seconds = duration,
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
}