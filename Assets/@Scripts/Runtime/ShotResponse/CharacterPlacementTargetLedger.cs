using System.Collections.Generic;
using Ked.Presentation.Core;
using UnityEngine;

// "트윈이 다 끝났다면 어디에 있을 것인가"의 정착 상태 측정.
//
// U13-b-3에서 계산부가 코어(PlacementTargetLedger + RectChainMath)로 승격됐고,
// 이 클래스는 얇은 어댑터만 남았다:
// - RectTransform ↔ 논리 키(rect.name) 대응
// - 라이브 체인 캡처 (유니티 → RectNodeState)
// - 좌표계 변환 (stopRoot 로컬 ↔ 월드)
//
// 종전의 "부모들을 target 값으로 잠깐 세팅 → 측정 → 복원" 트릭은 사라졌다.
// 측정이 유니티에 아무것도 쓰지 않으므로, 예외가 나도 리그가 더러워지지 않는다.
public sealed class CharacterPlacementTargetLedger
{
    private readonly PlacementTargetLedger _core = new();

    // 참조 → 논리 키. 게시된 노드만 담는다. 키는 rect.name이며
    // (리그 하나당 장부 하나 + 스키마 노드 이름 유일) 충돌하지 않는다 —
    // 만약 충돌하면 조용히 섞이는 대신 아래 RegisterKey가 소리를 낸다.
    private readonly Dictionary<RectTransform, string> _keys = new();
    private readonly Dictionary<string, RectTransform> _byKey = new();

    public void PublishAnchoredPosition(RectTransform node, Vector2 targetAnchoredPosition)
    {
        if (node == null)
            return;

        _core.PublishAnchoredPosition(RegisterKey(node), new Vec2(targetAnchoredPosition.x, targetAnchoredPosition.y));
    }

    public void PublishLocalScale(RectTransform node, Vector2 targetLocalScaleXY)
    {
        if (node == null)
            return;

        _core.PublishLocalScale(RegisterKey(node), new Vec2(targetLocalScaleXY.x, targetLocalScaleXY.y));
    }

    public void PublishLocalEuler(RectTransform node, Vector3 targetLocalEuler)
    {
        if (node == null)
            return;

        _core.PublishLocalEuler(RegisterKey(node), new Vec3(targetLocalEuler.x, targetLocalEuler.y, targetLocalEuler.z));
    }

    public void Clear(RectTransform node)
    {
        if (node != null && _keys.TryGetValue(node, out string key))
            _core.Clear(key);
    }

    /// <summary>
    /// measureRect 로컬 offset이, 예약된 target이 전부 도착했다고 가정할 때
    /// 월드 어디에 오는지. 예약이 없으면 라이브 측정과 동일하다.
    /// </summary>
    public Vector3 MeasureSettledWorldPoint(
        RectTransform measureRect,
        Vector3 localOffset,
        RectTransform stopRoot)
    {
        if (_core.IsEmpty)
            return measureRect.TransformPoint(localOffset);

        RectNodeState[] chain = CaptureSettledChain(measureRect, stopRoot);

        Vec3 stopLocal = RectChainMath.TransformPoint(
            chain,
            SpaceOf(stopRoot),
            new Vec3(localOffset.x, localOffset.y, localOffset.z));

        // stopRoot 위로는 예약이 없으므로 stopRoot의 라이브 포즈로 월드에 올린다.
        return stopRoot.TransformPoint(new Vector3(stopLocal.X, stopLocal.Y, stopLocal.Z));
    }

    /// <summary>
    /// 월드 점이, 예약된 target이 전부 도착한 parentRect의 로컬 어디에 오는지.
    /// </summary>
    public Vector2 WorldPointToSettledParentLocalPoint(
        RectTransform parentRect,
        Vector3 worldPoint,
        RectTransform stopRoot)
    {
        if (parentRect == null)
            return Vector2.zero;

        if (_core.IsEmpty)
        {
            Vector3 liveLocal = parentRect.InverseTransformPoint(worldPoint);
            return new Vector2(liveLocal.x, liveLocal.y);
        }

        RectNodeState[] chain = CaptureSettledChain(parentRect, stopRoot);

        Vector3 stopLocalUnity = stopRoot.InverseTransformPoint(worldPoint);

        Vec3 local = RectChainMath.InverseTransformPoint(
            chain,
            SpaceOf(stopRoot),
            new Vec3(stopLocalUnity.x, stopLocalUnity.y, stopLocalUnity.z));

        return new Vector2(local.X, local.Y);
    }

    // ── 체인 캡처 ────────────────────────────────────────────────────

