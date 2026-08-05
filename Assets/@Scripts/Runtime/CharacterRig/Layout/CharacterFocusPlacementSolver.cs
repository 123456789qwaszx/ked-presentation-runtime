using System.Collections.Generic;
using UnityEngine;

// place — 캐릭터의 focus 지점을 화면의 지정 지점으로 보내는 이동 목표값을 푼다.
//
// U13-b-5: 계산부가 코어(SettledFocusMath.SolveFocusPlacement)로 승격됐다.
// 여기는 입력을 모으는 어댑터다: 정착 체인 캡처 + 화면 지점 해석 + facing/튜닝 오프셋.
// 유니티에는 아무것도 쓰지 않는다.
public static class CharacterFocusPlacementSolver
{
    public static bool TryCalculateFocusPlacement(
        CommandRunScope scope,
        IShotResponseStageProvider stageProvider,
        string roleKey,
        RectTransform moveRect,
        CharacterFocusPreset focusPreset,
        Vector2 focusOffset,
        CharacterFocusTuningDBSO focusTuningDb,
        ScreenFocusPoint screenPoint,
        Vector2 screenOffset,
        out Vector2 destinationAnchoredPosition)
    {
        destinationAnchoredPosition = default;

        if (moveRect == null)
            return false;

        RectTransform rigSpaceRoot = stageProvider?.RigSpaceRoot;

        if (rigSpaceRoot == null)
            return false;

        string resolvedRigKey = CharacterRigTargetResolver.ResolveRigKeyByPolicy(scope, roleKey);
        scope.CharacterRigs.TryGetRig(resolvedRigKey, out CharacterRigRefs rigRefs);
        string tuningKey = CharacterRigTargetResolver.ResolveCharacterKeyFromTargetKey(scope, roleKey);

        if (!CharacterFocusPointResolver.TryResolveFocusMeasureInputs(
                rigRefs,
                tuningKey,
                focusPreset,
                focusOffset,
                focusTuningDb,
                CharacterFocusPointResolver.ResolveFacing(scope, roleKey),
                mirrorCommandOffset: true,
                out RectTransform measureRect,
                out Vector3 focusLocalOffset))
        {
            return false;
        }

        // 원하는 지점은 rig-space 화면 좌표다 (화면 지점 프리셋 + 오프셋).
        Vector2 desiredFocusInRigSpace =
            ScreenFocusPointResolver.Resolve(rigSpaceRoot, screenPoint) + screenOffset;

        List<RectTransform> chainRects = new();

        Ked.Presentation.Core.RectNodeState[] chain =
            rigRefs.PlacementTargets.CaptureSettledChain(measureRect, rigSpaceRoot, chainRects);

        int moveIndex = chainRects.IndexOf(moveRect);

        if (moveIndex < 0)
        {
            Debug.LogWarning(
                $"[CharacterFocusPlacementSolver] 이동 노드 '{moveRect.name}'가 " +
                "focus 측정 체인의 조상이 아니다. 배치를 건너뛴다 — 리그 구성을 확인할 것.");
            return false;
        }

        Ked.Presentation.Core.Vec2 solved =
            Ked.Presentation.Core.SettledFocusMath.SolveFocusPlacement(
                chain,
                CharacterPlacementTargetLedger.SpaceOf(rigSpaceRoot),
                moveIndex,
                new Ked.Presentation.Core.Vec2(focusLocalOffset.x, focusLocalOffset.y),
                new Ked.Presentation.Core.Vec2(desiredFocusInRigSpace.x, desiredFocusInRigSpace.y),
                new Ked.Presentation.Core.Vec2(moveRect.anchoredPosition.x, moveRect.anchoredPosition.y));

        destinationAnchoredPosition = new Vector2(solved.X, solved.Y);
        return true;
    }
}
