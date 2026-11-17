namespace Ked.Presentation.Core
{
    /// <summary>
    /// shot — 카메라 의도. 노드가 아니라 StageState.Shot 한 장을 갈아끼운다.
    ///
    /// shot_&lt;축&gt;은 그 축 하나를 절대값으로 놓고 나머지 축은 그대로 둔다(ShotIntents). 값 인자에는 생략값이 없다 —
    /// 빠졌거나 읽지 못하면 접지 않고 Unhandled로 남긴다(런타임도 그 줄을 건너뛴다).
    /// shot_reset · shot_reset_all은 값 인자가 없어 디스패치에서 바로 접는다. shot_shake는 접을 것이 없다.
    /// </summary>
    public static partial class StageReducer
    {
        /// <summary>
        /// shot_focus_to (role, focus="body", screenPoint="center", zoom=2.5, duration).
        ///
        /// 런타임과 같은 목표 계산을 태운다: 무대 좌표의 focus와 그 슬롯이 선 층 d를 ShotIntents.FocusTo에 넘긴다.
        /// 층 반응 곡선은 하네스가 무대 뷰를 판정할 때와 같은 것(ResolveLayerProfileCurve)이다.
        /// </summary>
        private static bool ApplyShotFocusTo(
            StageState state, in StageCommand cmd, StageReducerTuning tuning, out string reason)
        {
            if (!TryGetSpawnedSlot(state, cmd, out string slotKey, out reason))
                return false;

            // 샷은 본 무대만 움직인다(StageRoles). 덮는 무대의 리그로는 본 무대의 카메라를 풀 수 없다 — 런타임은 경고하고 건너뛴다.
            SlotAttachment attachment = state.GetAttachment(slotKey);

            if (StageRoles.Of(attachment.StageKey) != StageRole.Main)
            {
                reason = $"슬롯 '{slotKey}'은 덮는 무대 '{attachment.StageKey}'에 서 있다 — 샷은 본 무대만 움직인다";
                return false;
            }

            if (!FocusPresetName.TryNormalizeToken(cmd.Arg(1, "body"), out string focusName))
            {
                reason = $"focus 프리셋 토큰 '{cmd.Arg(1)}'을 모른다";
                return false;
            }

            string screenPointName = cmd.Arg(2, "center");

            if (!ScreenPointRatios.TryResolve(state.Nodes.RootSpace.Size, screenPointName, out Vec2 desired))
            {
                reason = $"화면 지점 '{screenPointName}'을 모른다";
                return false;
            }

            if (!NumberToken.TryParseFloat(cmd.Arg(3, "2.5"), out float zoom))
            {
                reason = $"zoom을 읽지 못했다: '{cmd.Arg(3)}'";
                return false;
            }

            state.TryGetCharacter(slotKey, out string characterKey);

            Vec2 focusOffset = FocusOffsetMath.Resolve(tuning.FocusTuning, characterKey, focusName, Vec2.Zero);

            RectNodeState[] chain = state.Nodes.BuildChainTo(
                StageState.NodeKeyOf(slotKey, NodeTargets.FocusMeasureNodeId));

            Vec2 focus = SettledFocusMath.FocusPointInRigSpace(
                chain, state.Nodes.RootSpace, focusOffset);

            state.Shot = ShotIntents.FocusTo(
                state.Shot,
                zoom,
                focus,
                desired,
                attachment.LayerDepth,
                tuning.ResolveLayerProfileCurve());

            return true;
        }

        private static bool ApplyShotZoom(StageState state, in StageCommand cmd, out string reason)
        {
            // 파서 표: shot_zoom(zoom, duration="0.45s", ease).
            if (!TryReadNumber(cmd, 0, "zoom", out float zoom, out reason))
                return false;

            state.Shot = ShotIntents.Zoom(state.Shot, zoom);
            return true;
        }

        private static bool ApplyShotPan(
            StageState state, in StageCommand cmd, StageReducerTuning tuning, out string reason)
        {
            // 파서 표: shot_pan(x, y, duration="0.45s", ease).
            if (!TryReadUnits(cmd, 0, tuning, out Vec2 pan, out reason))
                return false;

            state.Shot = ShotIntents.Pan(state.Shot, pan);
            return true;
        }

        private static bool ApplyShotTo(
            StageState state, in StageCommand cmd, StageReducerTuning tuning, out string reason)
        {
            // 파서 표: shot_to(zoom, x, y, duration="0.45s", ease).
            if (!TryReadNumber(cmd, 0, "zoom", out float zoom, out reason) ||
                !TryReadUnits(cmd, 1, tuning, out Vec2 pan, out reason))
            {
                return false;
            }

            state.Shot = ShotIntents.To(state.Shot, zoom, pan);
            return true;
        }

        private static bool ApplyShotTrack(
            StageState state, in StageCommand cmd, StageReducerTuning tuning, out string reason)
        {
            // 파서 표: shot_track(x, y, duration="0.45s", ease) — 카메라가 서는 자리(절대값).
            if (!TryReadUnits(cmd, 0, tuning, out Vec2 track, out reason))
                return false;

            state.Shot = ShotIntents.Track(state.Shot, track);
            return true;
        }

        private static bool ApplyShotDolly(StageState state, in StageCommand cmd, out string reason)
        {
            // 파서 표: shot_dolly(n, duration="0.45s", ease).
            if (!TryReadNumber(cmd, 0, "dolly", out float dolly, out reason))
                return false;

            state.Shot = ShotIntents.Dolly(state.Shot, dolly);
            return true;
        }

        private static bool ApplyShotRoll(StageState state, in StageCommand cmd, out string reason)
        {
            // 파서 표: shot_roll(도, duration="0.45s", ease).
            if (!TryReadNumber(cmd, 0, "roll", out float roll, out reason))
                return false;

            state.Shot = ShotIntents.Roll(state.Shot, roll);
            return true;
        }

        /// <summary>
        /// shot_shake (이름, 세기=1) — 화면 흔들림. 카메라 리그에 건 restore 모션이다.
        ///
        /// 접을 것이 없다: 흔들림은 구도 위에 얹혔다가 0으로 돌아오고, 상태에 남기는 것이 없다(restore 모션과 같은 규칙).
        /// 그래도 클립은 본다 — 모르는 이름이나 카메라 리그가 아닌 클립은 런타임이 틀지 않는 줄이라 Unhandled로 남긴다.
        /// </summary>
        private static bool ApplyShotShake(StageState state, in StageCommand cmd, StageReducerTuning tuning, out string reason)
        {
            string name = cmd.Arg(0);

            if (string.IsNullOrEmpty(name))
            {
                reason = "흔들림 이름이 없다";
                return false;
            }

            if (tuning.Motions == null || !tuning.Motions.TryGetValue(name, out MotionClip clip))
            {
                reason = tuning.Motions == null
                    ? $"흔들림 '{name}' — 튜닝에 motions.json이 없다"
                    : $"흔들림 '{name}'을 모른다 (motions.json에 없거나 검증에서 걸렸다)";
                return false;
            }

            if (clip.Rig != MotionRules.CameraRig)
            {
                reason = $"'{name}'은 '{clip.Rig}' 리그의 모션이다 — shot_shake는 '{MotionRules.CameraRig}' 리그의 클립만 튼다";
                return false;
            }

            if (cmd.Arg(1) != null && !NumberToken.TryParseFloat(cmd.Arg(1), out _))
            {
                reason = $"흔들림 '{name}'의 세기를 읽지 못했다: '{cmd.Arg(1)}'";
                return false;
            }

            reason = null;
            return true;
        }

        // 값 인자 하나(숫자). 생략값이 없다.
        private static bool TryReadNumber(in StageCommand cmd, int index, string what, out float value, out string reason)
        {
            value = 0f;
            string token = cmd.Arg(index);

            if (token == null)
            {
                reason = $"{cmd.Name}에 {what} 값이 없다 — 값 인자는 꼭 적는다";
                return false;
            }

            if (!NumberToken.TryParseFloat(token, out value))
            {
                reason = $"{cmd.Name}의 {what}을 읽지 못했다: '{token}'";
                return false;
            }

            reason = null;
            return true;
        }

        // 값 인자 둘(x · y, 단위 토큰). 생략값이 없다.
        private static bool TryReadUnits(
            in StageCommand cmd, int index, StageReducerTuning tuning, out Vec2 value, out string reason)
        {
            value = Vec2.Zero;
            string xToken = cmd.Arg(index);
            string yToken = cmd.Arg(index + 1);

            if (xToken == null || yToken == null)
            {
                reason = $"{cmd.Name}에 x · y 값이 없다 — 값 인자는 꼭 적는다";
                return false;
            }

            if (!UnitToken.TryParseSignedPixels(xToken, tuning.ReferenceStageWidth, out float x) ||
                !UnitToken.TryParseSignedPixels(yToken, tuning.ReferenceStageWidth, out float y))
            {
                reason = $"{cmd.Name}의 x · y를 읽지 못했다: '{xToken}' '{yToken}'";
                return false;
            }

            value = new Vec2(x, y);
            reason = null;
            return true;
        }
    }
}
