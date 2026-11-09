using Ked.Presentation.Core;
using UnityEngine;

/// <summary>
/// 그림 팝업 — 덮는 무대에 뒷막 + 그림을 한 페이드로 띄운다(docs/PLAN_popup.md).
///
///   popup       {무대=stage01} {그림} {x=0u} {y=0u} {배율=1}  →  PopupOpenCommandSpec  (뒷막 · 그림 → 페이드 인)
///   popup_close {무대=stage01}                                →  PopupCloseCommandSpec (페이드 아웃 → 닫힘)
///
/// x y는 그림 중심 — 무대 중심 기준 u(cutin_open과 같은 단위, 1u = 40px). 그림은 Resources/Popups의 키.
/// 배율은 프리셋 배율(원본 실측 0.91)에 곱한다. 본 무대(stage00)에는 쓰지 않는다 — 경고하고 건너뛴다(폴드는 Unhandled).
/// </summary>
public sealed partial class StageCommandParser
{
    private void EnqueuePopupOpenSpec(string stage, string pictureKey, string xToken, string yToken, float scale)
    {
        string stageKey = StageKeyParser.ParseStage(stage);

        if (StageRoles.Of(stageKey) == StageRole.Main)
        {
            Debug.LogWarning($"[StageCommandParser] <<popup>> 본 무대({stageKey})에는 그림 팝업을 열 수 없다 — 덮는 무대(stage01 · stage02)에 쓴다. 건너뛴다.");
            return;
        }

        if (string.IsNullOrWhiteSpace(pictureKey))
        {
            Debug.LogWarning("[StageCommandParser] <<popup>>에 그림 키가 없다. 건너뛴다.");
            return;
        }

        if (!(scale > 0f))
        {
            Debug.LogWarning($"[StageCommandParser] <<popup>> 배율은 0보다 커야 한다 ({scale}). 1로 간다.");
            scale = 1f;
        }

        Emit(new PopupOpenCommandSpec
        {
            stage = stageKey,
            pictureKey = pictureKey.Trim(),
            center = new Vector2(ParseSignedUnit(xToken), ParseSignedUnit(yToken)),
            scale = scale,
        });
    }

    private void EnqueuePopupCloseSpec(string stage)
    {
        string stageKey = StageKeyParser.ParseStage(stage);

        if (StageRoles.Of(stageKey) == StageRole.Main)
        {
            Debug.LogWarning($"[StageCommandParser] <<popup_close>> 본 무대({stageKey})에는 그림 팝업이 없다. 건너뛴다.");
            return;
        }

        Emit(new PopupCloseCommandSpec { stage = stageKey });
    }
}
