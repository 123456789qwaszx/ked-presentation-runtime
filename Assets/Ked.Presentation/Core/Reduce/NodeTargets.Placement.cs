using System.Collections.Generic;

namespace Ked.Presentation.Core
{
    /// <summary>
    /// 화면 지점 프리셋의 rig-space 좌표 — 프레임 크기 × 비율. 이 표가 사는 유일한 자리다
    /// (샘플의 격자 오버레이 ScreenFocusGridOverlay가 같은 값을 그린다).
    ///
    /// 주의: 이 비율은 게임별 값이므로 원칙적으로는 tuning 데이터가 맞다(target-boundary.md 백로그).
    /// </summary>
    public static class ScreenPointRatios
    {
        public const float OuterXRatio = 0.24f;
        public const float OuterYRatio = 0.16f;
        public const float InnerXRatio = 0.14f;
        public const float InnerYRatio = 0.09f;

        /// <summary>지점 이름 → rig-space 좌표. 모르면 false.</summary>
        public static bool TryResolve(Vec2 frameSize, string pointName, out Vec2 point)
        {
            float ox = frameSize.X * OuterXRatio;
            float oy = frameSize.Y * OuterYRatio;
            float ix = frameSize.X * InnerXRatio;
            float iy = frameSize.Y * InnerYRatio;

            switch ((pointName ?? "").Trim().ToLowerInvariant())
            {
                case "center": point = Vec2.Zero; return true;

                case "left": point = new Vec2(-ox, 0f); return true;
                case "right": point = new Vec2(ox, 0f); return true;
                case "top": point = new Vec2(0f, oy); return true;
                case "bottom": point = new Vec2(0f, -oy); return true;

                case "tl": point = new Vec2(-ox, oy); return true;
                case "tr": point = new Vec2(ox, oy); return true;
                case "bl": point = new Vec2(-ox, -oy); return true;
                case "br": point = new Vec2(ox, -oy); return true;

                case "inner_tl": point = new Vec2(-ix, iy); return true;
                case "inner_tr": point = new Vec2(ix, iy); return true;
                case "inner_bl": point = new Vec2(-ix, -iy); return true;
                case "inner_br": point = new Vec2(ix, -iy); return true;

                default: point = Vec2.Zero; return false;
            }
        }
    }

    /// <summary>
    /// focus 로컬 오프셋 해석 — 런타임 CharacterFocusTuningDBSO.ResolveOffset과 같은 합:
    ///   base[preset] + entry(character).defaultOffset + entry.offsets[preset] + 커맨드 오프셋
    ///
    /// 주의: facing 미러 축은 아직 폴드에 없다(기본 Right = 항등).
    /// mirror 커맨드를 쓰는 장면은 불일치로 드러난다 — 그때 facing 축을 폴드에 올린다.
    /// </summary>
    public static class FocusOffsetMath
    {
        public static Vec2 Resolve(
            FocusTuningBodyDto tuning,
            string characterKey,
            string presetName,
            Vec2 commandOffset)
        {
            Vec2 offset = Vec2.Zero;

            if (tuning?.baseOffsets != null && tuning.baseOffsets.TryGet(presetName, out Vec2 baseOffset))
                offset += baseOffset;

            if (tuning != null && characterKey != null && tuning.TryGetEntry(characterKey, out FocusEntryDto entry))
            {
                if (entry.defaultOffset != null)
                    offset += entry.defaultOffset.ToVec2();

                if (entry.offsets != null && entry.offsets.TryGet(presetName, out Vec2 presetOffset))
                    offset += presetOffset;
            }

            return offset + commandOffset;
        }
    }

    public static partial class NodeTargets
    {
        /// <summary>측정 사슬의 끝 — focus를 재는 노드.</summary>
        public const string FocusMeasureNodeId = "CharacterPortrait_VisualOffset";

        // ── 초점을 화면 지점에 (보조 계산) ───────────────────────
        //
        // 커맨드가 아니다 — 도구의 보조 버튼이 부르는 계산이다(docs/PLAN_command-prune.md P4).
        // 지금 무대에서 그 focus 지점이 그 화면 지점에 오려면 자리 축(CharSlot_Track — move_by가 쓰는 노드)이
        // 어디여야 하는가를 낸다. 버튼은 이 끝값과 지금 값의 차를 move_by로 적는다 — 대본에는 풀린 값만 남는다.
        // 무대 지점은 카메라가 중립일 때 그 화면 지점과 겹치는 무대 위 자리다. 이 노드 트리에는 카메라 ·
        // 깊이 층 프레이밍 노드가 없다 — 그것이 곧 카메라 중립이다.

