using System;
using UnityEngine;

// 저작자가 조작하는 샷 의도값.
// 실제 Transform 수치가 아니라, Rig가 해석할 상태값만 보관한다.
[Serializable]
public struct PresentationIntentState
{
    [Range(-10f, 10f)]
    public float zoom; // 얼마나 가까이 들어갈지

    public Vector2 pan; // 그 대상을 화면 구도 안으로 옮기기 위해 카메라를 민 양

    // Rig 공간(Stage_Root 기준)에서의 카메라가 보고 싶은 대상 위치
    public Vector2 focusPoint;

    public static PresentationIntentState Default => new PresentationIntentState
    {
        zoom = 0f,
        pan = Vector2.zero,
        focusPoint = Vector2.zero,
    };
}
