namespace Ked.Presentation.Core
{
    /// <summary>
    /// popup · popup_close — 덮는 무대의 그림 팝업 끝 상태(docs/PLAN_popup.md §7).
    ///
    /// 런타임(PopupOpenCommand · PopupCloseCommand)과 같은 규칙으로 접는다:
    ///   · 본 무대(stage00)에는 쓰지 않는다 — 런타임은 경고하고 건너뛰고, 여기서는 Unhandled로 소리를 낸다.
    ///   · 열지 않은 무대를 닫으면 런타임은 경고하고 건너뛴다 — 여기서도 Unhandled.
    ///   · 열린 팝업에 다시 popup을 쓰면 그림 · 자리 · 배율만 바뀐다(열림 그대로).
    /// 팝업은 무대 루트 바깥이라 리그 노드 트리(리그 로컬 값)에 들지 않는다.
    /// tx · 마스크 지우기는 런타임에서 팝업을 닫지만 폴드는 마스크를 모른다(target-boundary) — 컷인과 같은 경계다.
    /// </summary>
    public static partial class StageReducer
    {
        private const string DefaultPopupStage = "stage01";

        private static bool ApplyPopupOpen(
            StageState state, in StageCommand cmd, StageReducerTuning tuning, out string reason)
        {
            if (!TryGetPopupStage(cmd, out string stageKey, out reason))
                return false;

            string pictureKey = cmd.Arg(1);

            if (string.IsNullOrWhiteSpace(pictureKey))
            {
                reason = "그림 키 인자가 없다";
                return false;
            }

            if (!UnitToken.TryParseSignedPixels(cmd.Arg(2, "0u"), tuning.ReferenceStageWidth, out float x) ||
                !UnitToken.TryParseSignedPixels(cmd.Arg(3, "0u"), tuning.ReferenceStageWidth, out float y))
            {
                reason = $"자리 인자가 단위 토큰이 아니다 ('{cmd.Arg(2)}', '{cmd.Arg(3)}')";
                return false;
            }

            if (!NumberToken.TryParseFloat(cmd.Arg(4, "1"), out float scale) || !(scale > 0f))
            {
                reason = $"배율은 0보다 큰 수여야 한다 ('{cmd.Arg(4)}')";
                return false;
            }

            state.SetPopup(stageKey, new PopupState(true, pictureKey.Trim(), new Vec2(x, y), scale));
            reason = null;
            return true;
        }

        private static bool ApplyPopupClose(StageState state, in StageCommand cmd, out string reason)
        {
            if (!TryGetPopupStage(cmd, out string stageKey, out reason))
                return false;

            if (!state.TryGetPopup(stageKey, out PopupState popup) || !popup.IsOpen)
            {
                reason = $"{stageKey}에 열린 그림 팝업이 없다";
                return false;
            }

            state.SetPopup(stageKey, popup.Closed());
            reason = null;
            return true;
        }

        private static bool TryGetPopupStage(in StageCommand cmd, out string stageKey, out string reason)
        {
            string raw = (cmd.Arg(0, DefaultPopupStage) ?? "").Trim().ToLowerInvariant();
            stageKey = raw.Length == 0 ? DefaultPopupStage : raw;

            if (StageRoles.Of(stageKey) == StageRole.Main)
            {
                reason = $"본 무대({stageKey})에는 그림 팝업을 열고 닫을 수 없다 — 덮는 무대에 쓴다";
                return false;
            }

            reason = null;
            return true;
        }
    }
}
