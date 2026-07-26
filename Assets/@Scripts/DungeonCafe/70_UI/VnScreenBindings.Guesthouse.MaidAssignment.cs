using Yarn.Unity;

public sealed partial class VnScreenBindings
{
    private string _pendingMaidId;
    private bool _hasAssignmentResult;

    public async YarnTask<string> RequestMaidAssignmentAsync(MaidAssignmentRequest request)
    {
        _hasAssignmentResult = false;
        _pendingMaidId = null;

        UI.PushPanel<MaidAssignmentPanel>(panel =>
        {
            BindPanel(panel, ApplyMaidAssignmentBindings);
            panel.Present(request);
        });

        await AsyncWait.UntilAsync(() => _hasAssignmentResult);

        return _pendingMaidId;
    }

    private void ApplyMaidAssignmentBindings(MaidAssignmentPanel panel)
    {
        AddBinding(panel,
            p => p.OnMaidSelected += HandleMaidSelected,
            p => p.OnMaidSelected -= HandleMaidSelected);
    }

    private void HandleMaidSelected(string maidId)
    {
        _pendingMaidId = maidId;
        _hasAssignmentResult = true;

        ClosePanel();
    }
}
