using System;

/// <summary>카메라 리그의 노드. 이름이 곧 스키마 id다(rig-schemas.json의 `camera` 리그).</summary>
public enum CameraRigNode
{
    Camera_Root,
    Camera_Shake,
    Camera_Pan,
    Camera_Zoom,
    Camera_Roll,
    Camera_Dolly,
    Camera_Track,
}

/// <summary>
/// 카메라 리그의 계층 — 무대 리그(StageRigSchema)와 따로 선다. 캐릭터 리그와 같은 규율이다: 한 노드 = 한 축.
///
/// 이 런타임에 카메라는 없다. 카메라 변환의 역을 무대에 걸어 카메라처럼 보이게 한다 — 그래서 카메라에서
/// 부모인 몸이 여기서는 가장 안쪽이다.
///
///   Camera_Root       안 움직인다. 리그의 경계
///     Camera_Shake    연기  흔들림. 위치 · 회전 — 모션 클립만 쓴다. 끝나면 항등
///       Camera_Pan    렌즈  팬(화면 밀기). anchoredPosition
///         Camera_Zoom   렌즈  줌. localScale
///           Camera_Roll   헤드  롤. localEulerAngles.z
///             Camera_Dolly  몸    달리의 공통 몫(mid 면의 배율). localScale
///               Camera_Track  몸    트랙의 공통 몫. anchoredPosition = −track
///                 (층)        무대 리그가 여기에 건다 — Mount
///
/// 공통 몫은 카메라 노드에, 깊이 d에 따른 차이는 층 노드에 쓴다. 값은 전부 코어 ShotLayoutMath의 출력이다
/// (CameraPose) — 흔들림만 빼고. 줌 · 롤 · 달리는 중심이 같아(피벗 중앙 · 위치 0) 순서를 바꿔도 값이 같다.
/// 순서는 겹(렌즈 → 헤드 → 몸)을 읽히게 하려는 것이다.
///
/// 모든 노드는 stretch 앵커 · 피벗 중앙 · 항등 트랜스폼으로 선다. 그래서 카메라 노드의 중립은 전부 항등이다.
/// 노드 이름 = "{무대 키}_{노드}" — 카메라 리그는 무대마다 하나씩 선다. 어느 무대의 것을 샷이 움직이는지는
/// 역할(StageRoles)이 정한다.
/// </summary>
public static class CameraRigSchema
{
    public const string RigKind = "camera";

    public sealed class NodeDef
    {
        public CameraRigNode Id;
        public CameraRigNode? Parent;
    }

    public static readonly NodeDef[] Nodes =
    {
        new() { Id = CameraRigNode.Camera_Root, Parent = null },
        new() { Id = CameraRigNode.Camera_Shake, Parent = CameraRigNode.Camera_Root },
        new() { Id = CameraRigNode.Camera_Pan, Parent = CameraRigNode.Camera_Shake },
        new() { Id = CameraRigNode.Camera_Zoom, Parent = CameraRigNode.Camera_Pan },
        new() { Id = CameraRigNode.Camera_Roll, Parent = CameraRigNode.Camera_Zoom },
        new() { Id = CameraRigNode.Camera_Dolly, Parent = CameraRigNode.Camera_Roll },
        new() { Id = CameraRigNode.Camera_Track, Parent = CameraRigNode.Camera_Dolly },
    };

    /// <summary>가장 안쪽 노드 — 무대 리그가 층을 거는 자리.</summary>
    public const CameraRigNode Mount = CameraRigNode.Camera_Track;

    public static string[] NodeIds() => Array.ConvertAll(Nodes, n => n.Id.ToString());

    /// <summary>무대 키가 비면 스키마 id 그대로다 — 스키마를 덤프할 때의 견본 리그.</summary>
    public static string NodeName(string stageKey, CameraRigNode id)
        => string.IsNullOrEmpty(stageKey) ? id.ToString() : $"{stageKey}_{id}";
}
