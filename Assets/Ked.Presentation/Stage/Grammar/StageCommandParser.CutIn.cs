using Ked.Presentation.Core;
using UnityEngine;

/// <summary>
/// 컷인 — 덮는 무대 하나를 판 모양으로 띄운다(docs/PLAN_cutin.md).
///
///   cutin_open  {무대=stage01} {판} {x=0u} {y=0u} {배율=1}  →  CutInOpenCommandSpec  (창 피벗 · 판 · 마스크 → 팝 인)
///   cutin_close {무대=stage01}                               →  CutInCloseCommandSpec (팝 아웃 → 창 배율 0)
///
/// x y는 판 중심 — 무대 중심 기준 u(bg_move와 같은 단위, 1u = 40px). 판은 Resources/CutInPanels의 키.
/// 본 무대(stage00)에는 쓰지 않는다 — 경고하고 건너뛴다(폴드는 Unhandled).
/// 창 안의 인물은 일반 슬롯이다: slot c1 stage01 mid → show · size · place → cutin_open.
/// </summary>
public sealed partial class StageCommandParser
{
    private void EnqueueCutInOpenSpec(string stage, string panelKey, string xToken, string yToken, float scale)
    {
        string stageKey = StageKeyParser.ParseStage(stage);

        if (StageRoles.Of(stageKey) == StageRole.Main)
        {
            Debug.LogWarning($"[StageCommandParser] <<cutin_open>> 본 무대({stageKey})에는 컷인을 열 수 없다 — 덮는 무대(stage01 · stage02)에 쓴다. 건너뛴다.");
            return;
        }

        if (string.IsNullOrWhiteSpace(panelKey))
        {
            Debug.LogWarning("[StageCommandParser] <<cutin_open>>에 판 키가 없다. 건너뛴다.");
            return;
        }

        if (!(scale > 0f))
        {
            Debug.LogWarning($"[StageCommandParser] <<cutin_open>> 배율은 0보다 커야 한다 ({scale}). 1로 간다.");
            scale = 1f;
        }

        Emit(new CutInOpenCommandSpec
        {
            stage = stageKey,
            panelKey = panelKey.Trim(),
            center = new Vector2(ParseSignedUnit(xToken), ParseSignedUnit(yToken)),
            scale = scale,
        });
    }

    private void EnqueueCutInCloseSpec(string stage)
    {
        string stageKey = StageKeyParser.ParseStage(stage);

        if (StageRoles.Of(stageKey) == StageRole.Main)
        {
            Debug.LogWarning($"[StageCommandParser] <<cutin_close>> 본 무대({stageKey})에는 컷인이 없다. 건너뛴다.");
            return;
        }

        Emit(new CutInCloseCommandSpec { stage = stageKey });
    }
}
