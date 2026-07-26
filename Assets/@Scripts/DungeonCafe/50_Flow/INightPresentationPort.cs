// using Yarn.Unity;
//
// /// <summary>밤 진행이 필요로 하는 표현 계층 접점.</summary>
// public interface INightPresentationPort : IGuesthouseHudPort
// {
//     /// <summary>회복/관리 붕괴 중 무엇을 누구에게 적용할지 선택받는다.</summary>
//     YarnTask<NightPlan> RequestNightPlanAsync(NightPlanRequest request);
//
//     /// <summary>선택된 처리의 본편 노드를 재생한다.</summary>
//     YarnTask PlayNightProgramAsync(NightPlan plan, NightProgramResult result);
//
//     /// <summary>업무 숙련 이벤트를 재생한다.</summary>
//     YarnTask PlayMasteryEventAsync(MasteryEventResult result);
//
//     /// <summary>메이드 간 대화를 재생한다.</summary>
//     YarnTask PlayMaidConversationAsync(int dayNumber);
// }
