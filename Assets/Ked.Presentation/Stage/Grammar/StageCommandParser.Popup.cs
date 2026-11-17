using Ked.Presentation.Core;
using UnityEngine;

/// <summary>
/// 그림 팝업 — 덮는 무대에 그림 하나를 띄운다(docs/PLAN_popup.md · docs/PLAN_stage-visibility.md §3.3).
///
///   popup       {무대=stage01} {그림} {x=0u} {y=0u} {배율=1}  →  그림 리그(세우기 · 그림 · 자리 · 배율 · 페이드 인)
///   popup_close {무대=stage01}                                →  그림 리그 페이드 아웃
///
/// x y는 그림 중심 — 무대 중심 기준 u(cutin_open과 같은 단위, 1u = 40px). 그림은 Resources/Popups의 키.
/// 배율은 프리셋 배율(원본 실측 0.91)에 곱한다. 본 무대(stage00)에는 쓰지 않는다 — 경고하고 건너뛴다(폴드는 Unhandled).
///
/// 그림은 배경처럼 그 무대의 close 층에 서는 리그다(슬롯 키 PopupSlotKey(무대)) — 무대 안이라 그 무대의 마스크 · 창을 따른다.
/// 팝업은 그림 하나다. 뒤를 어둡게 까는 것은 대본(연출 카드)이 stage_dim으로 한다 — 그림 바로 뒤인 front 층의 판:
///   <<stage_dim front 0.47 0.4s Linear stage02>>  <<popup stage02 sample_letter>>  …  <<popup_close stage02>>  <<stage_dim front 0 0.3s Linear stage02>>
/// 열린 팝업에 다시 popup을 쓰면 그림 리그를 새로 세운다 — 새 그림이 페이드 인한다.
/// 폴드 밖이다(outsideFold) — 코어는 팝업 표(Popups)로만 안다.
/// </summary>
public sealed partial class StageCommandParser
{
    /// <summary>그 무대의 팝업 그림 리그의 슬롯 키(예: stage02_popup).</summary>
    public static string PopupSlotKey(string stageKey) => $"{stageKey}_popup";

    private float PopupPictureScale => _popupPreset != null ? _popupPreset.pictureScale : PopupPresetDBSO.DefaultPictureScale;
    private float PopupOpenSeconds => _popupPreset != null ? _popupPreset.openSeconds : PopupPresetDBSO.DefaultOpenSeconds;
    private float PopupCloseSeconds => _popupPreset != null ? _popupPreset.closeSeconds : PopupPresetDBSO.DefaultCloseSeconds;

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

        string slotKey = PopupSlotKey(stageKey);
        float pictureScale = PopupPictureScale * scale;

        Emit(new SetupCharRigCommandSpec
        {
            roleKey = slotKey,
            rigPrefab = _charRigPrefab,
            stage = stageKey,
            layerDepth = StageLayerLabels.Close,
            asBackground = true,
            outsideFold = true,
        });

        Emit(new SetSpriteCommandSpecCharR
        {
            roleKey = slotKey,
            node = CharacterRigNode.CharacterPortraitSprite_Image,
            resourcesRoot = PopupPresetDBSO.PictureResourcesRoot,
            spriteKey = pictureKey.Trim(),
            sizingMode = CharRigImageSizingMode.SourcePixels,
            raycastTarget = false,
        });

        Emit(new MoveByCommandSpecCharR
        {
            roleKey = slotKey,
            node = CharacterRigNode.CharSlot_Track,
            useAbsolutePosition = true,
            delta = new Vector2(ParseSignedUnit(xToken), ParseSignedUnit(yToken)),
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
            duration = PopupOpenSeconds,
        });
    }

    // 열지 않은 무대를 닫으면 그림 리그가 없다 — 브리지의 대상 확인이 경고하고 건너뛴다(폴드는 Unhandled).
    private void EnqueuePopupCloseSpec(string stage)
    {
        string stageKey = StageKeyParser.ParseStage(stage);

        if (StageRoles.Of(stageKey) == StageRole.Main)
        {
            Debug.LogWarning($"[StageCommandParser] <<popup_close>> 본 무대({stageKey})에는 그림 팝업이 없다. 건너뛴다.");
            return;
        }

        Emit(new FadeCommandSpecCharR
        {
            roleKey = PopupSlotKey(stageKey),
            node = CharacterRigNode.CharacterPortraitSprite_Root,
            targetAlpha = NodeTargets.FadeOutAlpha,
            duration = PopupCloseSeconds,
        });
    }
}
