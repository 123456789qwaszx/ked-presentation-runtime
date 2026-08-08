using System;
using System.Collections.Generic;

namespace Ked.Presentation.Core
{
    /// <summary>리듀서에 주입되는 게임별 값 (게임 데이터는 코드가 아니라 tuning 인자).</summary>
    public sealed class StageReducerTuning
    {
        /// <summary>ExportedTuning/rig-schemas.json. slot 폴드가 캐릭터 리그를 여기서 세운다.</summary>
        public RigSchemasFileDto RigSchemas;

        /// <summary>기준 해상도 가로 폭. 1u 환산의 유일한 입력.</summary>
        public float ReferenceStageWidth;

        /// <summary>기준 해상도. 무대 루트 공간의 크기다.</summary>
        public Vec2 BaseResolution;

        /// <summary>
        /// presets/role-anchor.json — show/set_anchor의 캐릭터별 앵커.
        /// 없으면(null) show가 Default로 접되 Unhandled로 소리를 낸다 —
        /// 실측 덤프에 비기본값 엔트리가 있으므로(tyrant·Amber) 침묵하면 그 장면이 어긋난다.
        /// </summary>
        public RoleAnchorTuningBodyDto RoleAnchors;
    }

    /// <summary>
    /// 커맨드 열 → 확정 무대 상태.
    ///
    /// Apply(state, command, tuning) → state — 순수 함수다: 입력 상태를 바꾸지 않고
    /// 새 상태를 돌려준다. 시간·랜덤·IO 없음. duration 인자는 정지 프레임에 무의미하므로
    /// 파싱조차 하지 않는다.
    ///
    /// Unhandled 규율 — 네 갈래를 전부 잡는다. 조용히 버리지 않는다:
    ///   ① 모르는 커맨드   ② 잘못된 인자   ③ 미스폰 슬롯   ④ 폴드 중 예외
    /// 이 기록이 "아직 못 접는 것"의 유일한 진실이고, 수렴의 작업 목록이 된다.
    ///
    /// 현재 어휘(v1): slot 계열(리그 스폰) · cast/actor(배역·별칭) ·
    ///   show(리셋+앵커+가시성 — 표정/초상 축은 Unhandled 기록) · fade_in/out ·
    ///   nudge/move/scale/rotate 계열 · shot 4종(+focus_to는 명시적 Unhandled) · char_to 계열.
    /// 명시적 한계: place/size 계열(focus·depth 튜닝 배선 전), 초기 alpha(덤프 확장 전),
    ///   배경/오버레이/이펙트/오디오/트랜지션, 절차적 연기 커맨드(목표값이 정의되지 않음).
    ///
    /// ⚠ v1 가정 — 등가성 하네스가 판정한다:
    /// 1. 스테이지/레이어 컨테이너는 항등 트랜스폼(스트레치 풀)이라 리그를 루트 직속으로
    ///    세워도 좌표가 같다. 컨테이너 스키마는 덤프에 없다 — 어긋나면 덤프에 추가한다.
    /// 2. 커맨드 기본값은 브리지 시그니처에서 옮겨 박았다(각 case의 주석 참조).
    ///    카탈로그가 데이터가 되면 tuning으로 옮긴다.
    /// </summary>
    public static class StageReducer
    {
        public static StageState CreateInitialState(StageReducerTuning tuning)
        {
            Require(tuning);
            return new StageState(new RectSpace(tuning.BaseResolution, Vec2.Half));
        }

        public static StageState Apply(StageState state, in StageCommand command, StageReducerTuning tuning)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            Require(tuning);

            StageState next = state.Clone();

            try
            {
                if (!TryApply(next, command, tuning, out string reason))
                    next.AddUnhandled(command, reason);
            }
            catch (Exception ex)
            {
                // 접다가 터진 커맨드도 침묵 대신 기록이다. 예외로 폴드가 멈추면
                // 그 뒤 라인을 하나도 판정할 수 없다. 상태는 클론이라 원본은 무사하다.
                next.AddUnhandled(command, $"폴드 중 예외: {ex.Message}");
            }

