using UnityEngine;

public sealed partial class YarnCommandBridge
{
    private const string DefaultDepthFocusPresetToken = "bust";
    private const string DefaultDepthFocusDurationToken = "10fr";
    
    private void EnqueueDepthAtPresetSpec(
        string roleKey,
        string depthArg,
        string preserveFocusArg,
        float duration)
    {
        var spec = new SetDepthCommandSpecCharR
        {
            slotKey = roleKey,
            duration = duration,
        };

        if (!CharacterDepthPresetParser.TryParse(depthArg, out CharacterDepthKey preset))
        {
            Debug.LogWarning(
                $"[YarnCommandBridge] Unknown depth preset '{depthArg}'. " +
                $"Fallback to '{CharacterDepthKey.Mid}'.");

            preset = CharacterDepthKey.Mid;
        }
        
        spec.preset = preset;
        
        if (!CharacterFocusPresetParser.TryParse(preserveFocusArg, out CharacterFocusPreset focusPreset))
        {
            Debug.LogWarning(
                $"[YarnCommandBridge] Unknown preserve focus preset '{preserveFocusArg}'. " +
                $"Fallback to '{CharacterFocusPreset.Bust}'.");
            
            focusPreset = CharacterFocusPreset.Bust;
        }
        
        spec.focusPreset = focusPreset;

        Collect(spec);
    }
    
    private void EnqueueDepthAtSpec(
        string roleKey,
        string depthArg,
        string preserveFocusArg = DefaultDepthFocusPresetToken,
        string durationToken = DefaultDepthFocusDurationToken)
        => EnqueueDepthAtPresetSpec(roleKey, depthArg, preserveFocusArg, durationToken);

    private void EnqueueDepthAtCloseSpec(
        string roleKey,
        string preserveFocusArg = DefaultDepthFocusPresetToken,
        string durationToken = DefaultDepthFocusDurationToken)
        => EnqueueDepthAtPresetSpec(roleKey, "close", preserveFocusArg, durationToken);

    private void EnqueueDepthAtFrontSpec(
        string roleKey,
        string preserveFocusArg = DefaultDepthFocusPresetToken,
        string durationToken = DefaultDepthFocusDurationToken)
        => EnqueueDepthAtPresetSpec(roleKey, "front", preserveFocusArg, durationToken);

    private void EnqueueDepthAtMidSpec(
        string roleKey,
        string preserveFocusArg = DefaultDepthFocusPresetToken,
        string durationToken = DefaultDepthFocusDurationToken)
        => EnqueueDepthAtPresetSpec(roleKey, "mid", preserveFocusArg, durationToken);

    private void EnqueueDepthAtBackSpec(
        string roleKey,
        string preserveFocusArg = DefaultDepthFocusPresetToken,
        string durationToken = DefaultDepthFocusDurationToken)
        => EnqueueDepthAtPresetSpec(roleKey, "back", preserveFocusArg, durationToken);

    private void EnqueueDepthAtFarSpec(
        string roleKey,
        string preserveFocusArg = DefaultDepthFocusPresetToken,
        string durationToken = DefaultDepthFocusDurationToken)
        => EnqueueDepthAtPresetSpec(roleKey, "far", preserveFocusArg, durationToken);

    private void EnqueueDepthAtPresetSpec(
        string roleKey,
        string depthArg,
        string preserveFocusArg,
        string durationToken)
        => EnqueueDepthAtPresetSpec(roleKey, depthArg, preserveFocusArg, YarnDurationParser.Parse(durationToken));
}