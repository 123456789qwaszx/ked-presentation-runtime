using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class SpriteImageRegistry
{
    private readonly Dictionary<string, SpriteImageRigRefs> _sprites =
        new(StringComparer.Ordinal);

    public void Register(string key, SpriteImageRigRefs refs)
    {
        key = (key ?? string.Empty).Trim();

        if (string.IsNullOrEmpty(key))
        {
            Debug.LogWarning("[SpriteImageRegistry] Register failed. key is empty.");
            return;
        }

        if (refs == null)
        {
            Debug.LogWarning($"[SpriteImageRegistry] Register failed. refs is null. key='{key}'.");
            return;
        }

        if (_sprites.TryGetValue(key, out SpriteImageRigRefs existing))
            DestroySprite(existing);

        _sprites[key] = refs;
    }

    public bool Unregister(string key)
    {
        key = (key ?? string.Empty).Trim();

        if (!_sprites.Remove(key, out SpriteImageRigRefs refs))
        {
            Debug.LogWarning($"[SpriteImageRegistry] Unregister failed. Sprite not found. key='{key}'.");
            return false;
        }

        DestroySprite(refs);
        return true;
    }

    public bool Has(string key)
    {
        key = (key ?? string.Empty).Trim();

        return _sprites.TryGetValue(key, out SpriteImageRigRefs refs)
               && refs?.RigRoot != null;
    }

    public bool TryGet(string key, out SpriteImageRigRefs refs)
    {
        key = (key ?? string.Empty).Trim();

        if (!_sprites.TryGetValue(key, out refs))
        {
            Debug.LogWarning($"[SpriteImageRegistry] Sprite not found. key='{key}'.");
            return false;
        }

        if (refs?.RigRoot == null)
        {
            Debug.LogWarning($"[SpriteImageRegistry] Sprite is registered but invalid or destroyed. key='{key}'.");
            refs = null;
            return false;
        }

        return true;
    }

    public bool TryPeek(string key, out SpriteImageRigRefs refs)
    {
        key = (key ?? string.Empty).Trim();
        return _sprites.TryGetValue(key, out refs);
    }

    public void CollectAlive(List<SpriteImageRigRefs> results)
    {
        if (results == null)
            return;

        foreach (SpriteImageRigRefs refs in _sprites.Values)
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
        foreach (SpriteImageRigRefs refs in _sprites.Values)
            DestroySprite(refs);

        _sprites.Clear();
    }

    private static void DestroySprite(SpriteImageRigRefs refs)
    {
        if (refs?.RigRoot == null)
            return;

        refs.KillAllTweens(false);
        Object.Destroy(refs.RigRoot.gameObject);
    }
}