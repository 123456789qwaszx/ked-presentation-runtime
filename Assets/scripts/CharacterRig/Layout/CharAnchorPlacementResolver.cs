using System;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

// Stage slot selection
public enum RectAnchorPreset3CharR
{
    Left,
    Center,
    Right,
}

// Per-slot offset container
[Serializable]
public struct AnchorOffset3
{
    public Vector2 left;
    public Vector2 center;
    public Vector2 right;

    public Vector2 Get(RectAnchorPreset3CharR preset) => preset switch
    {
        RectAnchorPreset3CharR.Left   => left,
        RectAnchorPreset3CharR.Center => center,
        RectAnchorPreset3CharR.Right  => right,
        _                             => center,
    };
}

// Final composition (resolution) logic
public static class CharAnchorPlacementResolver
{
    private static float PresetToRatioX(RectAnchorPreset3CharR preset, float baseRatioX) => preset switch
    {
        RectAnchorPreset3CharR.Left   => -baseRatioX,
        RectAnchorPreset3CharR.Center => 0f,
        RectAnchorPreset3CharR.Right  => +baseRatioX,
        _                             => 0f,
    };

    public static Vector2 ResolveAnchoredPosition(
        RectTransform anchorRect,
        RectAnchorPreset3CharR preset,
        float baseRatioX,
        CharStageTuningSO globalTuning,
        RoleAnchorTuningDBSO roleTuningDb,
        string roleKey,
        string poseKey,
        Vector2 commandOffset)
    {
        // 0) Build the lookup key
        string tuneKey = BuildTuneKey(roleKey, poseKey);

        // 1) Find the parent RectTransform
        RectTransform parentRect = null;
        if (anchorRect != null)
            parentRect = anchorRect.parent as RectTransform;

        // 2) Read the parent width in pixels
        float parentWidth = 0f;
        if (parentRect != null)
            parentWidth = parentRect.rect.width;

        // 3) Compute base slot X position from parent width and preset (Left/Center/Right), Y stays 0
        float baseX = parentWidth * PresetToRatioX(preset, baseRatioX);
        Vector2 pos = new Vector2(baseX, 0f);

        // 4) Apply global(stage) tuning offset
        if (globalTuning != null)
            pos += globalTuning.offsets.Get(preset);

        // 5) Apply role/pose-specific tuning offset
        if (roleTuningDb != null && roleTuningDb.TryGet(tuneKey, out var entry))
            pos += entry.offsets.Get(preset);

        // 6) Apply command-time offset (final per-command adjustment, applied last)
        pos += commandOffset;

        // 7) Return the final anchored position
        return pos;
    }

    private static string BuildTuneKey(string roleKey, string poseKey)
    {
        roleKey = roleKey ?? "";
        poseKey = poseKey ?? "";

        if (string.IsNullOrWhiteSpace(poseKey))
            return roleKey;

        // DB key rule: "role:pose"
        return $"{roleKey}:{poseKey.Trim()}";
    }
}