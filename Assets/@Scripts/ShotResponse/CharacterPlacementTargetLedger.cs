using System.Collections.Generic;
using UnityEngine;

// 측정 직전, 움직이는 상위 노드들을
// - 예약된 최종 target 값에 도착했다고 가정하여 값을 세팅하고,
// - focus point의 world position을 잠깐 측정하고
// - 즉시 원복
public sealed class CharacterPlacementTargetLedger
{
    private enum TargetKind
    {
        AnchoredPosition,
        LocalScale,
        LocalEuler,
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

    private readonly List<RectTransform> _scratchNodes = new(16);
    private readonly List<Entry> _scratchSaved = new(16);

    public void PublishAnchoredPosition(RectTransform node, Vector2 targetAnchoredPosition)
    {
        _targets[node] = new Entry(
            TargetKind.AnchoredPosition,
            new Vector3(targetAnchoredPosition.x, targetAnchoredPosition.y, 0f));
    }

    public void PublishLocalScale(RectTransform node, Vector2 targetLocalScaleXY)
    {
        _targets[node] = new Entry(
            TargetKind.LocalScale,
            new Vector3(targetLocalScaleXY.x, targetLocalScaleXY.y, 0f));
    }

    public void PublishLocalEuler(RectTransform node, Vector3 targetLocalEuler)
    {
        _targets[node] = new Entry(TargetKind.LocalEuler, targetLocalEuler);
    }

    public void Clear(RectTransform node)
    {
        _targets.Remove(node);
    }

    public Vector3 MeasureSettledWorldPoint(
        RectTransform measureRect,
        Vector3 localOffset,
        RectTransform stopRoot)
    {
        if (_targets.Count == 0)
            return measureRect.TransformPoint(localOffset);

        _scratchNodes.Clear();
        _scratchSaved.Clear();

        // 1) measureRect에서 stopRoot까지 부모를 타고 올라가며, target 값이 게시된 RectTransform을 찾은 뒤,
        // 찾은 노드들의 현재 값을 저장하고,잠깐 target 값으로 바꿈.
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

        // 2) 그 상태에서 measureRect.TransformPoint(localOffset)을 측정.
        Vector3 settledWorld = measureRect.TransformPoint(localOffset);

        // 3) 측정이 끝나면 즉시 원래 값으로 복원.
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
                Vector3 s = rect.localScale;
                rect.localScale = new Vector3(entry.value.x, entry.value.y, s.z);
                break;

            case TargetKind.LocalEuler:
                rect.localEulerAngles = entry.value;
                break;
        }
    }
}