using System.Threading.Tasks;

public sealed partial class VnScreenBindings
{
    private bool _hasAssignmentResult;
    private string _pendingMaidId;

    public async Task<string> RequestMaidAssignmentAsync(MaidAssignmentRequest request)
    {
        _hasAssignmentResult = false;
        _pendingMaidId = null;

        OpenMaidAssignmentPanel(request);

        await AsyncWait.UntilAsync(() => _hasAssignmentResult);

        ClosePanel();

        return _pendingMaidId;
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
        _pendingMaidId = maidId;
        _hasAssignmentResult = true;
    }
}