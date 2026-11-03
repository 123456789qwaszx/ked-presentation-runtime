using System;
using Yarn.Unity;

public sealed class OptionSelectionSession : IDisposable
{
    private readonly YarnTaskCompletionSource<OptionViewModel> _source = new();
    private readonly LineCancellationToken _lineToken;

    private bool _disposed;

    public YarnTask<OptionViewModel> Task => _source.Task;

    public OptionSelectionSession(LineCancellationToken lineToken)
    {
        _lineToken = lineToken;
        WatchCancellationAsync().Forget();
    }

    public bool TrySubmit(OptionViewModel viewModel)
    {
        if (_disposed)
            return false;

        return _source.TrySetResult(viewModel);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _source.TrySetResult(null);
    }

    private async YarnTask WatchCancellationAsync()
    {
        while (!_disposed)
        {
            if (_lineToken.IsNextContentRequested)
            {
                _source.TrySetResult(null);
                return;
            }

            await YarnTask.Yield();
        }
    }
}