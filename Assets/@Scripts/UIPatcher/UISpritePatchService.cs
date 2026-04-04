using System.Collections;
using UnityEngine;

public sealed class UISpritePatchService
{
    private readonly SpritePatchResolver _resolver;
    private readonly UISpritePatcher _patcher;

    public UISpritePatchService(SpritePatchResolver resolver, UISpritePatcher patcher)
    {
        _resolver = resolver;
        _patcher = patcher;
    }

    public IEnumerator ApplyInHierarchyIfSupported(Component root, UIContext ctx)
    {
        if (root == null)
            yield break;

        var providers = root.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);

        for (int i = 0; i < providers.Length; i++)
        {
            if (providers[i] is not IUISpritePortProvider provider)
                continue;

            var bindings = _resolver.BuildBindings(provider, ctx);
            if (bindings == null || bindings.Count == 0)
                continue;

            yield return _patcher.Apply(provider, bindings);
        }
    }
}