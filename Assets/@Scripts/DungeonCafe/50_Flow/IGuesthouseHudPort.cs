// /// <summary>
// /// 노드 재생과 무관하게 계속 떠 있는 표시 계층의 접점.
// ///
// /// 패널은 입력을 받기 위해 열리고 닫히지만, 상황 표시는 노드가 재생되는 동안에도 남아 있어야 한다.
// /// 그래서 패널 스택이 아니라 별도 경로로 갱신한다.
// ///
// /// 세 개의 플로우 포트가 공통으로 상속하므로, 구현체는 한 번만 채우면 된다.
// /// 갱신은 전부 동기 호출이다. 여기서 await 하면 노드 재생 시점이 밀린다.
// /// </summary>
// public interface IGuesthouseHudPort
// {
//     /// <summary>캠페인 시작 시 한 번. 이후 구간 전환마다 값만 갱신한다.</summary>
//     void ShowHud();
//
//     /// <summary>캠페인 종료 시 한 번.</summary>
//     void HideHud();
//
//     /// <summary>표시값을 덮어쓴다. 노드를 재생하기 '직전'에 호출해야 동시에 보인다.</summary>
//     void NotifyHud(in GuesthouseHudSnapshot snapshot);
// }
