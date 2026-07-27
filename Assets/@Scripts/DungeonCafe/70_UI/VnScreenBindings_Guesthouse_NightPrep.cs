using System.Collections.Generic;
using Yarn.Unity;

/// <summary>
/// 밤 시작 상점/장착. (v3 §11)
/// 구매 예약과 장착 구성을 한 번에 받아 그대로 시스템에 돌려준다.
/// </summary>
public sealed partial class VnScreenBindings
{
    private bool _hasNightPrepResult;
    private IReadOnlyList<string> _pendingPrepPurchases;
    private IReadOnlyList<string> _pendingPrepEquips;

    public async YarnTask<NightPrepResponseV3> RequestNightPrepAsync(NightPrepRequestV3 request)
    {
        RefreshGuesthouseHud("밤 - 상점");

        _hasNightPrepResult = false;
        _pendingPrepPurchases = null;
        _pendingPrepEquips = null;

        UI.PushPanel<NightPrepPanel>(panel =>
        {
            BindPanel(panel, ApplyNightPrepBindings);
            panel.Present(request);
        });

        await AsyncWait.UntilAsync(() => _hasNightPrepResult);

        ClosePanel();

        return new NightPrepResponseV3(_pendingPrepPurchases, _pendingPrepEquips);
    }

    private void ApplyNightPrepBindings(NightPrepPanel panel)
    {
        AddBinding(panel,
            p => p.OnPrepConfirmed += HandleNightPrepConfirmed,
            p => p.OnPrepConfirmed -= HandleNightPrepConfirmed);
    }

    private void HandleNightPrepConfirmed(IReadOnlyList<string> purchases, IReadOnlyList<string> equips)
    {
        _pendingPrepPurchases = purchases;
        _pendingPrepEquips = equips;
        _hasNightPrepResult = true;
    }
}
