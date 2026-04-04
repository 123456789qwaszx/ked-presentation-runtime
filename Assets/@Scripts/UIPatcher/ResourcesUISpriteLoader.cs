using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class ResourcesUISpriteLoader : IUISpriteLoader
{
    private readonly Dictionary<string, Sprite> _runtimeSpriteCache = new();

    public bool TryGetCached(string address, out Sprite sprite)
    {
        if (_runtimeSpriteCache.TryGetValue(address, out sprite) && sprite != null)
            return true;

        sprite = Resources.Load<Sprite>(address);
        if (sprite != null)
        {
            _runtimeSpriteCache[address] = sprite;
            return true;
        }
        
        Texture2D texture = Resources.Load<Texture2D>(address);
        if (texture != null)
        {
            Debug.LogWarning(
                $"[ResourcesUISpriteLoader] '{address}' is Texture2D, not Sprite. Set Import Type to Sprite (2D and UI)."
            );
        }

        return false;
    }

    public IEnumerator Load(string address, Action<Sprite> onLoaded, Action onFailed = null)
    {
        yield return null;
    }
}