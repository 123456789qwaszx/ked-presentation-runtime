using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IUISpriteLoader
{
    bool TryGetCached(string address, out Sprite sprite);
    IEnumerator Load(string address, Action<Sprite> onLoaded, Action onFailed = null);
}

public sealed class UISpritePatcher
{
    private readonly IUISpriteLoader _loader;

    public UISpritePatcher(IUISpriteLoader loader)
    {
        _loader = loader;
    }

    public IEnumerator Apply(IUISpritePortProvider targetUI, List<SpritePortAssignment> patches)
    {
        for (int i = 0; i < patches.Count; i++)
        {
            SpritePortAssignment patch = patches[i];

            if (_loader.TryGetCached(patch.spriteAddress, out Sprite cachedSprite))
            {
                Debug.Log(patch.spriteAddress);
                targetUI.TrySetSprite(patch.portId, cachedSprite);
                continue;
            }
        }

        yield break;
    }
}