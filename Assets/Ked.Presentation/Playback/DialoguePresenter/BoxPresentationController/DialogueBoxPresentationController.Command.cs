using System.Threading;
using Yarn.Unity;

public partial class DialogueBoxPresentationController
{
    // box_hide·box_show 페이드의 수명. 다음 수동 전환·줄 전환(ShowLineAsync)·CloseAll·CleanupStale이 끊는다.
    private CancellationTokenSource _visibilityTransitionCts;

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
        CancellationToken token = BeginVisibilityTransition();

        if (_boxState.Box == null || !_boxState.IsVisible)
        {
            _box.SetVisibleImmediate(false);
            _boxState.MarkHidden();
            return;
        }

        await _box.FadeOutAsync(
            FadeDownDuration,
            token);

        if (token.IsCancellationRequested)
            return;

        _box.SetVisibleImmediate(false);
        _boxState.MarkHidden();
    }

    public async YarnTask ShowCurrentAsync()
    {
        CancellationToken token = BeginVisibilityTransition();

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
            token);

        if (token.IsCancellationRequested)
            return;

        _box.SetVisibleImmediate(true);
        _boxState.TryMarkVisible();
    }

    private CancellationToken BeginVisibilityTransition()
    {
        InvalidateVisibilityTransition();

        _visibilityTransitionCts = new CancellationTokenSource();
        return _visibilityTransitionCts.Token;
    }

    private void InvalidateVisibilityTransition()
    {
        if (_visibilityTransitionCts == null)
            return;

        _visibilityTransitionCts.Cancel();
        _visibilityTransitionCts.Dispose();
        _visibilityTransitionCts = null;
    }
}