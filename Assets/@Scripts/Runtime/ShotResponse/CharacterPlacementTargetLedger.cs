using System.Collections.Generic;
using Ked.Presentation.Core;
using UnityEngine;

// "트윈이 다 끝났다면 어디에 있을 것인가"의 예약 장부 — 코어 PlacementTargetLedger의 어댑터.
//
// 종전 구현은 정착 상태를 알기 위해 유니티 무대를 잠깐 오염시켰다 되돌렸다:
//   측정 직전에 조상들을 예약 target 값으로 세팅 → 측정 → 즉시 원복.
// try/finally가 없어서 측정 중 예외가 나면 리그가 더럽게 남았다.
//
// 이제 측정이 아무것도 쓰지 않는다. 라이브 체인을 값으로 떠서 예약을 입히고
// RectChainMath로 직접 계산한다 — 복원이 없으니 더러워질 상태가 없다.
//
// 여기 남는 일은 유니티 경계뿐이다:
// - RectTransform ↔ 논리 키 대응 (참조 기반. 이름은 읽기 좋으라고 쓴다)
// - 라이브 체인 캡처
// - stopRoot 로컬 ↔ 월드 변환
//
// 공개 시그니처는 종전 그대로다 — 호출부는 손대지 않는다.
public sealed class CharacterPlacementTargetLedger
{
    private readonly PlacementTargetLedger _core = new();

    // 참조 → 키. 이름이 같은 rect가 둘 있어도 참조가 다르면 키를 나눈다.
    private readonly Dictionary<RectTransform, string> _keyByRect = new();
    private readonly Dictionary<string, RectTransform> _rectByKey = new();

    private readonly List<RectTransform> _scratchRects = new(48);
    private readonly List<RectNodeState> _scratchChain = new(48);

    // ── 게시 ─────────────────────────────────────────────────────────

    public void PublishAnchoredPosition(RectTransform node, Vector2 targetAnchoredPosition)
    {
        if (node == null)
            return;

        _core.PublishAnchoredPosition(KeyOf(node), ToVec2(targetAnchoredPosition));
    }

    public void PublishLocalScale(RectTransform node, Vector2 targetLocalScaleXY)
    {
        if (node == null)
            return;

        _core.PublishLocalScale(KeyOf(node), ToVec2(targetLocalScaleXY));
    }

    public void PublishLocalEuler(RectTransform node, Vector3 targetLocalEuler)
    {
        if (node == null)
            return;

        _core.PublishLocalEuler(KeyOf(node), ToVec3(targetLocalEuler));
    }

    public void Clear(RectTransform node)
    {
        if (node == null)
            return;

        if (_keyByRect.TryGetValue(node, out string key))
            _core.Clear(key);
    }

    // ── 정착 측정 (아무것도 쓰지 않는다) ─────────────────────────────

    /// <summary>
    /// measureRect 로컬의 한 점을, 예약이 전부 도착했다고 가정한 상태의 월드 좌표로.
    /// 예약이 없으면 라이브 측정과 같다(종전과 동일한 빠른 경로).
    /// </summary>
    public Vector3 MeasureSettledWorldPoint(
        RectTransform measureRect,
        Vector3 localOffset,
        RectTransform stopRoot)
    {
        if (measureRect == null)
            return Vector3.zero;

        if (_core.IsEmpty || stopRoot == null)
            return measureRect.TransformPoint(localOffset);

        RectNodeState[] chain = CaptureSettledChain(measureRect, stopRoot);

        Vec3 inRootSpace = RectChainMath.TransformPoint(chain, SpaceOf(stopRoot), ToVec3(localOffset));

        return stopRoot.TransformPoint(new Vector3(inRootSpace.X, inRootSpace.Y, inRootSpace.Z));
    }

    /// <summary>
    /// 월드의 한 점을, 예약이 전부 도착했다고 가정한 상태의 parentRect 로컬 좌표로.
    /// </summary>
    public Vector2 WorldPointToSettledParentLocalPoint(
        RectTransform parentRect,
        Vector3 worldPoint,
        RectTransform stopRoot)
    {
        if (parentRect == null)
            return Vector2.zero;

        if (_core.IsEmpty || stopRoot == null)
        {
            Vector3 liveLocal = parentRect.InverseTransformPoint(worldPoint);
            return new Vector2(liveLocal.x, liveLocal.y);
        }

        RectNodeState[] chain = CaptureSettledChain(parentRect, stopRoot);

        Vector3 inRootSpace = stopRoot.InverseTransformPoint(worldPoint);

        Vec3 settledLocal = RectChainMath.InverseTransformPoint(
            chain, SpaceOf(stopRoot), ToVec3(inRootSpace));

        return new Vector2(settledLocal.X, settledLocal.Y);
    }

