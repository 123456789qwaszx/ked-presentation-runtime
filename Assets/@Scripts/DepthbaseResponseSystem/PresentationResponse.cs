using UnityEngine;

/// <summary>
/// Solver가 계산한 최종 적용값.
/// position은 target parent 기준이 아니라 Rig 공간 좌표다.
/// Target adapter가 이를 자신의 parent local 공간으로 변환해 적용한다.
/// </summary>
public struct PresentationResponse
{
    public Vector2 positionInRigSpace;
    public Vector2 scale;
    public float alpha;
}
