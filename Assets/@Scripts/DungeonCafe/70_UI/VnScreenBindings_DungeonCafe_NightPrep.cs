using System.Collections.Generic;
using Yarn.Unity;

public sealed partial class VnScreenBindings
{
    private bool _hasNightPrepResult;
    private IReadOnlyList<string> _pendingPrepPurchases;
    private IReadOnlyList<string> _pendingPrepEquips;

    public async YarnTask<NightPrepResponse> RequestNightPrepAsync(NightPrepRequest request)
    {
        RefreshDungeonCafeHud("밤 - 상점");

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

        return new NightPrepResponse(
            purchase: _pendingPrepPurchases,
            equip: _pendingPrepEquips);
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
