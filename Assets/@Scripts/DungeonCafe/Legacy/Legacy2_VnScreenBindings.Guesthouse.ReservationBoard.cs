// using System.Collections.Generic;
//
// public sealed partial class VnScreenBindings
// {
//     public void OpenReservationBoardPanel(
//         int dayNumber,
//         IReadOnlyList<ServiceBookingState> bookings)
//     {
//         UI.PushPanel<ReservationBoardPanel>(panel =>
//         {
//             BindPanel(panel, ApplyReservationBoardBindings);
//             panel.Present(dayNumber, bookings);
//         });
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
//     }
// }