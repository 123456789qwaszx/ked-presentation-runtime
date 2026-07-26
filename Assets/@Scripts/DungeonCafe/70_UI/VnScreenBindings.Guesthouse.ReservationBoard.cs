using System.Collections.Generic;
using System.Threading.Tasks;

public sealed partial class VnScreenBindings
{
    private bool _hasBoardResult;
    private int _pendingBoardIndex;

    public async Task<int> RequestReservationSelectionAsync(
        int dayNumber,
        IReadOnlyList<ServiceBookingState> bookings)
    {
        _hasBoardResult = false;
        _pendingBoardIndex = 0;

        OpenReservationBoardPanel(dayNumber, bookings);

        await AsyncWait.UntilAsync(() => _hasBoardResult);

        ClosePanel();

        return _pendingBoardIndex;
    }

    private void OpenReservationBoardPanel(
        int dayNumber,
        IReadOnlyList<ServiceBookingState> bookings)
    {
        UI.PushPanel<ReservationBoardPanel>(panel =>
        {
            BindPanel(panel, ApplyReservationBoardBindings);
            panel.Present(dayNumber, bookings);
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