public sealed class StageDepthLayerBinder
{
    private IShotResponseStageProvider _provider;

    private readonly PresentationResponseProfile _far = PresentationResponseProfile.DepthFar;
    private readonly PresentationResponseProfile _back = PresentationResponseProfile.DepthBack;
    private readonly PresentationResponseProfile _mid = PresentationResponseProfile.DepthMid;
    private readonly PresentationResponseProfile _front = PresentationResponseProfile.DepthFront;
    private readonly PresentationResponseProfile _close = PresentationResponseProfile.DepthClose;

    public void EnsureBindings(PresentationShotResponseSystem rig)
    {
        if (!TryEnsureProvider())
            return;

        BindStage(rig, PresentationStageKey.Stage00);
        BindStage(rig, PresentationStageKey.Stage01);
        BindStage(rig, PresentationStageKey.Stage02);
    }

    private void BindStage(
        PresentationShotResponseSystem rig,
        PresentationStageKey root)
    {
        Bind(rig, root, PresentationDepthLayerKey.Far, _far);
        Bind(rig, root, PresentationDepthLayerKey.Back, _back);
        Bind(rig, root, PresentationDepthLayerKey.Mid, _mid);
        Bind(rig, root, PresentationDepthLayerKey.Front, _front);
        Bind(rig, root, PresentationDepthLayerKey.Close, _close);
    }

    private void Bind(
        PresentationShotResponseSystem rig,
        PresentationStageKey root,
        PresentationDepthLayerKey layer,
        PresentationResponseProfile profile)
    {
        string key = $"{root}/{layer}";

        if (rig.TryUpdateBindingProfile(key, profile))
            return;

        StageDepthLayerRects rects = _provider.GetLayerRects(root, layer);

        var target = new StageDepthResponseTarget(
            rects.Measure,
            rects.Position,
            rects.Scale);

        rig.RegisterRuntimeBinding(
            key,
            target,
            profile);
    }

    private bool TryEnsureProvider()
    {
        if (_provider != null)
            return true;

        _provider = UIManager.Instance.GetUI<PresentationUIRoot>();

        return _provider != null;
    }
}