using Yarn.Unity;

/// <summary>
/// 캠페인 엔딩. 표시와 대기를 분리한 유일한 화면이다.
///
/// 엔딩 노드 재생과 '함께' 떠 있어야 하므로 PresentEnding 은 await 하지 않는다.
/// 여기서 대기하면 노드가 끝날 때까지 화면이 비어 있게 된다.
/// 노드가 끝난 뒤 WaitEndingDismissAsync 가 확인 버튼을 열고 입력을 기다린다.
/// </summary>
public sealed partial class VnScreenBindings
{
    private bool _hasEndingResult;

    public void PresentEnding(CampaignEndingResult ending, CampaignState campaignState)
    {
        _hasEndingResult = false;

        UI.PushPanel<CampaignEndingPanel>(panel =>
        {
            BindPanel(panel, ApplyEndingBindings);
            panel.Present(ending, campaignState);
        });
    }

    /// <summary>엔딩 노드가 끝난 뒤에 확인 버튼을 연다.</summary>
    public async YarnTask WaitEndingDismissAsync()
    {
        CampaignEndingPanel panel = UI.GetUI<CampaignEndingPanel>();

        if (panel != null)
            panel.AllowDismiss();

        await YarnWait.UntilAsync(() => _hasEndingResult);

        ClosePanel();
        HideGuesthouseHud();
    }

    private void ApplyEndingBindings(CampaignEndingPanel panel)
    {
        AddBinding(panel,
            p => p.OnDismissed += HandleEndingDismissed,
            p => p.OnDismissed -= HandleEndingDismissed);
    }

    private void HandleEndingDismissed() => _hasEndingResult = true;
}
