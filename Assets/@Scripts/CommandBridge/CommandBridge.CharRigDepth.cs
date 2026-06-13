using UnityEngine;

public sealed partial class YarnCommandBridge
{
    private void EnqueueSetDepthSpec(string roleKey, string depthArg)
    {
        // 즉시 상태 세팅.
        EnqueueSetDepthSpec(roleKey, depthArg, 0f);
    }

    private void EnqueueSetDepthSpec(string roleKey, string depthArg, float duration)
    {
        duration = Mathf.Max(0f, duration);

        var spec = new SetDepthCommandSpecCharR
        {
            slotKey = roleKey,
            duration = duration,

            // duration이 명시된 depth_to는 실제로 그 시간 동안 line을 붙잡는다.
            wait = duration > 0f,
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

            // <<depth_focus_to c1 8 face 3>> 이 3초 동안 유지되도록 한다.
            //wait = duration > 0f,
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
        if (spec == null)
            return;

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
        if (spec == null)
            return;

        if (string.IsNullOrWhiteSpace(preserveFocusArg))
            return;

        if (CharacterFocusPresetParser.TryParse(
                preserveFocusArg,
                out CharacterFocusPreset focusPreset))
        {
            spec.overridePreserveFocus = true;
            spec.preserveFocusPreset = focusPreset;
            spec.preserveCustomFocusKey = "";
            return;
        }

        // 알 수 없는 focusArg는 custom point key로 취급한다.
        spec.overridePreserveFocus = true;
        spec.preserveFocusPreset = CharacterFocusPreset.Custom;
        spec.preserveCustomFocusKey = preserveFocusArg.Trim();
    }
}