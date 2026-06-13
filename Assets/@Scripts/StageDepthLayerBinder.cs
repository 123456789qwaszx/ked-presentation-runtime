// depth 레이어는 캐릭터/배경처럼 생성·소멸하는 대상이 아니라 무대 고정 인프라.
// 현재는 ResetVisualState() → PresentationResponseRig.Clear()가 binding 장부를 비우는 중.
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
        if (!TryEnsureProvider())
            return;

        Bind(rig, StageDepthLayer.Far,   _far);
        Bind(rig, StageDepthLayer.Slot1, _slot1);
        Bind(rig, StageDepthLayer.Slot2, _slot2);
        Bind(rig, StageDepthLayer.Slot3, _slot3);
        Bind(rig, StageDepthLayer.Close, _close);
    }

    private void Bind(PresentationResponseRig rig, StageDepthLayer layer, PresentationResponseProfile profile)
    {
        if (!_provider.TryGetLayerRects(layer, out var measure, out var position, out var scale))
            return;

        var target = new StageDepthResponseTarget(measure, position, scale);

        rig.RegisterRuntimeBinding(
            ResponseBindingKeys.StageDepthLayer(layer),
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