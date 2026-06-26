using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class OverlayRigRegistry
{
    private readonly Dictionary<string, OverlayRigRefs> _overlays =
        new(StringComparer.Ordinal);

    public void Register(string key, OverlayRigRefs refs)
    {
        key = (key ?? string.Empty).Trim();

        if (string.IsNullOrEmpty(key))
        {
            Debug.LogWarning("[OverlayRigRegistry] Register failed. key is empty.");
            return;
        }

        if (refs == null)
        {
            Debug.LogWarning($"[OverlayRigRegistry] Register failed. refs is null. key='{key}'.");
            return;
        }

        if (_overlays.TryGetValue(key, out OverlayRigRefs existing))
            DestroyOverlay(existing);

        _overlays[key] = refs;
    }

    public bool Unregister(string key)
    {
        key = (key ?? string.Empty).Trim();

        if (!_overlays.Remove(key, out OverlayRigRefs refs))
        {
            Debug.LogWarning($"[OverlayRigRegistry] Unregister failed. Overlay not found. key='{key}'.");
            return false;
        }

        DestroyOverlay(refs);
        return true;
    }

    public bool Has(string key)
    {
        key = (key ?? string.Empty).Trim();

        return _overlays.TryGetValue(key, out OverlayRigRefs refs)
               && refs?.RigRoot != null;
    }

    public bool TryGet(string key, out OverlayRigRefs refs)
    {
        key = (key ?? string.Empty).Trim();

        if (!_overlays.TryGetValue(key, out refs))
        {
            Debug.LogWarning($"[OverlayRigRegistry] Overlay not found. key='{key}'.");
            return false;
        }

        if (refs?.RigRoot == null)
        {
            Debug.LogWarning($"[OverlayRigRegistry] Overlay is registered but invalid or destroyed. key='{key}'.");
            refs = null;
            return false;
        }

        return true;
    }

    public bool TryPeek(string key, out OverlayRigRefs refs)
    {
        key = (key ?? string.Empty).Trim();
        return _overlays.TryGetValue(key, out refs);
    }

    public void CollectAlive(List<OverlayRigRefs> results)
    {
        if (results == null)
            return;

        foreach (OverlayRigRefs refs in _overlays.Values)
        {
            if (refs == null)
                continue;

            if (refs.RigRoot == null)
                continue;

            results.Add(refs);
        }
    }

    public void Clear()
    {
        foreach (OverlayRigRefs refs in _overlays.Values)
            DestroyOverlay(refs);

        _overlays.Clear();
    }

    private static void DestroyOverlay(OverlayRigRefs refs)
    {
        if (refs?.RigRoot == null)
            return;

        refs.KillAllTweens(false);
        Object.Destroy(refs.RigRoot.gameObject);
    }
}