            return next;
        }

        public static StageState ApplyAll(
            StageState state, IEnumerable<StageCommand> commands, StageReducerTuning tuning)
        {
            if (commands == null)
                throw new ArgumentNullException(nameof(commands));

            foreach (StageCommand command in commands)
                state = Apply(state, command, tuning);

            return state;
        }

        // ── 디스패치 ─────────────────────────────────────────────────

        private static bool TryApply(
            StageState state, in StageCommand cmd, StageReducerTuning tuning, out string reason)
        {
            reason = null;

            switch (cmd.Name)
            {
                // 슬롯 스폰. 브리지: slot(slotKey, stage="stage00", layer="mid") / slotNN(slotKey, layer="mid")
                case "slot":   return ApplySlot(state, cmd, tuning, cmd.Arg(1, "stage00"), cmd.Arg(2, "mid"), out reason);
                case "slot00": return ApplySlot(state, cmd, tuning, "stage00", cmd.Arg(1, "mid"), out reason);
                case "slot01": return ApplySlot(state, cmd, tuning, "stage01", cmd.Arg(1, "mid"), out reason);
                case "slot02": return ApplySlot(state, cmd, tuning, "stage02", cmd.Arg(1, "mid"), out reason);

                // 배역·별칭 — 커맨드 대상 해석(TryResolveSlot)의 전제.
                case "cast": return ApplyCast(state, cmd, out reason);
                case "actor": return ApplyActor(state, cmd, out reason);

                // show — SetAnchor(리셋+역할 앵커) + 가시성. 표정/초상 축은 Unhandled 기록.
                case "show": return ApplyShow(state, cmd, tuning, out reason);

                // fade — 브리지와 같은 표적(CharacterPortraitSprite_Root).
                case "fade_in": return ApplyFade(state, cmd, visible: true, out reason);
                case "fade_out": return ApplyFade(state, cmd, visible: false, out reason);

                // nudge (unitToken 필수 — 브리지에 기본값이 없다)
                case "left":  return ApplyNudge(state, cmd, tuning, -1f, 0f, "CharSlot_Track_X", out reason);
                case "right": return ApplyNudge(state, cmd, tuning, 1f, 0f, "CharSlot_Track_X", out reason);
                case "up":    return ApplyNudge(state, cmd, tuning, 0f, 1f, "CharSlot_Track_Y", out reason);
                case "down":  return ApplyNudge(state, cmd, tuning, 0f, -1f, "CharSlot_Track_Y", out reason);

                // move-per-frame (거리 = 1u × 프레임 수. 브리지: ParseFrames(token, 폴백 8), 기본 "1fr")
                case "left_per":  return ApplyMovePer(state, cmd, tuning, -1f, 0f, "CharSlot_Track_X", out reason);
                case "right_per": return ApplyMovePer(state, cmd, tuning, 1f, 0f, "CharSlot_Track_X", out reason);
                case "up_per":    return ApplyMovePer(state, cmd, tuning, 0f, 1f, "CharSlot_Track_Y", out reason);
                case "down_per":  return ApplyMovePer(state, cmd, tuning, 0f, -1f, "CharSlot_Track_Y", out reason);

                // staging 이동/스케일/회전
                case "move_by":      return ApplyMoveBy(state, cmd, tuning, out reason);
                case "move_reset":   return ApplyMoveReset(state, cmd, out reason);
                case "scale_by":     return ApplyScaleBy(state, cmd, out reason);
                case "scale_reset":  return ApplyScaleReset(state, cmd, out reason);
                case "rotate_by":    return ApplyRotateBy(state, cmd, out reason);
                case "rotate_reset": return ApplyRotateReset(state, cmd, out reason);

                // shot
                case "shot_zoom":  return ApplyShotZoom(state, cmd, out reason);
                case "shot_to":    return ApplyShotTo(state, cmd, tuning, out reason);
                case "shot_track": return ApplyShotTrack(state, cmd, tuning, out reason);
                case "shot_reset":
                    state.Shot = ShotResetReduction.Reduce();
                    return true;

                case "shot_focus_to":
                    // 리덕션(ShotZoomFocusReduction)은 이미 있다. 없는 것은 입력이다 —
                    // 정착 focus 측정에 focus 튜닝(오프셋)과 화면 지점표가 필요한데 아직
                    // tuning에 배선되지 않았다. 명시적으로 남겨야 "리덕션은 있는데 디스패치를
                    // 빼먹는" 실수와 구분된다.
                    reason = "focus 튜닝·화면 지점표가 아직 tuning에 배선되지 않았다";
                    return false;

                // 구조 축 (v1: 부착 기록만 — 컨테이너 항등 가정으로 좌표 영향 없음)
                case "char_to":    return ApplyCharTo(state, cmd, cmd.Arg(1, "stage00"), cmd.Arg(2, "mid"), out reason);
                case "char_to_s0": return ApplyCharTo(state, cmd, "stage00", cmd.Arg(1, "mid"), out reason);
                case "char_to_s1": return ApplyCharTo(state, cmd, "stage01", cmd.Arg(1, "mid"), out reason);
                case "char_to_s2": return ApplyCharTo(state, cmd, "stage02", cmd.Arg(1, "mid"), out reason);

                default:
                    reason = "아직 코어로 이관되지 않은 커맨드";
                    return false;
            }
        }

