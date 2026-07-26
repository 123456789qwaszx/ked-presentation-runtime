using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public sealed partial class VnScreenBindings
{
    private TaskCompletionSource<int> _boardSelectionCompletion;

    public Task<int> RequestReservationSelectionAsync(
        int dayNumber,
        IReadOnlyList<ServiceBookingState> bookings)
    {
        if (_boardSelectionCompletion != null)
            throw new InvalidOperationException("예약 게시판의 선택을 이미 기다리고 있습니다.");
        
        // TrySetResult()가 await 이후 코드를 그 자리에서 즉시 끼워 넣지 못하게 해서, 현재 HandleBookingSelected()를 끝까지 실행
        _boardSelectionCompletion =
            new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        OpenReservationBoardPanel(dayNumber, bookings);

        return _boardSelectionCompletion.Task;
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
        if (_boardSelectionCompletion == null)
            return;
        
        _boardSelectionCompletion.TrySetResult(index);
        
        ClosePanel();
        _boardSelectionCompletion = null;
    }
}