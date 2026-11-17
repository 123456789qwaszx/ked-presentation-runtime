using Ked.Presentation.Core;
using UnityEngine;

/// <summary>
/// 컷인 — 덮는 무대 하나를 판 모양으로 띄운다(docs/PLAN_cutin.md).
///
///   cutin_open  {무대=stage01} {판} {x=0u} {y=0u} {배율=1}  →  판 리그(세우기 · 그림 · 자리 · 배율 · 보임) + CutInOpenCommandSpec(창 피벗 · 마스크 → 팝 인)
///   cutin_close {무대=stage01}                               →  CutInCloseCommandSpec (팝 아웃 → 마스크 숨김)
///
/// x y는 판 중심 — 무대 중심 기준 u(bg_move와 같은 단위, 1u = 40px). 판은 Resources/CutInPanels의 키.
/// 본 무대(stage00)에는 쓰지 않는다 — 경고하고 건너뛴다(폴드는 Unhandled).
/// 창 안의 인물은 일반 슬롯이다: slot c1 stage01 mid → show · size · place → cutin_open.
///
/// 판 그림은 배경처럼 그 무대의 far 층에 서는 리그다 — 무대 안이라 마스크가 판까지 함께 자른다(docs/PLAN_stage-visibility.md §3.2).
/// 슬롯 키는 CutInPanelSlotKey(무대)로 정해져 있다. 닫아도 남는다(마스크가 숨긴다) — 다음 cutin_open이 다시 세우고,
/// 같은 무대를 stage_mask 인서트로 쓰기 전에는 대본이 인물처럼 치운다. 폴드 밖이다(outsideFold) — 코어는 컷인 표(CutIns)로만 안다.
/// </summary>
public sealed partial class StageCommandParser
{
    /// <summary>그 무대의 컷인 판 리그의 슬롯 키. 대본이 판을 치울 때 이 키를 쓴다(예: stage01_cutin_panel).</summary>
    public static string CutInPanelSlotKey(string stageKey) => $"{stageKey}_cutin_panel";

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

        Vector2 center = new(ParseSignedUnit(xToken), ParseSignedUnit(yToken));

        EnqueueCutInPanelRigSpecs(stageKey, panelKey.Trim(), center, scale);

        Emit(new CutInOpenCommandSpec
        {
            stage = stageKey,
            center = center,
            scale = scale,
        });
    }

    // 판 리그 — 배경과 같은 방식(bg_spawn · bg_sprite)으로 그 무대의 far 층에 세우고, 판 중심 · 배율로 두고, 바로 보인다.
    // 판 그림은 캔버스의 PanelPictureResolution배 해상도라 원본 픽셀 크기에 1/그 값을 곱한다.
    private void EnqueueCutInPanelRigSpecs(string stageKey, string panelKey, Vector2 center, float scale)
    {
        string slotKey = CutInPanelSlotKey(stageKey);
        float pictureScale = scale / CutInPresetDBSO.PanelPictureResolution;

        Emit(new SetupCharRigCommandSpec
        {
            roleKey = slotKey,
            rigPrefab = _charRigPrefab,
            stage = stageKey,
            layerDepth = StageLayerLabels.Far,
            asBackground = true,
            outsideFold = true,
        });

        Emit(new SetSpriteCommandSpecCharR
        {
            roleKey = slotKey,
            node = CharacterRigNode.CharacterPortraitSprite_Image,
            resourcesRoot = CutInPresetDBSO.PanelResourcesRoot,
            spriteKey = panelKey,
            sizingMode = CharRigImageSizingMode.SourcePixels,
            raycastTarget = false,
        });

        Emit(new MoveByCommandSpecCharR
        {
            roleKey = slotKey,
            node = CharacterRigNode.CharSlot_Track,
            useAbsolutePosition = true,
            delta = center,
            duration = 0f,
        });

        Emit(new ScaleToCommandSpecCharR
        {
            roleKey = slotKey,
            node = CharacterRigNode.CharacterPortrait_ActingScale,
            toScale = new Vector2(pictureScale, pictureScale),
            duration = 0f,
        });

        Emit(new FadeCommandSpecCharR
        {
            roleKey = slotKey,
            node = CharacterRigNode.CharacterPortraitSprite_Root,
            targetAlpha = NodeTargets.FadeInAlpha,
            duration = 0f,
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
