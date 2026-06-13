public sealed class StageDepthLayerBinder
{
    private IStageDepthLayerProvider _provider;

    private PresentationResponseProfile _far   = PresentationResponseProfile.DepthFar;
    private PresentationResponseProfile _slot1 = PresentationResponseProfile.DepthBack;
    private PresentationResponseProfile _slot2 = PresentationResponseProfile.DepthMid;
    private PresentationResponseProfile _slot3 = PresentationResponseProfile.DepthFront;
    private PresentationResponseProfile _close = PresentationResponseProfile.DepthClose;

    public void ConfigureProfiles(
        PresentationResponseProfile far,
        PresentationResponseProfile slot1,
        PresentationResponseProfile slot2,
        PresentationResponseProfile slot3,
        PresentationResponseProfile close)
    {
        if (far   != null) _far   = far;
        if (slot1 != null) _slot1 = slot1;
        if (slot2 != null) _slot2 = slot2;
        if (slot3 != null) _slot3 = slot3;
        if (close != null) _close = close;
    }

    public void EnsureBindings(PresentationResponseRig rig)
    {
        if (rig == null)
            return;

        if (!TryEnsureProvider())
            return;

        Bind(rig, StageDepthLayer.Far,   _far);
        Bind(rig, StageDepthLayer.Slot1, _slot1);
        Bind(rig, StageDepthLayer.Slot2, _slot2);
        Bind(rig, StageDepthLayer.Slot3, _slot3);
        Bind(rig, StageDepthLayer.Close, _close);
    }

    private void Bind(
        PresentationResponseRig rig,
        StageDepthLayer layer,
        PresentationResponseProfile profile)
    {
        if (profile == null)
            return;

        string key = ResponseBindingKeys.StageDepthLayer(layer);

        // 이미 살아있는 binding이 있으면 profile만 갱신하고 종료.
        if (rig.TryUpdateBindingProfile(key, profile))
            return;

        if (!_provider.TryGetLayerRects(layer, out var measure, out var position, out var scale))
            return;

        var target = new StageDepthResponseTarget(measure, position, scale);

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