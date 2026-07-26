// using System.Collections.Generic;
// using Yarn.Unity;
//
// /// <summary>하루 진행이 필요로 하는 표현 계층 접점.</summary>
// public interface IDayPresentationPort : IGuesthouseHudPort
// {
//     /// <summary>게시판에 오늘의 예약 문의를 띄우고 확인 입력을 기다린다.</summary>
//     YarnTask PresentReservationBoardAsync(int dayNumber, IReadOnlyList<ServiceBookingState> bookings);
//
//     /// <summary>예약 확정 통화를 재생한다.</summary>
//     YarnTask PresentReservationCallAsync(ServiceBookingState booking);
//
//     /// <summary>담당 메이드를 선택받고 MaidId 를 반환한다.</summary>
//     YarnTask<string> RequestMaidAssignmentAsync(MaidAssignmentRequest request);
//
//     /// <summary>하루 리포트를 표시한다.</summary>
//     YarnTask PresentDayReportAsync(DayCycleState day);
// }
