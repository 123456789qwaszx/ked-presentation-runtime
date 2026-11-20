using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 컷인의 흰 덮개 — 네 점을 한 색으로 칠한다(cutin_flash_in의 흰 선 · 흰 판 · 걷힘, docs/PLAN_cutin.md §5.1).
/// 창 아래, 무대 루트 다음에 서서 그 무대의 내용(판 · 사람) 위를 덮는다. 마스크 밖이라 잘리지 않고, 창 안이라 창과 함께 움직인다.
/// 네 점은 무대 중심 기준 px(y 위) — 이 노드는 창을 꽉 채우고 피벗이 가운데라 판과 같은 좌표다.
/// 흰빛으로 여는 컷인이 처음 올 때 슬롯(StageCutInSlot)이 세운다 — 쓰지 않는 무대에는 없다.
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class StageCutInFlashGraphic : Graphic
{
    private Vector2 _p0;
    private Vector2 _p1;
    private Vector2 _p2;
    private Vector2 _p3;

    /// <summary>네 점(왼아래 · 왼위 · 오른위 · 오른아래 — 마스크의 사각형 규약과 같다).</summary>
    public void SetQuad(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
    {
        if (_p0 == p0 && _p1 == p1 && _p2 == p2 && _p3 == p3)
            return;

        _p0 = p0;
        _p1 = p1;
        _p2 = p2;
        _p3 = p3;

        SetVerticesDirty();
    }

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        UIVertex v = UIVertex.simpleVert;
        v.color = color;

        v.position = _p0;
        vh.AddVert(v);

        v.position = _p1;
        vh.AddVert(v);

        v.position = _p2;
        vh.AddVert(v);

        v.position = _p3;
        vh.AddVert(v);

        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(0, 2, 3);
    }
}
