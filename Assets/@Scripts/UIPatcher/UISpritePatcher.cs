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

    public IEnumerator Apply(IUISpritePortProvider ui, List<SpritePortAssignment> bindings)
    {
        if (ui == null || bindings == null || bindings.Count == 0)
            yield break;

        for (int i = 0; i < bindings.Count; i++)
        {
            var binding = bindings[i];

            if (!_loader.TryGetCached(binding.imageAddress, out Sprite sprite))
                continue;

            ui.TrySetSprite(binding.portId, sprite);
        }

        yield break;
    }
}