using System;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

// Stage slot selection
public enum CharAnchorPreset
{
    None = 0,
    Left = 1,
    Center = 2,
    Right = 3,
    BoxSide = 4,
    
    Exp1 = 100,
    Exp2 = 101
}

// Per-slot offset container
[Serializable]
public struct AnchorOffset3
{
    public Vector2 left;
    public Vector2 center;
    public Vector2 right;
    public Vector2 talk;
    
    public Vector2 exp1;
    public Vector2 exp2;

    public Vector2 Get(CharAnchorPreset preset) => preset switch
    {
        CharAnchorPreset.None   => Vector2.zero,
        CharAnchorPreset.Left   => left,
        CharAnchorPreset.Center => center,
        CharAnchorPreset.Right  => right,
        CharAnchorPreset.BoxSide   => talk,
        
        CharAnchorPreset.Exp1   => exp1,
        CharAnchorPreset.Exp2   => exp2,
        _                             => Vector2.zero,
    };
}

// Final composition (resolution) logic
public static class CharAnchorPlacementResolver
{
    private static float PresetToRatioX(CharAnchorPreset preset, float baseRatioX) => preset switch
    {
        CharAnchorPreset.None   => 0f,
        CharAnchorPreset.Left   => -baseRatioX,
        CharAnchorPreset.Center => 0f,
        CharAnchorPreset.Right  => +baseRatioX,
        CharAnchorPreset.BoxSide   => 0f,
        
        CharAnchorPreset.Exp1   => 0f,
        CharAnchorPreset.Exp2   => 0f,
        _                             => 0f,
    };

    public static Vector2 ResolveAnchoredPosition(
        RectTransform anchorRect,
        CharAnchorPreset preset,
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