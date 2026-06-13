using System.Collections.Generic;
using UnityEngine;

// placement 계열 커맨드가 "도달할 최종 transform 값(settled target)"을 게시.
// focus/placement solver가 라이브 transform이 아니라 정착 상태 기준으로 풀 수 있게 한다.
//
// 메커니즘: 측정 직전, 움직이는 조상 노드들을 "잠깐 target 값으로 세팅 → 측정 → 즉시 원복".
//   - 동기 호출이라 프레임 경계가 없어 시각적 부작용이 없다.
//   - Unity 실제 계층 변환을 쓰므로 translation/scale/rotation, 다중 노드 합성이 전부 정확하다.
//   - translation의 경우 기존 "잔여 world 벡터 더하기"와 결과가 동일하다(회귀 없음).
public sealed class CharacterPlacementTargetLedger
{
    private enum TargetKind
    {
        AnchoredPosition,
        LocalScale,
        LocalEuler, // 회전도 동일 메커니즘으로 준비. publisher만 연결하면 즉시 동작.
    }

    private readonly struct Entry
    {
        public readonly TargetKind kind;
        public readonly Vector3 value;

        public Entry(TargetKind kind, Vector3 value)
        {
            this.kind = kind;
            this.value = value;
        }
    }

    private readonly Dictionary<RectTransform, Entry> _targets = new();

    // 재사용 스크래치(할당 회피). 메인 스레드 / 비재진입 전제.
    private readonly List<RectTransform> _scratchNodes = new(16);
    private readonly List<Entry> _scratchSaved = new(16);

    // 호환: 기존 호출부의 Publish(node, Vector2)는 anchoredPosition 게시를 의미한다.
    public void Publish(RectTransform node, Vector2 targetAnchoredPosition)
        => PublishAnchoredPosition(node, targetAnchoredPosition);

    public void PublishAnchoredPosition(RectTransform node, Vector2 targetAnchoredPosition)
    {
        if (node == null)
            return;

        _targets[node] = new Entry(
            TargetKind.AnchoredPosition,
            new Vector3(targetAnchoredPosition.x, targetAnchoredPosition.y, 0f));
    }

    public void PublishLocalScale(RectTransform node, Vector2 targetLocalScaleXY)
    {
        if (node == null)
            return;

        _targets[node] = new Entry(
            TargetKind.LocalScale,
            new Vector3(targetLocalScaleXY.x, targetLocalScaleXY.y, 0f));
    }

    public void PublishLocalEuler(RectTransform node, Vector3 targetLocalEuler)
    {
        if (node == null)
            return;

        _targets[node] = new Entry(TargetKind.LocalEuler, targetLocalEuler);
    }

    public void Clear(RectTransform node)
    {
        if (node == null)
            return;

        _targets.Remove(node);
    }

    public void ClearAll()
    {
        _targets.Clear();
    }

    // measureRect.TransformPoint(localOffset)을, 조상 체인의 "tween 중 + target 게시" 노드를
    // 일시적으로 target 값으로 세팅한 상태에서 측정해 돌려준다. 측정 후 즉시 원복한다.
    public Vector3 MeasureSettledWorldPoint(
        RectTransform measureRect,
        Vector3 localOffset,
        RectTransform stopRoot)
    {
        if (measureRect == null)
            return Vector3.zero;

        if (_targets.Count == 0)
            return measureRect.TransformPoint(localOffset);

        _scratchNodes.Clear();
        _scratchSaved.Clear();

        // 1) 보정 대상 수집 + 라이브 값 백업 + target 적용.
        Transform current = measureRect;

        while (current != null && current != stopRoot)
        {
            if (current is RectTransform rect && _targets.TryGetValue(rect, out Entry entry))
            {
                _scratchNodes.Add(rect);
                _scratchSaved.Add(CaptureLive(rect, entry.kind));
                ApplyEntry(rect, entry);
            }

            current = current.parent;
        }

        // 2) settled 상태에서 측정.
        Vector3 settledWorld = measureRect.TransformPoint(localOffset);

        // 3) 라이브 값 원복(적용 역순).
        for (int i = _scratchNodes.Count - 1; i >= 0; i--)
            ApplyEntry(_scratchNodes[i], _scratchSaved[i]);

        _scratchNodes.Clear();
        _scratchSaved.Clear();

        return settledWorld;
    }

    private static Entry CaptureLive(RectTransform rect, TargetKind kind)
    {
        switch (kind)
        {
            case TargetKind.AnchoredPosition:
                Vector2 ap = rect.anchoredPosition;
                return new Entry(kind, new Vector3(ap.x, ap.y, 0f));

            case TargetKind.LocalScale:
                return new Entry(kind, rect.localScale);

            case TargetKind.LocalEuler:
                return new Entry(kind, rect.localEulerAngles);

            default:
                return new Entry(kind, Vector3.zero);
        }
    }

    private static void ApplyEntry(RectTransform rect, Entry entry)
    {
        switch (entry.kind)
        {
            case TargetKind.AnchoredPosition:
                rect.anchoredPosition = new Vector2(entry.value.x, entry.value.y);
                break;

            case TargetKind.LocalScale:
                // target은 xy만 지정. z는 라이브 값을 유지한다(원복 시에도 z 불변).
                Vector3 s = rect.localScale;
                rect.localScale = new Vector3(entry.value.x, entry.value.y, s.z);
                break;

            case TargetKind.LocalEuler:
                rect.localEulerAngles = entry.value;
                break;
        }
    }
}