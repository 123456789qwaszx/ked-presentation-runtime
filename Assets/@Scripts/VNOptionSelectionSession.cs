using System;
using System.Threading;
using Yarn.Unity;

public sealed class VNOptionSelectionSession : IDisposable
{
    private readonly YarnTaskCompletionSource<VNOptionViewModel> _source = new();
    private readonly CancellationTokenSource _lifetimeCts = new();

    private readonly LineCancellationToken _lineToken;
    private bool _disposed;

    public YarnTask<VNOptionViewModel> Task => _source.Task;

    public VNOptionSelectionSession(LineCancellationToken lineToken)
    {
        _lineToken = lineToken;
        WatchCancellationAsync().Forget();
    }

    public bool TrySubmit(VNOptionViewModel viewModel)
    {
        if (_disposed)
            return false;

        if (_lifetimeCts.IsCancellationRequested)
            return false;

        if (viewModel == null)
            return false;

        return _source.TrySetResult(viewModel);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _source.TrySetResult(null);

        _lifetimeCts.Cancel();
        _lifetimeCts.Dispose();
    }

    private async YarnTask WatchCancellationAsync()
    {
        while (!_lifetimeCts.IsCancellationRequested)
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