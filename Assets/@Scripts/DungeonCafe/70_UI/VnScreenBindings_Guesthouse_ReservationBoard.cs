using System.Collections.Generic;
using Yarn.Unity;

/// <summary>
/// 예약 게시판. (v3 §1)
/// 편성은 시스템이 확정한 뒤 넘어오므로 이 화면은 열람이다 - 어느 카드를 눌러도 닫힌다.
/// </summary>
public sealed partial class VnScreenBindings
{
    private bool _hasBoardResult;

    public async YarnTask PresentBoardAsync(
        int dayNumber, IReadOnlyList<MonsterProfileV3> bookings, CampaignStateV3 campaign)
    {
        _guesthouseCampaign = campaign;
        _hudSlotIndex = 0;
        RefreshGuesthouseHud("예약 게시판");

        _hasBoardResult = false;

        UI.PushPanel<ReservationBoardPanel>(panel =>
        {
            BindPanel(panel, ApplyReservationBoardBindings);
            panel.Present(dayNumber, bookings, campaign);
        });

        await AsyncWait.UntilAsync(() => _hasBoardResult);

        ClosePanel();
    }

    private void ApplyReservationBoardBindings(ReservationBoardPanel panel)
    {
        AddBinding(panel,
            p => p.OnBookingSelected += HandleBookingSelected,
            p => p.OnBookingSelected -= HandleBookingSelected);
    }

    private void HandleBookingSelected(int index)
    {
        _hasBoardResult = true;
    }
}
