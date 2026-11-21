using UnityEngine;

/// <summary>
/// 카메라 중립에서 본 무대 — 배치 계산이 카메라를 모르게 하는 계약.
///
/// 이 런타임에 카메라는 없다. 샷은 무대 컨테이너(카메라 리그의 노드, 깊이 층)를 움직여
/// 카메라처럼 보이게 한다. 초점 측정은 무대 좌표로 정의되므로, 계산 사슬이
/// 이 노드들을 지날 때 그 순간의 값이 아니라 중립값을 써야 한다 — 그래야 결과가 샷의 진행도와
/// 무관하고, 일반 재생 · 압축 재생(롤백 · 이어하기) · 코어 폴드가 같은 자리를 낸다.
///
/// 중립 = 샷이 아무것도 하지 않은 무대: 카메라 노드와 깊이 층이 기본 의도(<see cref="PresentationIntentState.Default"/>)의 값,
/// 곧 항등이다. 코어 노드 트리에 카메라 노드가 없는 것과 같은 뜻이다.
/// 샷이 움직이는 노드의 중립 회전은 언제나 0이다 — 그래서 위치 · 크기만 낸다(받는 쪽이 회전을 0으로 둔다).
/// </summary>
public interface IStageViewNeutral
{
    /// <summary>
    /// <paramref name="rect"/>가 샷이 움직이는 무대 노드면 중립 위치 · 크기를 내고 true.
    /// 아니면 false — 호출자는 라이브 값을 쓴다.
    /// </summary>
    bool TryGetNeutral(RectTransform rect, out Vector2 anchoredPosition, out Vector3 localScale);
}
