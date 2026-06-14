public sealed class StageDepthLayerBinder
{
    private IShotResponseStageProvider _provider;

    private readonly PresentationResponseProfile _far   = PresentationResponseProfile.DepthFar;
    private readonly PresentationResponseProfile _slot1 = PresentationResponseProfile.DepthBack;
    private readonly PresentationResponseProfile _slot2 = PresentationResponseProfile.DepthMid;
    private readonly PresentationResponseProfile _slot3 = PresentationResponseProfile.DepthFront;
    private readonly PresentationResponseProfile _close = PresentationResponseProfile.DepthClose;

    public void EnsureBindings(PresentationShotResponseSystem rig)
    {
        if (!TryEnsureProvider())
            return;

        Bind(rig, StageDepthLayer.Far,   _far);
        Bind(rig, StageDepthLayer.Slot1, _slot1);
        Bind(rig, StageDepthLayer.Slot2, _slot2);
        Bind(rig, StageDepthLayer.Slot3, _slot3);
        Bind(rig, StageDepthLayer.Close, _close);
    }

    private void Bind(
        PresentationShotResponseSystem rig,
        StageDepthLayer layer,
        PresentationResponseProfile profile)
    {
        string key = layer.ToString();

        // 이미 살아있는 binding이 있으면 profile만 갱신하고 종료.
        if (rig.TryUpdateBindingProfile(key, profile))
            return;

        StageDepthLayerRects rects = _provider.GetLayerRects(layer);

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