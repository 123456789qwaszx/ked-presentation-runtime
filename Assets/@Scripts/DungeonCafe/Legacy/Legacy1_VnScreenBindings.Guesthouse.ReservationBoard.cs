// using System;
// using System.Collections.Generic;
// using System.Threading.Tasks;
//
// public sealed partial class VnScreenBindings
// {
//     private TaskCompletionSource<int> _boardSelectionCompletion;
//
//     public Task<int> PresentReservationBoardAsync(
//         int dayNumber,
//         IReadOnlyList<ServiceBookingState> bookings)
//     {
//         if (_boardSelectionCompletion != null &&
//             !_boardSelectionCompletion.Task.IsCompleted)
//         {
//             throw new InvalidOperationException(
//                 "예약 게시판의 선택을 이미 기다리고 있습니다.");
//         }
//
//         _boardSelectionCompletion =
//             new TaskCompletionSource<int>(
//                 TaskCreationOptions.RunContinuationsAsynchronously);
//
//
//         UI.PushPanel<ReservationBoardPanel>(panel =>
//         {
//             BindPanel(panel, ApplyReservationBoardBindings);
//             panel.Present(dayNumber, bookings);
//         });
//
//         return _boardSelectionCompletion.Task;
//     }
//
//     private void ApplyReservationBoardBindings(ReservationBoardPanel panel)
//     {
//         AddBinding(
//             panel,
//             p => p.OnBookingSelected += HandleBookingSelected,
//             p => p.OnBookingSelected -= HandleBookingSelected);
//     }
//
//     private void HandleBookingSelected(int index)
//     {
//         _boardSelectionCompletion?.TrySetResult(index);
//     }
// }