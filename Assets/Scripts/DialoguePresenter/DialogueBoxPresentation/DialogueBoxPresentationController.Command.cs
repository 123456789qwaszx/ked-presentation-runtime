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

    public void ApplyCurrentSurfaceLayoutToCommittedBox()
    {
        if (_boxState.Box == null)
            return;

        ApplySurfaceLayoutFor(
            _boxState.Box,
            _boxState.BoxKind ?? DefaultLineBoxKind);
    }

    public async YarnTask HideCurrentAsync()
    {
        VisibilityTransitionRun visibilityRun = BeginVisibilityTransition();

        IPresentationDialogueBoxView currentBox = _boxState.Box;

        if (currentBox == null)
        {
            _box.SetVisibleImmediate(false);
            _boxState.MarkHidden();
            return;
        }

        if (!_boxState.IsVisible)
        {
            currentBox.SetVisibleImmediate(false);
            _boxState.MarkHidden();
            return;
        }

        await currentBox.FadeOutAsync(
            _fadeDownDuration,
            visibilityRun.Token);

        if (!IsCurrentVisibilityTransition(visibilityRun))
            return;

        if (!ReferenceEquals(_boxState.Box, currentBox))
        {
            currentBox.SetVisibleImmediate(false);

            if (_boxState.IsVisible && _boxState.Box != null)
                _boxState.Box.SetVisibleImmediate(true);

            return;
        }

        currentBox.SetVisibleImmediate(false);
        _boxState.MarkHidden();
    }

    public async YarnTask ShowCurrentAsync()
    {
        VisibilityTransitionRun visibilityRun = BeginVisibilityTransition();

        IPresentationDialogueBoxView currentBox = _boxState.Box;

        if (currentBox == null)
            return;


        if (_boxState.IsVisible)
        {
            currentBox.SetVisibleImmediate(true);
            _boxState.TryMarkVisible();
            return;
        }

        currentBox.PrepareHidden();

        await currentBox.FadeInAsync(
            _fadeUpDuration,
            visibilityRun.Token);

        if (!IsCurrentVisibilityTransition(visibilityRun))
            return;

        if (!ReferenceEquals(_boxState.Box, currentBox))
        {
            currentBox.SetVisibleImmediate(false);

            if (_boxState.IsVisible && _boxState.Box != null)
                _boxState.Box.SetVisibleImmediate(true);

            return;
        }

        currentBox.SetVisibleImmediate(true);
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
