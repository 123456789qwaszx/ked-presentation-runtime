using UnityEngine;

/// <summary>
/// 카메라 리그 하나의 노드 참조. 빌더가 만들고 보장한다 — null 검사하지 않는다.
/// 카메라 노드의 소유 목록이 곧 이 객체다: Owns.
/// </summary>
public sealed class CameraRigRefs
{
    private readonly RectTransform[] _rects;

    internal CameraRigRefs(RectTransform[] rects)
    {
        _rects = rects;
    }

    public RectTransform Get(CameraRigNode node) => _rects[(int)node];

    public RectTransform Root => Get(CameraRigNode.Camera_Root);
    public RectTransform Shake => Get(CameraRigNode.Camera_Shake);
    public RectTransform Pan => Get(CameraRigNode.Camera_Pan);
    public RectTransform Zoom => Get(CameraRigNode.Camera_Zoom);
    public RectTransform Roll => Get(CameraRigNode.Camera_Roll);
    public RectTransform Dolly => Get(CameraRigNode.Camera_Dolly);
    public RectTransform Track => Get(CameraRigNode.Camera_Track);

    /// <summary>층이 서는 자리 — 가장 안쪽 노드.</summary>
    public RectTransform Mount => Get(CameraRigSchema.Mount);

    /// <summary>이 리그의 노드인가. 카메라 노드의 중립은 전부 항등이라, 어느 노드인지는 물을 필요가 없다.</summary>
    public bool Owns(RectTransform rect)
    {
        for (int i = 0; i < _rects.Length; i++)
        {
            if (_rects[i] == rect)
                return true;
        }

        return false;
    }
}
