using System.Collections.Generic;
using Yarn.Unity;

public sealed partial class VnScreenBindings
{
    private bool _hasAssignmentResult;
    private string _pendingMaidId;

    public async YarnTask<string> RequestAssignmentAsync(
        MonsterProfileV3 monster, IReadOnlyList<MaidStateV3> candidates, CampaignStateV3 campaign)
    {
        _hudSlotIndex++;
        RefreshGuesthouseHud("메이드 배정");

        _hasAssignmentResult = false;
        _pendingMaidId = null;

        UI.PushPanel<MaidAssignmentPanel>(panel =>
        {
            BindPanel(panel, ApplyMaidAssignmentBindings);
            panel.Present(monster, candidates, campaign);
        });

        await AsyncWait.UntilAsync(() => _hasAssignmentResult);

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