        // ── 슬롯 ─────────────────────────────────────────────────────

        private static bool ApplySlot(
            StageState state, in StageCommand cmd, StageReducerTuning tuning,
            string stageKey, string layerKey, out string reason)
        {
            if (!TryGetSlotKey(cmd, out string slotKey, out reason))
                return false;

            if (state.HasSlot(slotKey))
            {
                // 같은 슬롯 재선언: 런타임은 기존 리그를 재사용한다. 부착만 갱신한다.
                state.SetAttachment(slotKey, new SlotAttachment(stageKey, layerKey));
                return true;
            }

            if (tuning.RigSchemas == null)
            {
                reason = "tuning에 리그 스키마가 없어 슬롯을 세울 수 없다";
                return false;
            }

            if (!TryFindCharacterRig(tuning.RigSchemas, out RigSchemaRigDto rig))
            {
                reason = "리그 스키마 덤프에 character 리그가 없다";
                return false;
            }

            // v1 가정 1: 컨테이너는 항등이므로 리그를 루트 공간 직속으로 세운다.
            RigSchemaLoader.AddRigTo(state.Nodes, rig, keyPrefix: slotKey + "/");
            state.RegisterSlot(slotKey);
            state.SetAttachment(slotKey, new SlotAttachment(stageKey, layerKey));

            // 초기 가시성: 덤프의 CanvasGroup alpha.
            // 초상·오버레이·이모지 루트는 0으로 태어난다 — 기본값 1로 두면
            // show 전 구간이 전부 어긋난다(첫 리포트의 최대 불일치 클래스였다).
            for (int i = 0; i < rig.nodes.Count; i++)
            {
                RigSchemaNodeDto node = rig.nodes[i];

                if (node != null && node.hasCanvasGroup)
                    state.SetAlpha(StageState.NodeKeyOf(slotKey, node.id), node.canvasGroupAlpha);
            }

            return true;
        }

        private static bool TryFindCharacterRig(RigSchemasFileDto file, out RigSchemaRigDto rig)
        {
            rig = null;

            if (file.rigs == null)
                return false;

            for (int i = 0; i < file.rigs.Count; i++)
            {
                if (file.rigs[i]?.rigKind == "character")
                    rig = file.rigs[i];
            }

            return rig != null;
        }

