using UnityEngine;


// PresentationIntentState 한 단계 뒤,
// Solver가 계산한 최종 적용값.

// position은 target parent 기준이 아니라 Rig 공간 좌표다.
// local position이 아니고 Rig 공간인 이유는 좌표계가 섞이지 않게하기 위함.

// Target adapter가 이를 자신의 parent local 공간으로 변환해 적용한다.
public struct PresentationResponse
{
    // positionInRigSpace는 Rig 공간 기준 좌표,
    // 각 target의 parent 기준 좌표로 다시 바꿔야 함.
    public Vector2 positionInRigSpace;
    public Vector2 scale;
    public float alpha;
}
