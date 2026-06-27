using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    private const string DefaultDepthFocusPresetToken = "bust";
    private const string DefaultDepthFocusDurationToken = "10fr";

    private void RegisterDepthFocusCommands(DialogueRunner runner)
    {
        // Generic form:
        // <<at c1 close bust 12fr>>
        // <<at c1 front>>
        // <<at c1 7 face 8fr>>
        runner.AddCommandHandler<string, string, string, string>(
            "at", EnqueueDepthAtSpec);

        runner.AddCommandHandler<string, string, string>(
            "at_far", EnqueueDepthAtFarSpec);
        
        runner.AddCommandHandler<string, string, string>(
            "at_back", EnqueueDepthAtBackSpec);
        
        runner.AddCommandHandler<string, string, string>(
            "at_mid", EnqueueDepthAtMidSpec);

        runner.AddCommandHandler<string, string, string>(
            "at_front", EnqueueDepthAtFrontSpec);
        
        runner.AddCommandHandler<string, string, string>(
            "at_close", EnqueueDepthAtCloseSpec);
        

    }

    private void EnqueueDepthAtSpec(
        string roleKey,
        string depthArg,
        string preserveFocusArg = DefaultDepthFocusPresetToken,
        string durationToken = DefaultDepthFocusDurationToken)
    {
        EnqueueDepthAtPresetSpec(
            roleKey,
            depthArg,
            preserveFocusArg,
            durationToken);
    }

    private void EnqueueDepthAtCloseSpec(
        string roleKey,
        string preserveFocusArg = DefaultDepthFocusPresetToken,
        string durationToken = DefaultDepthFocusDurationToken)
    {
        EnqueueDepthAtPresetSpec(
            roleKey,
            "close",
            preserveFocusArg,
            durationToken);
    }

    private void EnqueueDepthAtFrontSpec(
        string roleKey,
        string preserveFocusArg = DefaultDepthFocusPresetToken,
        string durationToken = DefaultDepthFocusDurationToken)
    {
        EnqueueDepthAtPresetSpec(
            roleKey,
            "front",
            preserveFocusArg,
            durationToken);
    }

    private void EnqueueDepthAtMidSpec(
        string roleKey,
        string preserveFocusArg = DefaultDepthFocusPresetToken,
        string durationToken = DefaultDepthFocusDurationToken)
    {
        EnqueueDepthAtPresetSpec(
            roleKey,
            "mid",
            preserveFocusArg,
            durationToken);
    }

    private void EnqueueDepthAtBackSpec(
        string roleKey,
        string preserveFocusArg = DefaultDepthFocusPresetToken,
        string durationToken = DefaultDepthFocusDurationToken)
    {
        EnqueueDepthAtPresetSpec(
            roleKey,
            "back",
            preserveFocusArg,
            durationToken);
    }

    private void EnqueueDepthAtFarSpec(
        string roleKey,
        string preserveFocusArg = DefaultDepthFocusPresetToken,
        string durationToken = DefaultDepthFocusDurationToken)
    {
        EnqueueDepthAtPresetSpec(
            roleKey,
            "far",
            preserveFocusArg,
            durationToken);
    }

    private void EnqueueDepthAtPresetSpec(
        string roleKey,
        string depthArg,
        string preserveFocusArg,
        string durationToken)
    {
        float duration = YarnDurationParser.Parse(durationToken);

        EnqueueDepthAtPresetSpec(
            roleKey,
            depthArg,
            preserveFocusArg,
            duration);
    }

    private void EnqueueDepthAtPresetSpec(
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

            wait = false
        };

        ApplyDepthArg(spec, depthArg);
        ApplyPreserveFocusArg(spec, preserveFocusArg);

        Collect(spec);
    }

    private static void ApplyDepthArg(SetDepthCommandSpecCharR spec, string depthArg)
    {
        if (!CharacterDepthPresetParser.TryParse(depthArg, out CharacterDepthKey preset))
        {
            Debug.LogWarning(
                $"[YarnCommandBridge] Unknown depth preset '{depthArg}'. " +
                $"Fallback to '{CharacterDepthKey.Mid}'.");

            preset = CharacterDepthKey.Mid;
        }

        spec.preset = preset;
    }

    private static void ApplyPreserveFocusArg(
        SetDepthCommandSpecCharR spec,
        string preserveFocusArg)
    {
        if (string.IsNullOrWhiteSpace(preserveFocusArg))
        {
            spec.focusPreset = CharacterFocusPreset.Bust;
            return;
        }

        if (CharacterFocusPresetParser.TryParse(
                preserveFocusArg,
                out CharacterFocusPreset focusPreset))
        {
            spec.focusPreset = focusPreset;
            return;
        }

        Debug.LogWarning(
            $"[YarnCommandBridge] Unknown preserve focus preset '{preserveFocusArg}'. " +
            $"Fallback to '{CharacterFocusPreset.Bust}'.");

        spec.focusPreset = CharacterFocusPreset.Bust;
    }
}