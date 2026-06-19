using Yarn.Unity;

public sealed class VNOptionsBoxPresentationController
{
    private readonly DialogueBoxHost _dialogueBoxHost;
    
    private OptionsBoxKind _defaultKind = OptionsBoxKind.Default;
    private float _fadeDuration = 0.12f;

    private IPresentationOptionsBoxView _currentView;

    public VNOptionsBoxPresentationController(DialogueBoxHost host)
    {
        _dialogueBoxHost = host;
    }

    public async YarnTask<VNOptionsBoxPresentationResult> ShowOptionsAsync(
        VNOptionsBoxPresentationOptions options)
    {
        IPresentationOptionsBoxView nextView = _dialogueBoxHost.ResolveOptionsTarget(_defaultKind);

        _dialogueBoxHost.HideAllOptionsBoxesExcept(nextView);

        _currentView = nextView;
        _currentView.ResetPresentationTransform();
        _currentView.PrepareHidden();
        _currentView.SetInputEnabled(false);

        if (options != null && options.UseImmediateTransition)
        {
            _currentView.SetVisibleImmediate(true);
            _currentView.SetInputEnabled(false);
        }
        else
        {
            await _currentView.FadeInAsync(_fadeDuration, default);
        }

        return new VNOptionsBoxPresentationResult(_currentView);
    }

    public void CleanupAborted(VNOptionsBoxPresentationResult result)
    {
        if (result == null || result.View == null)
            return;

        result.View.SetInputEnabled(false);
        result.View.SetVisibleImmediate(false);

        if (ReferenceEquals(_currentView, result.View))
            _currentView = null;
    }

    public void HideImmediate()
    {
        if (_currentView != null)
        {
            _currentView.SetInputEnabled(false);
            _currentView.SetVisibleImmediate(false);
        }

        _currentView = null;
    }
}