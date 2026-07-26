using System;
using System.Threading.Tasks;

public sealed partial class VnScreenBindings
{
    private TaskCompletionSource<string> _maidAssignmentCompletion;

    public Task<string> RequestMaidAssignmentAsync(MaidAssignmentRequest request)
    {
        if (_maidAssignmentCompletion != null)
            throw new InvalidOperationException("메이드 배정 결과를 이미 기다리고 있습니다.");
        
        _maidAssignmentCompletion =
            new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        OpenMaidAssignmentPanel(request);

        return _maidAssignmentCompletion.Task;
    }

    private void OpenMaidAssignmentPanel(MaidAssignmentRequest request)
    {
        UI.PushPanel<MaidAssignmentPanel>(panel =>
        {
            BindPanel(panel, ApplyMaidAssignmentBindings);
            panel.Present(request);
        });
    }

    private void ApplyMaidAssignmentBindings(MaidAssignmentPanel panel)
    {
        AddBinding(panel,
            p => p.OnMaidSelected += HandleMaidSelected,
            p => p.OnMaidSelected -= HandleMaidSelected);
    }

    private void HandleMaidSelected(string maidId)
    {
        if (_maidAssignmentCompletion == null)
            return;

        _maidAssignmentCompletion.TrySetResult(maidId);

        ClosePanel();
        _maidAssignmentCompletion = null;
    }
}