        // ── 배역·별칭 ────────────────────────────────────────────────

        /// <summary>cast (slot, characterKey, [variant="a"], [emotion="1"]) — 배역만 접는다.</summary>
        private static bool ApplyCast(StageState state, in StageCommand cmd, out string reason)
        {
            if (!TryGetSpawnedSlot(state, cmd, out string slotKey, out reason))
                return false;

            string characterKey = cmd.Arg(1);

            if (string.IsNullOrEmpty(characterKey))
            {
                reason = "cast에 캐릭터 키가 없다";
                return false;
            }

            state.SetCast(slotKey, characterKey);

            // 브리지는 cast를 pose+face로 팬아웃한다(초상 그림·사이징). 그 축은 아직
            // 상태 모델 밖이다 — 배역은 접되 초상 축은 기록으로 남긴다.
            state.AddUnhandled(cmd, "초상 축(변형·표정·사이징)은 아직 상태 모델 밖");

            return true;
        }

        private static bool ApplyActor(StageState state, in StageCommand cmd, out string reason)
        {
            reason = null;

            string aliasSymbol = cmd.Arg(0);
            string targetKey = cmd.Arg(1);

            if (string.IsNullOrEmpty(aliasSymbol) || string.IsNullOrEmpty(targetKey))
            {
                reason = "actor 인자가 모자란다 (별칭, 대상)";
                return false;
            }

            state.SetAlias(aliasSymbol, targetKey);
            return true;
        }

        // ── show · fade ──────────────────────────────────────────────

        // SetAnchorCommandCharR의 리셋 목록(두 플래그 모두 켜진 show 경로).
        // 리그 스키마 지식이라 리덕션이 아니라 여기(디스패치)가 갖는다.
        private static readonly string[] ShowResetPositionIds =
        {
            "CharSlot_Track", "CharSlot_Track_X", "CharSlot_Track_Y",
            "CharacterPortrait_Track", "CharacterPortrait_Track_Move",
            "CharacterPortrait_Track_Move_X", "CharacterPortrait_Track_Move_Y",
            "CharacterPortrait_SwayPivot", "CharacterPortrait_Shake",
        };

        private static readonly string[] ShowResetEulerIds =
        {
            // CharSlot_SwayPivot은 목록에 없다 — rotate_by의 표적인데 show가 되돌리지
            // 않는 것이 런타임 실동작이다(StagingBundleReductionTests가 고정).
            "CharSlot_Rotation",
            "CharacterPortrait_Rotation", "CharacterPortrait_SwayPivot", "CharacterPortrait_Shake",
        };

        private static readonly string[] ShowResetScaleIds =
        {
            "CharSlot_Scale",
            "CharacterPortrait_SwayPivot", "CharacterPortrait_Shake",
            "CharacterPortrait_ActingScale", "CharacterPortrait_ActingScale_X", "CharacterPortrait_ActingScale_Y",
        };

        private static bool ApplyShow(
            StageState state, in StageCommand cmd, StageReducerTuning tuning, out string reason)
        {
            if (!TryGetSpawnedSlot(state, cmd, out string slotKey, out reason))
                return false;

            // 역할 앵커: cast된 캐릭터의 튜닝. 배역이 없거나 엔트리가 없으면 Default —
            // 그게 데이터의 의미다. 튜닝 파일 자체가 없으면 침묵하지 않는다.
            SetAnchorReduction.RoleAnchorTuning anchorTuning = SetAnchorReduction.RoleAnchorTuning.Default;

            if (tuning.RoleAnchors == null)
            {
                state.AddUnhandled(cmd, "role-anchor 튜닝이 tuning에 없다 — 앵커를 기본값으로 접었다");
            }
            else if (state.TryGetCharacter(slotKey, out string characterKey))
            {
                tuning.RoleAnchors.TryGet(characterKey, out anchorTuning);
            }

            StageNodeClaim[] claims = SetAnchorReduction.Reduce(
                StageState.NodeKeyOf(slotKey, "CharacterPortrait_VisualOffset"),
                anchorTuning,
                Prefixed(slotKey, ShowResetPositionIds),
                Prefixed(slotKey, ShowResetEulerIds),
                Prefixed(slotKey, ShowResetScaleIds));

            foreach (StageNodeClaim claim in claims)
                state.Apply(claim);

            // 가시성 두 장: 리그 루트 + 초상 스프라이트 루트.
            state.Apply(FadeInReduction.Reduce(StageState.NodeKeyOf(slotKey, RigSchemaLoader.RootKey)));
            state.Apply(FadeInReduction.Reduce(StageState.NodeKeyOf(slotKey, "CharacterPortraitSprite_Root")));

            // 표정 토큰(faceToken, 기본 "e1")과 초상 사이징은 아직 상태 모델 밖.
            state.AddUnhandled(cmd, "표정·초상 사이징 축은 아직 상태 모델 밖");

            return true;
        }

