using System.Threading;
using Yarn.Unity;

public partial class DialogueBoxPresentationController
{
    private int _visibilityTransitionEpoch;
    private CancellationTokenSource _visibilityTransitionCts;

    private readonly struct VisibilityTransitionRun
    {
        public readonly int Epoch;
        public readonly CancellationToken Token;

        public VisibilityTransitionRun(
            int epoch,
            CancellationToken token)
        {
            Epoch = epoch;
            Token = token;
        }
    }

    // surface_layout is a persistent presentation state mutation.
    // It is intentionally not applied to the currently committed box here,
    // because front-matter commands for the next line may run while the previous
    // line is still visible. The layout is applied deterministically at ShowLineAsync,
    // before PrimeText.
    public void SetSurfaceLayout(string presetKey)
    {
        _surfaceState.SetLayout(presetKey);
    }

    public void ResetSurfaceLayout()
    {
        _surfaceState.Reset();
    }

    public async YarnTask HideCurrentAsync()
    {
        VisibilityTransitionRun visibilityRun = BeginVisibilityTransition();

        if (_boxState.Box == null || !_boxState.IsVisible)
        {
            _box.SetVisibleImmediate(false);
            _boxState.MarkHidden();
            return;
        }

        await _box.FadeOutAsync(
            FadeDownDuration,
            visibilityRun.Token);

        if (!IsCurrentVisibilityTransition(visibilityRun))
            return;

        _box.SetVisibleImmediate(false);
        _boxState.MarkHidden();
    }

    public async YarnTask ShowCurrentAsync()
    {
        VisibilityTransitionRun visibilityRun = BeginVisibilityTransition();

        if (_boxState.Box == null)
            return;

        if (_boxState.IsVisible)
        {
            _box.SetVisibleImmediate(true);
            _boxState.TryMarkVisible();
            return;
        }

        _box.PrepareHidden();

        await _box.FadeInAsync(
            FadeUpDuration,
            visibilityRun.Token);

        if (!IsCurrentVisibilityTransition(visibilityRun))
            return;

        _box.SetVisibleImmediate(true);
        _boxState.TryMarkVisible();
    }

    private VisibilityTransitionRun BeginVisibilityTransition()
    {
        InvalidateVisibilityTransition();

        _visibilityTransitionCts = new CancellationTokenSource();

        return new VisibilityTransitionRun(
            _visibilityTransitionEpoch,
            _visibilityTransitionCts.Token);
    }

    private void InvalidateVisibilityTransition()
    {
        _visibilityTransitionEpoch++;

        if (_visibilityTransitionCts == null)
            return;

        _visibilityTransitionCts.Cancel();
        _visibilityTransitionCts = null;
    }

    private bool IsCurrentVisibilityTransition(VisibilityTransitionRun visibilityRun)
    {
        return _visibilityTransitionEpoch == visibilityRun.Epoch &&
               !visibilityRun.Token.IsCancellationRequested;
    }
}