using UnityEngine;

public sealed partial class YarnCommandBridge
{
    private void EnqueueSetDepthSpec(string roleKey, string depthArg)
    {
        EnqueueSetDepthSpec(roleKey, depthArg, 0f);
    }

    private void EnqueueSetDepthSpec(string roleKey, string depthArg, float duration)
    {
        duration = Mathf.Max(0f, duration);

        var spec = new SetDepthCommandSpecCharR
        {
            slotKey = roleKey,
            duration = duration,
        };

        ApplyDepthArg(spec, depthArg);

        Collect(spec);
    }

    private void EnqueueSetDepthFocusSpec(
        string roleKey,
        string depthArg,
        string preserveFocusArg,
        float duration)
    {
        duration = Mathf.Max(0f, duration);

        var spec = new SetDepthCommandSpecCharR
        {
            slotKey = roleKey,
            duration = duration,
        };

        ApplyDepthArg(spec, depthArg);
        ApplyPreserveFocusArg(spec, preserveFocusArg);

        Collect(spec);
    }

    private void EnqueueDepthResetSpec(string roleKey, float duration)
    {
        duration = Mathf.Max(0f, duration);

        var spec = new SetDepthCommandSpecCharR
        {
            slotKey = roleKey,
            preset = CharacterDepthPreset.Mid,
            useLevel = false,
            duration = duration,
            wait = duration > 0f,
        };

        Collect(spec);
    }

    private static void ApplyDepthArg(
        SetDepthCommandSpecCharR spec,
        string depthArg)
    {
        if (YarnNumberParser.TryParseFloat(depthArg, out float level))
        {
            spec.useLevel = true;
            spec.level = Mathf.Clamp(level, 0f, 10f);
            spec.preset = CharacterDepthPreset.Mid;
            return;
        }

        if (!CharacterDepthPresetParser.TryParse(depthArg, out CharacterDepthPreset preset))
        {
            Debug.LogWarning(
                $"[YarnCommandBridge] Unknown depth preset '{depthArg}'. " +
                $"Fallback to '{CharacterDepthPreset.Mid}'.");

            preset = CharacterDepthPreset.Mid;
        }

        spec.useLevel = false;
        spec.preset = preset;
    }

    private static void ApplyPreserveFocusArg(
        SetDepthCommandSpecCharR spec,
        string preserveFocusArg)
    {
        if (CharacterFocusPresetParser.TryParse(
                preserveFocusArg,
                out CharacterFocusPreset focusPreset))
        {
            spec.overridePreserveFocus = true;
            spec.preserveFocusPreset = focusPreset;
            return;
        }
    }
}