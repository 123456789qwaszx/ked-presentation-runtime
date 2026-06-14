using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    private const string DefaultDepthFocusPresetToken = "bust";
    private const string DefaultDepthFocusDurationToken = "10fr";

    private void RegisterDepthFocusCommands(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string, string>(
            "at_close", EnqueueDepthAtCloseSpec);

        runner.AddCommandHandler<string, string, string>(
            "at_front", EnqueueDepthAtFrontSpec);

        runner.AddCommandHandler<string, string, string>(
            "at_mid", EnqueueDepthAtMidSpec);

        runner.AddCommandHandler<string, string, string>(
            "at_back", EnqueueDepthAtBackSpec);

        runner.AddCommandHandler<string, string, string>(
            "at_far", EnqueueDepthAtFarSpec);
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

            overridePreserveFocus = true,
            wait = false
        };

        ApplyDepthArg(spec, depthArg);
        ApplyPreserveFocusArg(spec, preserveFocusArg);

        Collect(spec);
    }
}