        private static bool ApplyFade(StageState state, in StageCommand cmd, bool visible, out string reason)
        {
            if (!TryGetSpawnedSlot(state, cmd, out string slotKey, out reason))
                return false;

            string nodeKey = StageState.NodeKeyOf(slotKey, "CharacterPortraitSprite_Root");

            state.Apply(visible
                ? FadeInReduction.Reduce(nodeKey)
                : FadeOutReduction.Reduce(nodeKey));

            return true;
        }

        // ── 이동/스케일/회전 ─────────────────────────────────────────

        private static bool ApplyNudge(
            StageState state, in StageCommand cmd, StageReducerTuning tuning,
            float xSign, float ySign, string targetId, out string reason)
        {
            if (!TryGetSpawnedSlot(state, cmd, out string slotKey, out reason))
                return false;

            string unitToken = cmd.Arg(1);

            if (!UnitToken.TryParsePixels(unitToken, tuning.ReferenceStageWidth, out float pixels))
            {
                reason = $"거리 토큰을 읽지 못했다: '{unitToken}'";
                return false;
            }

            ApplyMoveClaim(state, slotKey, targetId, relative: true, new Vec2(pixels * xSign, pixels * ySign));
            return true;
        }

        private static bool ApplyMovePer(
            StageState state, in StageCommand cmd, StageReducerTuning tuning,
            float xSign, float ySign, string targetId, out string reason)
        {
            if (!TryGetSpawnedSlot(state, cmd, out string slotKey, out reason))
                return false;

            // 브리지 규약: 거리 = 1u × 프레임 수 (기본 "1fr", 파싱 실패 폴백 8).
            if (!DurationToken.TryParseFrames(cmd.Arg(1, "1fr"), out float frames))
                frames = 8f;

            float pixels = UnitToken.UnitsToPixels(1f, tuning.ReferenceStageWidth) * frames;

            ApplyMoveClaim(state, slotKey, targetId, relative: true, new Vec2(pixels * xSign, pixels * ySign));
            return true;
        }

        private static bool ApplyMoveBy(
            StageState state, in StageCommand cmd, StageReducerTuning tuning, out string reason)
        {
            if (!TryGetSpawnedSlot(state, cmd, out string slotKey, out reason))
                return false;

            if (!UnitToken.TryParseSignedPixels(cmd.Arg(1, "0u"), tuning.ReferenceStageWidth, out float x) ||
                !UnitToken.TryParseSignedPixels(cmd.Arg(2, "0u"), tuning.ReferenceStageWidth, out float y))
            {
                reason = $"거리 토큰을 읽지 못했다: '{cmd.Arg(1)}', '{cmd.Arg(2)}'";
                return false;
            }

            ApplyMoveClaim(state, slotKey, "CharSlot_Track", relative: true, new Vec2(x, y));
            return true;
        }

