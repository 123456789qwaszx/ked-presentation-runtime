using System;
using System.Collections;
using UnityEngine;

public sealed class ResourcesUISpriteLoader : IUISpriteLoader
{
    public bool TryGetCached(string address, out Sprite sprite)
    {
        sprite = Resources.Load<Sprite>(address);
        return sprite != null;
    }

    public IEnumerator Load(string address, Action<Sprite> onLoaded, Action onFailed = null)
    {
        yield return null;
    }
}