    /// <summary>
    /// stopRoot(제외)부터 endRect(포함)까지의 라이브 상태에 예약 target을 입힌 사슬.
    /// 루트 쪽이 앞이다(RectChainMath 규약).
    /// chainRects를 주면 같은 순서로 실제 RectTransform을 담아 준다 —
    /// 호출자가 특정 노드의 체인 인덱스를 찾을 때 쓴다(depth/focus solver).
    /// </summary>
    public RectNodeState[] CaptureSettledChain(
        RectTransform endRect,
        RectTransform stopRoot,
        List<RectTransform> chainRects = null)
    {
        List<RectNodeState> reversed = new List<RectNodeState>(16);
        List<RectTransform> reversedRects = chainRects != null ? new List<RectTransform>(16) : null;

        Transform current = endRect;

        while (current != null && current != stopRoot)
        {
            if (current is not RectTransform rect)
            {
                // 종전 구현은 일반 Transform을 그냥 지나쳤지만, 순수 앵커 수학은
                // RectTransform이어야 한다. 이 게임의 리그 체인은 전부 RectTransform이라
                // 도달하지 않는 경로다 — 도달하면 조용히 어긋나는 대신 알린다.
                throw new System.ArgumentException(
                    $"[CharacterPlacementTargetLedger] '{current.name}'는 RectTransform이 아니라 " +
                    "정착 계산을 할 수 없다.");
            }

            RectNodeState live = CaptureLive(rect);

            reversed.Add(_keys.TryGetValue(rect, out string key)
                ? _core.ApplyTo(key, live)
                : live);

            reversedRects?.Add(rect);

            current = current.parent;
        }

        if (current == null)
        {
            throw new System.ArgumentException(
                $"[CharacterPlacementTargetLedger] stopRoot '{(stopRoot != null ? stopRoot.name : "null")}'가 " +
                $"'{endRect.name}'의 조상이 아니다.");
        }

        RectNodeState[] chain = new RectNodeState[reversed.Count];

        for (int i = 0; i < chain.Length; i++)
            chain[i] = reversed[chain.Length - 1 - i];

        if (chainRects != null)
        {
            chainRects.Clear();

            for (int i = 0; i < reversedRects.Count; i++)
                chainRects.Add(reversedRects[reversedRects.Count - 1 - i]);
        }

        return chain;
    }

    private static RectNodeState CaptureLive(RectTransform rect)
    {
        Vector2 anchoredPosition = rect.anchoredPosition;
        Vector2 anchorMin = rect.anchorMin;
        Vector2 anchorMax = rect.anchorMax;
        Vector2 pivot = rect.pivot;
        Vector2 sizeDelta = rect.sizeDelta;
        Vector3 localScale = rect.localScale;
        Vector3 localEuler = rect.localEulerAngles;

        return new RectNodeState(
            anchoredPosition: new Vec2(anchoredPosition.x, anchoredPosition.y),
            anchorMin: new Vec2(anchorMin.x, anchorMin.y),
            anchorMax: new Vec2(anchorMax.x, anchorMax.y),
            pivot: new Vec2(pivot.x, pivot.y),
            sizeDelta: new Vec2(sizeDelta.x, sizeDelta.y),
            localScale: new Vec3(localScale.x, localScale.y, localScale.z),
            localEulerAngles: new Vec3(localEuler.x, localEuler.y, localEuler.z));
    }

    /// <summary>stopRoot의 rect를 코어 좌표 공간으로. depth/focus solver도 같은 규약을 쓴다.</summary>
    public static RectSpace SpaceOf(RectTransform stopRoot)
    {
        Vector2 size = stopRoot.rect.size;
        Vector2 pivot = stopRoot.pivot;

        return new RectSpace(new Vec2(size.x, size.y), new Vec2(pivot.x, pivot.y));
    }

    private string RegisterKey(RectTransform node)
    {
        if (_keys.TryGetValue(node, out string existing))
            return existing;

        string key = node.name;

        if (_byKey.TryGetValue(key, out RectTransform other) && other != node && other != null)
        {
            // 같은 이름의 다른 노드가 이미 게시 중이다. 리그 하나당 장부 하나라
            // 정상 경로에서는 없다 — 섞이는 대신 키를 가르고 알린다.
            key = $"{key}#{node.GetInstanceID()}";

            Debug.LogWarning(
                $"[CharacterPlacementTargetLedger] 노드 이름 충돌: '{node.name}'. " +
                $"'{key}'로 갈라 게시한다. 리그 구성을 확인할 것.");
        }

        _keys[node] = key;
        _byKey[key] = node;

        return key;
    }
}
