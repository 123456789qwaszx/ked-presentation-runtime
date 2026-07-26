// using Yarn.Unity;
//
// /// <summary>
// /// 접객 세션이 필요로 하는 표현 계층 접점.
// /// 플로우가 Yarn/UI 구현을 직접 알지 않도록 분리한다. 테스트에서는 즉시 응답하는 더미로 교체한다.
// /// </summary>
// public interface IServicePresentationPort : IGuesthouseHudPort
// {
//     /// <summary>
//     /// 세션 상황이 갱신되었음을 알린다. 표현 계층은 표시용 문맥만 동기화한다.
//     /// 접객 노드는 배정 메이드와 무관하게 공유되므로, 대본이 배정 결과를 알려면 이 통지가 필요하다.
//     /// 여기서 게임 상태를 바꾸어서는 안 된다.
//     /// </summary>
//     void NotifySessionContext(ServiceSessionState session);
//
//     /// <summary>지정한 Yarn 노드를 끝까지 재생한다. 노드 이름이 비어 있으면 즉시 반환한다.</summary>
//     YarnTask PlayNodeAsync(string nodeName);
//
//     /// <summary>행동 승인을 요청하고 선택된 후보의 인덱스를 반환한다.</summary>
//     YarnTask<int> RequestActionApprovalAsync(ServiceApprovalRequest request);
//
//     /// <summary>통제 신호가 거부되었음을 알린다. 이후 승인 입력은 무시된다.</summary>
//     void NotifyControlLost(ServiceSessionState session);
//
//     /// <summary>엔딩 화면을 띄운다. 엔딩 노드 재생과 겹쳤야 하므로 기다리지 않는다.</summary>
//     void PresentEnding(CampaignEndingResult ending, CampaignState campaign);
//
//     /// <summary>엔딩 노드가 끝난 뒤 확인 입력을 기다린다.</summary>
//     YarnTask WaitEndingDismissAsync();
//
//     /// <summary>결산 결과를 표시하고 확인 입력을 기다린다.</summary>
//     YarnTask PresentSettlementAsync(ServiceSettlementResult result);
// }
