using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

// Character placement 계열 커맨드들이 "도달할 최종 anchoredPosition"을 게시.
//
// - FocusPoint 계열 solver가 라이브 transform만 보고 계산하지 않게 한다.
// - 움직이는 placement 노드가 있으면, 현재 focus point에 "아직 남은 이동량"을 더해
//   settled 상태의 focus point를 예측한다.
// - SetAnchor / PlaceTo / PlaceCharacterFocus 같은 composition placement만 게시.
public sealed class CharacterPlacementTargetLedger
{
    private const float ResidualSqrEpsilon = 0.0001f;

    private readonly Dictionary<RectTransform, Vector2> _settledTargets = new();

    public void Publish(RectTransform node, Vector2 settledAnchoredPosition)
    {
        if (node == null)
            return;

        _settledTargets[node] = settledAnchoredPosition;
    }

    public void Clear(RectTransform node)
    {
        if (node == null)
            return;

        _settledTargets.Remove(node);
    }

    public void ClearAll()
    {
        _settledTargets.Clear();
    }

    public bool TryGetTarget(RectTransform node, out Vector2 settledAnchoredPosition)
    {
        settledAnchoredPosition = default;

        if (node == null)
            return false;

        return _settledTargets.TryGetValue(node, out settledAnchoredPosition);
    }

    // measureRect의 상위 계층 중 현재 tween 중인 placement target들의 잔여 이동량을 world vector로 합산.
    //
    // 예:
    // CharSlot_Anchor가 place_to로 이동 중이고,
    // CharSlot_Size가 그 하위에 있으면,
    // CharSlot_Size.TransformPoint(...)로 얻은 focusWorld는 아직 중간 위치.
    //
    // 이 때,
    // CharSlot_Anchor.targetAnchoredPosition - CharSlot_Anchor.anchoredPosition을 world vector로 변환해서 더하면,
    // "CharSlot_Anchor가 최종 위치에 도착했을 때의 focusWorld"가 됨.
    public Vector3 AccumulateResidualWorldDisplacement(
        RectTransform measureRect,
        RectTransform stopRoot)
    {
        Vector3 residualWorld = Vector3.zero;

        if (measureRect == null || _settledTargets.Count == 0)
            return residualWorld;

        Transform current = measureRect;

        while (current != null && current != stopRoot)
        {
            if (current is RectTransform rect &&
                _settledTargets.TryGetValue(rect, out Vector2 targetAnchoredPosition) &&
                DOTween.IsTweening(rect))
            {
                Vector2 residualLocal =
                    targetAnchoredPosition - rect.anchoredPosition;

                if (residualLocal.sqrMagnitude > ResidualSqrEpsilon)
                {
                    residualWorld += ConvertAnchoredDeltaToWorldVector(
                        rect,
                        residualLocal);
                }
            }

            current = current.parent;
        }

        return residualWorld;
    }

    private static Vector3 ConvertAnchoredDeltaToWorldVector(
        RectTransform rect,
        Vector2 anchoredDelta)
    {
        if (rect == null)
            return Vector3.zero;

        RectTransform parent = rect.parent as RectTransform;

        Vector3 localVector = new(anchoredDelta.x, anchoredDelta.y, 0f);

        if (parent == null)
            return localVector;

        // anchoredPosition의 delta는 parent local 기준의 translation vector를 봄.
        // 점 변환이 아니라 vector 변환이므로 translation 성분은 섞지 않음.
        return parent.TransformVector(localVector);
    }
}