using UnityEngine;

public sealed class PresentationResolver
{
    private readonly PresentationUIRoot _root;

    public PresentationResolver(PresentationUIRoot root)
    {
        _root = root;
    }

    public RectTransform ResolveRect(PresentationTarget target)
    {
        if (_root == null)
            return null;

        switch (target)
        {
            case PresentationTarget.Stage00_Root:
                return _root.ResolveRect(PresentationUIRoot.Refs.Stage00_Root);

            case PresentationTarget.Stage01_Root:
                return _root.ResolveRect(PresentationUIRoot.Refs.Stage01_Root);

            case PresentationTarget.Stage02_Root:
                return _root.ResolveRect(PresentationUIRoot.Refs.Stage02_Root);

            case PresentationTarget.StagePan_Root:
                return _root.ResolveRect(PresentationUIRoot.Refs.StagePan_Root);

            case PresentationTarget.StageZoom_Root:
                return _root.ResolveRect(PresentationUIRoot.Refs.StageZoom_Root);

            case PresentationTarget.LightSweep:
                return _root.ResolveRect(PresentationUIRoot.Refs.LightSweep);

            default:
                Debug.LogWarning($"[PresentationResolver] Unsupported target: {target}");
                return null;
        }
    }
}