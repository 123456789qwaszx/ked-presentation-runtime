using UnityEngine;

public sealed partial class YarnCommandBridge
{
    private void EnqueueCharVisualPresetSpec(
        string roleKey,
        string presetKey,
        float intensity = 1f,
        string durationToken = "6fr")
    {
        var spec = new CharVisualFocusCommandSpecCharR
        {
            slotKey = roleKey.Trim(),
            presetKey = CharacterVisualFocusPresetDBSO.NormalizeKey(presetKey),
            intensity = Mathf.Clamp01(intensity),
            duration = YarnDurationParser.Parse(durationToken, 0.25f),
            wait = false
        };

        Collect(spec);
    }

    private void EnqueueCharFocusSpec(
        string roleKey,
        float intensity = 1f,
        string durationToken = "10fr")
    {
        var spec = new CharVisualFocusCommandSpecCharR
        {
            slotKey = roleKey.Trim(),
            presetKey = "focus",
            intensity = Mathf.Clamp01(intensity),
            duration = YarnDurationParser.Parse(durationToken, 0.4f),
            wait = false
        };

        Collect(spec);
    }

    private void EnqueueCharDefocusSpec(
        string roleKey,
        float intensity = 1f,
        string durationToken = "17fr")
    {
        var spec = new CharVisualFocusCommandSpecCharR
        {
            slotKey = roleKey.Trim(),
            presetKey = "defocus",
            intensity = Mathf.Clamp01(intensity),
            duration = YarnDurationParser.Parse(durationToken, 0.7f),
            wait = false
        };

        Collect(spec);
    }

    private void EnqueueCharClearFocusSpec(
        string roleKey,
        string durationToken = "6fr")
    {
        var spec = new CharVisualFocusCommandSpecCharR
        {
            slotKey = roleKey.Trim(),
            presetKey = "clear",
            intensity = 1f,
            duration = YarnDurationParser.Parse(durationToken, 0.25f),
            wait = false
        };

        Collect(spec);
    }

    private void EnqueueCharDimSpec(
        string roleKey,
        float intensity = 1f,
        string durationToken = "6fr")
    {
        var spec = new CharVisualFocusCommandSpecCharR
        {
            slotKey = roleKey.Trim(),
            presetKey = "dim",
            intensity = Mathf.Clamp01(intensity),
            duration = YarnDurationParser.Parse(durationToken, 0.25f),
            wait = false
        };

        Collect(spec);
    }

    private void EnqueueCharSilhouetteSpec(
        string roleKey,
        float intensity = 1f,
        string durationToken = "6fr")
    {
        var spec = new CharVisualFocusCommandSpecCharR
        {
            slotKey = roleKey.Trim(),
            presetKey = "silhouette",
            intensity = Mathf.Clamp01(intensity),
            duration = YarnDurationParser.Parse(durationToken, 0.25f),
            wait = false
        };

        Collect(spec);
    }

    private void EnqueueCharInnerRimSpec(
        string roleKey,
        float intensity = 1f,
        string durationToken = "6fr")
    {
        var spec = new CharVisualFocusCommandSpecCharR
        {
            slotKey = roleKey.Trim(),
            presetKey = "inner_rim",
            intensity = Mathf.Clamp01(intensity),
            duration = YarnDurationParser.Parse(durationToken, 0.25f),
            wait = false
        };

        Collect(spec);
    }

    private void EnqueueCharOuterRimSpec(
        string roleKey,
        float intensity = 1f,
        string durationToken = "6fr")
    {
        var spec = new CharVisualFocusCommandSpecCharR
        {
            slotKey = roleKey.Trim(),
            presetKey = "outer_rim",
            intensity = Mathf.Clamp01(intensity),
            duration = YarnDurationParser.Parse(durationToken, 0.25f),
            wait = false
        };

        Collect(spec);
    }
}