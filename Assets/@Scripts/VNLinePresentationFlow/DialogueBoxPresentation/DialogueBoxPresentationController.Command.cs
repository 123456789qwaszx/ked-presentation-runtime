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
    
    public void SetProtagonistLineBoxKind(DialogueBoxKind kind) => _protagonistLineBoxKind = kind;
    public void SetNamedLineBoxKind(DialogueBoxKind kind) => _namedLineBoxKind = kind;
    
    public void ResetDefaultLineBoxKinds()
    {
        _protagonistLineBoxKind = DefaultProtagonistLineBoxKind;
        _namedLineBoxKind = DefaultNamedLineBoxKind;
    }
    
    public async YarnTask HideCurrentAsync()
    {
        VisibilityTransitionRun visibilityRun = BeginVisibilityTransition();

        IPresentationDialogueBoxView currentBox = _boxState.Box;

        if (currentBox == null)
        {
            _host.HideAllDialogueBoxes();
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

        _host.HideAllDialogueBoxesExcept(currentBox);

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

        _host.HideAllDialogueBoxesExcept(currentBox);
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
