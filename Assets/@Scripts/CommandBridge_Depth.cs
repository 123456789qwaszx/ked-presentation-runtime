using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    private const string DefaultDepthFocusDurationToken = "10fr";

    private void RegisterDepthFocusCommands(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string, string>(
            "at_face",
            EnqueueDepthFocusFaceSpec);

        runner.AddCommandHandler<string, string, string>(
            "at_bust",
            EnqueueDepthFocusBustSpec);

        runner.AddCommandHandler<string, string, string>(
            "at_body",
            EnqueueDepthFocusBodySpec);

        runner.AddCommandHandler<string, string, string>(
            "at_feet",
            EnqueueDepthFocusFeetSpec);
    }

    private void EnqueueDepthFocusFaceSpec(
        string roleKey,
        string depthArg,
        string durationToken = DefaultDepthFocusDurationToken)
    {
        EnqueueDepthFocusPresetSpec(
            roleKey,
            depthArg,
            CharacterFocusPreset.Face,
            durationToken);
    }

    private void EnqueueDepthFocusBustSpec(
        string roleKey,
        string depthArg,
        string durationToken = DefaultDepthFocusDurationToken)
    {
        EnqueueDepthFocusPresetSpec(
            roleKey,
            depthArg,
            CharacterFocusPreset.Bust,
            durationToken);
    }

    private void EnqueueDepthFocusBodySpec(
        string roleKey,
        string depthArg,
        string durationToken = DefaultDepthFocusDurationToken)
    {
        EnqueueDepthFocusPresetSpec(
            roleKey,
            depthArg,
            CharacterFocusPreset.Body,
            durationToken);
    }

    private void EnqueueDepthFocusFeetSpec(
        string roleKey,
        string depthArg,
        string durationToken = DefaultDepthFocusDurationToken)
    {
        EnqueueDepthFocusPresetSpec(
            roleKey,
            depthArg,
            CharacterFocusPreset.Feet,
            durationToken);
    }

    private void EnqueueDepthFocusPresetSpec(
        string roleKey,
        string depthArg,
        CharacterFocusPreset preserveFocusPreset,
        string durationToken)
    {
        float duration = YarnDurationParser.Parse(
            durationToken);

        EnqueueDepthFocusPresetSpec(
            roleKey,
            depthArg,
            preserveFocusPreset,
            duration);
    }

    private void EnqueueDepthFocusPresetSpec(
        string roleKey,
        string depthArg,
        CharacterFocusPreset preserveFocusPreset,
        float duration)
    {
        duration = Mathf.Max(0f, duration);

        var spec = new SetDepthCommandSpecCharR
        {
            slotKey = roleKey,
            duration = duration,

            overridePreserveFocus = true,
            preserveFocusPreset = preserveFocusPreset,
            preserveCustomFocusKey = "",

            wait = false
        };

        ApplyDepthArg(spec, depthArg);

        Collect(spec);
    }
}