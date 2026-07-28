using System.Collections.Generic;
using System.Threading.Tasks;
using Yarn.Unity;

public sealed partial class VnScreenBindings
{
    private bool _hasBoardResult;
    private int _pendingBoardIndex;

    public async Task<int> PresentBoardAsync(
        int dayNumber,
        IReadOnlyList<MonsterProfileV3> bookings,
        CampaignStateV3 campaign)
    {
        _hudSlotIndex = 0;
        RefreshGuesthouseHud("예약 게시판");

        _hasBoardResult = false;
        _pendingBoardIndex = 0;

        OpenReservationBoardPanel(dayNumber, bookings, campaign);

        await AsyncWait.UntilAsync(() => _hasBoardResult);

        ClosePanel();
        return _pendingBoardIndex;
    }
    
    private void OpenReservationBoardPanel(
        int dayNumber,
        IReadOnlyList<MonsterProfileV3> bookings,
        CampaignStateV3 campaign)
    {
        UI.PushPanel<ReservationBoardPanel>(panel =>
        {
            BindPanel(panel, ApplyReservationBoardBindings);
            panel.Present(dayNumber, bookings, campaign);
        });
    }

    private void ApplyReservationBoardBindings(ReservationBoardPanel panel)
    {
        AddBinding(panel,
            p => p.OnBookingSelected += HandleBookingSelected,
            p => p.OnBookingSelected -= HandleBookingSelected);
    }

    private void HandleBookingSelected(int index)
    {
        _pendingBoardIndex = index;
        _hasBoardResult = true;
    }
}
