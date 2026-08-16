// // - BeginForwardSettle registers a scripted advance as awaiting settle completion.
// // - NotifySettled advances Epoch only for registered in-flight settles.
// // - ClearInFlightSettles drops only the pending settles that have not completed yet.
// public sealed class ForwardSettleClock
// {
//     // Number of forward advances still awaiting settle completion.
//     private int _inFlightForwardSettles;
//
//     // Total number of valid settle completions so far.
//     public int Epoch { get; private set; }
//
//     public void BeginForwardSettle() => _inFlightForwardSettles++;
//     public void ClearInFlightSettles() => _inFlightForwardSettles = 0;
//     
//     public void NotifySettled()
//     {
//         if (_inFlightForwardSettles <= 0)
//             return;
//
//         _inFlightForwardSettles--;
//         Epoch++;
//     }
// }