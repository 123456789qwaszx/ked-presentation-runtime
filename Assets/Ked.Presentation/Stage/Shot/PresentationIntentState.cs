using System;
using UnityEngine;

// 저작된 샷 의도 — 카메라의 축 값 한 벌. 최종 트랜스폼이 아니다.
// 코어 ShotIntentState와 필드가 1:1이고(PresentationIntentStateCoreBridge), 무대에 쓰는 값은 코어 ShotLayoutMath가 이 값에서 푼다.
[Serializable]
public struct PresentationIntentState
{
    // 렌즈 — 저작된 zoom 의도. 최종 카메라 배율이 아니다.
    public float zoom;

    // 렌즈 — 팬(화면 밀기). rig space px, 화면이 가는 쪽이 +.
    public Vector2 panInRigSpace;

    // 렌즈 — rig space의 논리 초점. 층 산개가 이 점 기준으로 풀린다. 줌과 함께 움직인다.
    public Vector2 focusPointInRigSpace;

    // 몸 — 카메라가 서는 자리. mid 면의 이 무대 점이 화면 가운데에 온다. 카메라가 가는 쪽이 +.
    public Vector2 track;

    // 몸 — 카메라의 앞뒤 자리. 줌과 같은 눈금, +면 다가간다.
    public float dolly;

    // 헤드 — 화면의 기울기(도). +면 그림이 반시계로 돈다.
    public float roll;

    public static PresentationIntentState Default => new()
    {
        zoom = 0f,
        panInRigSpace = Vector2.zero,
        focusPointInRigSpace = Vector2.zero,
        track = Vector2.zero,
        dolly = 0f,
        roll = 0f,
    };
}