    // ── 유니티 경계 ──────────────────────────────────────────────────

    /// <summary>
    /// leaf에서 stopRoot 직전까지의 라이브 상태를 루트→leaf 순서로 뜨고, 예약을 입힌다.
    ///
    /// stopRoot가 조상이 아니거나 체인에 RectTransform이 아닌 노드가 있으면
    /// 조용히 어긋나는 대신 예외다 — 실경로에서는 발생하지 않는 방어선이다.
    /// </summary>
    private RectNodeState[] CaptureSettledChain(RectTransform leaf, RectTransform stopRoot)
    {
        _scratchRects.Clear();
        _scratchChain.Clear();

        Transform current = leaf;

        while (current != null && current != stopRoot)
        {
            if (!(current is RectTransform rect))
            {
                throw new System.InvalidOperationException(
                    $"[CharacterPlacementTargetLedger] '{leaf.name}'에서 '{stopRoot.name}'까지의 체인에 " +
                    $"RectTransform이 아닌 노드 '{current.name}'가 있다. 좌표 규약이 성립하지 않는다.");
            }

            _scratchRects.Add(rect);
            current = current.parent;
        }

        if (current == null)
        {
            throw new System.InvalidOperationException(
                $"[CharacterPlacementTargetLedger] '{stopRoot.name}'가 '{leaf.name}'의 조상이 아니다.");
        }

        // 루트에 가까운 쪽부터 — RectChainMath 규약(chain[0]의 부모가 rootSpace).
        for (int i = _scratchRects.Count - 1; i >= 0; i--)
        {
            RectTransform rect = _scratchRects[i];
            RectNodeState live = CaptureLive(rect);

            // 예약이 있으면 입힌다. 없으면 라이브 그대로 — 아무것도 쓰지 않는다.
            _scratchChain.Add(
                _keyByRect.TryGetValue(rect, out string key)
                    ? _core.ApplyTo(key, live)
                    : live);
        }

        return _scratchChain.ToArray();
    }

    private static RectNodeState CaptureLive(RectTransform rect)
    {
        return new RectNodeState(
            anchoredPosition: ToVec2(rect.anchoredPosition),
            anchorMin: ToVec2(rect.anchorMin),
            anchorMax: ToVec2(rect.anchorMax),
            pivot: ToVec2(rect.pivot),
            sizeDelta: ToVec2(rect.sizeDelta),
            localScale: ToVec3(rect.localScale),
            localEulerAngles: ToVec3(rect.localEulerAngles));
    }

    private static RectSpace SpaceOf(RectTransform rect)
        => new(ToVec2(rect.rect.size), ToVec2(rect.pivot));

    /// <summary>
    /// 참조 → 논리 키. 이름을 키로 쓰되 대응은 참조 기반이라, 이름이 겹쳐도 섞이지 않는다.
    /// </summary>
    private string KeyOf(RectTransform rect)
    {
        if (_keyByRect.TryGetValue(rect, out string existing))
            return existing;

        string key = rect.name;

        if (_rectByKey.TryGetValue(key, out RectTransform other) && other != null && other != rect)
        {
            // 같은 이름의 다른 rect. 종전 구현은 참조를 키로 써서 안 섞였다 —
            // 그 성질을 유지하되, 조용히 넘어가지 않고 알린다.
            key = $"{rect.name}#{rect.GetInstanceID()}";

            Debug.LogWarning(
                $"[CharacterPlacementTargetLedger] 이름이 겹치는 노드가 있다: '{rect.name}'. " +
                $"참조로 구분해 '{key}'로 다룬다.", rect);
        }

        _keyByRect[rect] = key;
        _rectByKey[key] = rect;

        return key;
    }

    private static Vec2 ToVec2(Vector2 v) => new(v.x, v.y);
    private static Vec3 ToVec3(Vector3 v) => new(v.x, v.y, v.z);
}
