using Yarn.Unity;

/// <summary>
/// 캠페인 엔딩. (v3 §15)
/// 엔딩 노드 연출은 CampaignFlow 이 이 호출 전에 끝낸다 - 패널을 올리고 즉시 확인을 연다.
/// </summary>
public sealed partial class VnScreenBindings
{
    private bool _hasEndingResult;

    public async YarnTask PresentEndingAsync(CampaignStateV3 campaign, EndingKindV3 ending)
    {
        RefreshGuesthouseHud("폐점");

        _hasEndingResult = false;

        UI.PushPanel<CampaignEndingPanel>(panel =>
        {
            BindPanel(panel, ApplyEndingBindings);
            panel.Present(campaign, ending);
            panel.AllowDismiss();
        });

        await AsyncWait.UntilAsync(() => _hasEndingResult);

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
