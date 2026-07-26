using System.Collections.Generic;
using Yarn.Unity;

/// <summary>
/// 예약 게시판. 하루의 예약 3건을 한 번에 보여준다.
///
/// 다른 패널과 달리 대기가 끝나도 닫지 않는다.
/// 이어지는 예약 확정 통화 노드가 재생되는 동안 게시판이 배경에 남아 있어야 하기 때문이다.
/// 실제 닫기는 배정 패널이 열릴 때 CloseBoardIfOpen 으로 처리한다.
/// </summary>
public sealed partial class VnScreenBindings
{
    private bool _hasBoardResult;
    private bool _isBoardOpen;

    public async YarnTask PresentReservationBoardAsync(
        int dayNumber,
        IReadOnlyList<ServiceBookingState> bookings)
    {
        _hasBoardResult = false;

        UI.PushPanel<ReservationBoardPanel>(panel =>
        {
            BindPanel(panel, ApplyReservationBoardBindings);
            panel.Present(dayNumber, bookings);
        });

        _isBoardOpen = true;

        await YarnWait.UntilAsync(() => _hasBoardResult);
    }

    /// <summary>
    /// 게시판이 열려 있으면 닫는다. 배정 패널이 열리는 시점에 호출된다.
    /// 통화 노드가 중간에 끊겨도 게시판이 남지 않도록 여기서만 닫는다.
    /// </summary>
    private void CloseBoardIfOpen()
    {
        if (!_isBoardOpen)
            return;

        _isBoardOpen = false;
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
