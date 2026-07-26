using Yarn.Unity;

/// <summary>
/// 담당 메이드 배정. 접객 슬롯마다 한 번 열린다.
///
/// 여는 시점에 예약 게시판을 걷어낸다. 통화 노드가 끝난 뒤 게시판이 남아 있을 수 있기 때문이다.
/// </summary>
public sealed partial class VnScreenBindings
{
    private string _pendingMaidId;
    private bool _hasAssignmentResult;

    public async YarnTask<string> RequestMaidAssignmentAsync(MaidAssignmentRequest request)
    {
        CloseBoardIfOpen();

        _hasAssignmentResult = false;
        _pendingMaidId = null;

        UI.PushPanel<MaidAssignmentPanel>(panel =>
        {
            BindPanel(panel, ApplyMaidAssignmentBindings);
            panel.Present(request);
        });

        await YarnWait.UntilAsync(() => _hasAssignmentResult);

        ClosePanel();

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
    }
}
