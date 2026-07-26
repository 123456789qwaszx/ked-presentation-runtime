using System;
using System.Threading.Tasks;

public sealed partial class VnScreenBindings
{
    private bool _isWaitingEndingDismiss;
    private bool _hasEndingResult;

    public void PresentEnding(CampaignEndingResult ending, CampaignState campaign)
    {
        _hasEndingResult = false;

        OpenEndingPanel(ending, campaign);
    }

    // 엔딩 노드가 끝난 뒤에 확인 버튼을 연다.
    public async Task WaitEndingDismissAsync()
    {
        if (_isWaitingEndingDismiss)
            throw new InvalidOperationException("엔딩 확인을 이미 기다리고 있습니다.");

        _isWaitingEndingDismiss = true;

        AllowEndingDismiss();

        await AsyncWait.UntilAsync(() => _hasEndingResult);

        ClosePanel();
        HideGuesthouseHud();

        _isWaitingEndingDismiss = false;
    }

    private void OpenEndingPanel(CampaignEndingResult ending, CampaignState campaign)
    {
        UI.PushPanel<CampaignEndingPanel>(panel =>
        {
            BindPanel(panel, ApplyEndingBindings);
            panel.Present(ending, campaign);
        });
    }

    private void AllowEndingDismiss()
    {
        CampaignEndingPanel panel = UI.GetUI<CampaignEndingPanel>();

        if (panel != null)
            panel.AllowDismiss();
    }

    private void ApplyEndingBindings(CampaignEndingPanel panel)
    {
        AddBinding(panel,
            p => p.OnDismissed += HandleEndingDismissed,
            p => p.OnDismissed -= HandleEndingDismissed);
    }

    private void HandleEndingDismissed() => _hasEndingResult = true;
}