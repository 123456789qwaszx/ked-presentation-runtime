using Yarn.Unity;

/// <summary>
/// 업무 수첩. (v3 §8) 통화로 확정된 개체만 열람할 수 있다.
///
/// 플로우가 강제하지 않는 열람 화면이다. 게시판 버튼 등 열람 진입점을 붙일 때 호출한다.
/// </summary>
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
