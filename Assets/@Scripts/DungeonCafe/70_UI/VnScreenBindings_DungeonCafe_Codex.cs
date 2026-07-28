using Yarn.Unity;

// 업무 수첩.
// 통화로 확정된 개체만 열람.
// 플로우가 강제하지 않는 열람 화면이다. 게시판 버튼 등 열람 진입점을 붙일 때 호출.
public sealed partial class VnScreenBindings
{
    private bool _hasCodexResult;

    public async YarnTask PresentCodexAsync(CampaignState campaign)
    {
        _hasCodexResult = false;

        UI.PushPanel<MonsterCodexPanel>(panel =>
        {
            BindPanel(panel, ApplyCodexBindings);
            panel.Present(campaign);
        });

        await AsyncWait.UntilAsync(() => _hasCodexResult);

        ClosePanel();
    }

    private void ApplyCodexBindings(MonsterCodexPanel panel)
    {
        AddBinding(panel,
            p => p.OnCloseRequested += HandleCodexClosed,
            p => p.OnCloseRequested -= HandleCodexClosed);
    }

    private void HandleCodexClosed() => _hasCodexResult = true;
}