        private static bool ApplyMoveReset(StageState state, in StageCommand cmd, out string reason)
        {
            if (!TryGetSpawnedSlot(state, cmd, out string slotKey, out reason))
                return false;

            // 브리지는 Track과 Track_Focus 두 노드에 절대 0을 건다.
            ApplyMoveClaim(state, slotKey, "CharSlot_Track", relative: false, Vec2.Zero);
            ApplyMoveClaim(state, slotKey, "CharSlot_Track_Focus", relative: false, Vec2.Zero);
            return true;
        }

        private static void ApplyMoveClaim(
            StageState state, string slotKey, string targetId, bool relative, Vec2 delta)
        {
            string nodeKey = StageState.NodeKeyOf(slotKey, targetId);

            state.Apply(MoveByReduction.Reduce(
                nodeKey,
                new MoveByReduction.Args(!relative, delta),
                state.Nodes.GetState(nodeKey).AnchoredPosition));
        }

        private static bool ApplyScaleBy(StageState state, in StageCommand cmd, out string reason)
        {
            if (!TryGetSpawnedSlot(state, cmd, out string slotKey, out reason))
                return false;

            if (!NumberToken.TryParseFloat(cmd.Arg(1), out float multiplier))
            {
                reason = $"배율을 읽지 못했다: '{cmd.Arg(1)}'";
                return false;
            }

            string nodeKey = StageState.NodeKeyOf(slotKey, "CharSlot_Scale");

            state.Apply(ScaleToReduction.Reduce(
                nodeKey,
                new ScaleToReduction.Args(true, new Vec2(multiplier, multiplier)),
                state.Nodes.GetState(nodeKey).LocalScale.XY));

            return true;
        }

        private static bool ApplyScaleReset(StageState state, in StageCommand cmd, out string reason)
        {
            if (!TryGetSpawnedSlot(state, cmd, out string slotKey, out reason))
                return false;

            string nodeKey = StageState.NodeKeyOf(slotKey, "CharSlot_Scale");

            state.Apply(ScaleToReduction.Reduce(
                nodeKey,
                new ScaleToReduction.Args(false, Vec2.One),
                state.Nodes.GetState(nodeKey).LocalScale.XY));

            return true;
        }

        private static bool ApplyRotateBy(StageState state, in StageCommand cmd, out string reason)
        {
            if (!TryGetSpawnedSlot(state, cmd, out string slotKey, out reason))
                return false;

            if (!NumberToken.TryParseFloat(cmd.Arg(1), out float degree))
            {
                reason = $"각도를 읽지 못했다: '{cmd.Arg(1)}'";
                return false;
            }

            string nodeKey = StageState.NodeKeyOf(slotKey, "CharSlot_SwayPivot");

            state.Apply(RotateToReduction.Reduce(
                nodeKey,
                new RotateToReduction.Args(true, new Vec3(0f, 0f, degree)),
                state.Nodes.GetState(nodeKey).LocalEulerAngles));

            return true;
        }

        private static bool ApplyRotateReset(StageState state, in StageCommand cmd, out string reason)
        {
            if (!TryGetSpawnedSlot(state, cmd, out string slotKey, out reason))
                return false;

            string nodeKey = StageState.NodeKeyOf(slotKey, "CharSlot_SwayPivot");

            state.Apply(RotateToReduction.Reduce(
                nodeKey,
                new RotateToReduction.Args(false, Vec3.Zero),
                state.Nodes.GetState(nodeKey).LocalEulerAngles));

            return true;
        }

        // ── shot ─────────────────────────────────────────────────────

        private static bool ApplyShotZoom(StageState state, in StageCommand cmd, out string reason)
        {
            // 브리지: shot_zoom(zoom=1f, duration).
            if (!NumberToken.TryParseFloat(cmd.Arg(0, "1"), out float zoom))
            {
                reason = $"zoom을 읽지 못했다: '{cmd.Arg(0)}'";
                return false;
            }

            state.Shot = ShotZoomReduction.Reduce(state.Shot, zoom);
            reason = null;
            return true;
        }