        /// <summary>자리 축 — move_by가 쓰는 노드(브리지: CharacterRigNode.CharSlot_Track).</summary>
        public const string PlaceMoveNodeId = "CharSlot_Track";

        public static bool TryPlaceFocus(
            StageState state,
            string slotKey,
            string characterKey,
            string focusPresetName,
            string screenPointName,
            FocusTuningBodyDto focusTuning,
            out NodeTarget target,
            out string reason)
        {
            target = default;

            if (!ScreenPointRatios.TryResolve(state.Nodes.RootSpace.Size, screenPointName, out Vec2 desired))
            {
                reason = $"화면 지점 '{screenPointName}'을 모른다";
                return false;
            }

            Vec2 focusOffset = FocusOffsetMath.Resolve(focusTuning, characterKey, focusPresetName, Vec2.Zero);

            string measureKey = StageState.NodeKeyOf(slotKey, FocusMeasureNodeId);
            string moveKey = StageState.NodeKeyOf(slotKey, PlaceMoveNodeId);

            List<string> chainKeys = new();
            RectNodeState[] chain = state.Nodes.BuildChainTo(measureKey, chainKeys);

            int moveIndex = chainKeys.IndexOf(moveKey);

            if (moveIndex < 0)
            {
                reason = $"이동 축 '{moveKey}'가 측정 체인에 없다";
                return false;
            }

            Vec2 solved = SettledFocusMath.SolveFocusPlacement(
                chain,
                state.Nodes.RootSpace,
                moveIndex,
                focusOffset,
                desired,
                state.Nodes.GetState(moveKey).AnchoredPosition);

            target = NodeTarget.AnchoredPosition(moveKey, solved);
            reason = null;
            return true;
        }

        // ── size (depth) ────────────────────────────────────────
        //
        // size 계열 — depth 레벨의 값을 그대로 놓는다. DepthY 위치 + DepthScale 배율 두 장.
        // 무대를 재지 않는다 — 커브가 낸 값이 곧 목표다. 크기를 바꾸며 어느 지점을 제자리에 두려면
        // 그 보정은 자리 명령으로 따로 적는다(도구의 일 — docs/PLAN_command-prune.md P3).

        public const string DepthYNodeId = "CharSlot_DepthY";
        public const string DepthScaleNodeId = "CharSlot_DepthScale";

        public static bool TrySetDepth(
            string slotKey,
            string depthToken,
            DepthLevelTuningDto depthLevel,
            out NodeTarget depthYTarget,
            out NodeTarget depthScaleTarget,
            out string reason)
        {
            depthYTarget = default;
            depthScaleTarget = default;

            // 깊이의 진실은 level 커브 하나다. 토큰은 숫자이거나 라벨이고,
            // 라벨은 커브 위에서 쓸 수치를 고르는 이름표일 뿐이다(DepthLevelLabels).
            // 그래서 재생·정지 프레임·툴 프리뷰가 같은 커브 한 장을 지난다.
            if (!NumberToken.TryParseFloat(depthToken, out float level) &&
                !DepthLevelLabels.TryGetLevel(depthToken, out level))
            {
                reason = $"depth 토큰 '{depthToken}'을 모른다 (숫자 레벨도, 라벨도 아니다)";
                return false;
            }

            if (depthLevel == null ||
                !depthLevel.TryResolve(level, out float levelY, out float levelScale))
            {
                reason = $"depth 레벨 {level}을 풀 커브가 tuning에 없다";
                return false;
            }

            depthYTarget = NodeTarget.AnchoredPosition(
                StageState.NodeKeyOf(slotKey, DepthYNodeId), new Vec2(0f, levelY));

            depthScaleTarget = NodeTarget.LocalScaleXY(
                StageState.NodeKeyOf(slotKey, DepthScaleNodeId), new Vec2(levelScale, levelScale));
            reason = null;
            return true;
        }
    }
}