        private static bool ApplyShotTo(
            StageState state, in StageCommand cmd, StageReducerTuning tuning, out string reason)
        {
            // 브리지: shot_to(zoom=1f, x="2.5u", y="0u", duration).
            if (!NumberToken.TryParseFloat(cmd.Arg(0, "1"), out float zoom) ||
                !UnitToken.TryParseSignedPixels(cmd.Arg(1, "2.5u"), tuning.ReferenceStageWidth, out float x) ||
                !UnitToken.TryParseSignedPixels(cmd.Arg(2, "0u"), tuning.ReferenceStageWidth, out float y))
            {
                reason = $"shot_to 인자를 읽지 못했다: {cmd}";
                return false;
            }

            state.Shot = ShotToReduction.Reduce(state.Shot, zoom, new Vec2(x, y));
            reason = null;
            return true;
        }

        private static bool ApplyShotTrack(
            StageState state, in StageCommand cmd, StageReducerTuning tuning, out string reason)
        {
            // 브리지: shot_track(x="2.5u", y="0u", duration).
            if (!UnitToken.TryParseSignedPixels(cmd.Arg(0, "2.5u"), tuning.ReferenceStageWidth, out float x) ||
                !UnitToken.TryParseSignedPixels(cmd.Arg(1, "0u"), tuning.ReferenceStageWidth, out float y))
            {
                reason = $"shot_track 인자를 읽지 못했다: {cmd}";
                return false;
            }

            state.Shot = ShotTrackReduction.Reduce(state.Shot, new Vec2(x, y));
            reason = null;
            return true;
        }

        // ── 구조 ─────────────────────────────────────────────────────

        private static bool ApplyCharTo(
            StageState state, in StageCommand cmd, string stageKey, string layerKey, out string reason)
        {
            if (!TryGetSpawnedSlot(state, cmd, out string slotKey, out reason))
                return false;

            state.SetAttachment(slotKey, new SlotAttachment(stageKey, layerKey));
            return true;
        }

        // ── helper ───────────────────────────────────────────────────

        private static bool TryGetSlotKey(in StageCommand cmd, out string slotKey, out string reason)
        {
            slotKey = cmd.Arg(0);

            if (string.IsNullOrEmpty(slotKey))
            {
                reason = "슬롯 키 인자가 없다";
                return false;
            }

            reason = null;
            return true;
        }

        private static bool TryGetSpawnedSlot(
            StageState state, in StageCommand cmd, out string slotKey, out string reason)
        {
            if (!TryGetSlotKey(cmd, out string targetKey, out reason))
            {
                slotKey = null;
                return false;
            }

            // 별칭("@3")·캐릭터 키("parkeunseol")도 슬롯으로 푼다 (actor/cast 축).
            if (!state.TryResolveSlot(targetKey, out slotKey))
            {
                reason = $"대상 '{targetKey}'를 슬롯으로 풀 수 없다 (slot/cast/actor 선행 필요)";
                return false;
            }

            reason = null;
            return true;
        }

        private static string[] Prefixed(string slotKey, string[] ids)
        {
            string[] keys = new string[ids.Length];

            for (int i = 0; i < ids.Length; i++)
                keys[i] = StageState.NodeKeyOf(slotKey, ids[i]);

            return keys;
        }

        private static void Require(StageReducerTuning tuning)
        {
            if (tuning == null)
                throw new ArgumentNullException(nameof(tuning));

            if (!(tuning.ReferenceStageWidth > 0f))
                throw new ArgumentException("tuning.ReferenceStageWidth가 유효하지 않다.", nameof(tuning));

            if (!(tuning.BaseResolution.X > 0f) || !(tuning.BaseResolution.Y > 0f))
                throw new ArgumentException("tuning.BaseResolution이 유효하지 않다.", nameof(tuning));
        }
    }
}