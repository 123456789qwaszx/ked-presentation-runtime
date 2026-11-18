using Ked.Presentation.Core;
using UnityEngine;

/// <summary>
/// 대사창 초상 — 촬영소에 초상 인물을 세운다(docs/PLAN_portrait-capture.md).
///
///   portrait       {캐릭터} {포즈=a} {표정=1}  →  초상 리그(세우기 · 캐스팅 · 포즈 · 표정 · 앵커 · 보임)
///   portrait_close                             →  초상 리그를 숨긴다
///
/// 팝업 · 컷인과 같은 결이다 — 대본은 뜻만 말하고, 리그를 세우는 일과 슬롯 키 · 서는 자리는 파서가 정한다.
/// 슬롯 키는 PortraitStudio.SlotKey(portrait) 하나, 서는 자리는 촬영소(PortraitStudio.StageKey — 대본의 무대가 아니다).
/// 촬영소는 그 슬롯을 찍고, 대사창은 초상 줄(종류 Portrait)에서 그 그림을 보인다.
///
/// 표정 · 몸짓 · 이모지는 그 슬롯 키에 보통 명령으로 쓴다: <<face portrait 2>> · <<motion portrait g01_hop>>.
/// 자리 · 크기 명령은 써도 보이는 것이 없다 — 구도는 프리셋(PortraitFramePresetDBSO)이 정한다.
/// 캐스팅은 슬롯만 묶는다(slotOnly) — 같은 캐릭터가 무대에 서 있어도 `face harin 3`은 무대의 인물로 간다.
/// portrait를 다시 쓰면 초상 리그를 새로 세운다(인물을 바꾼다).
/// 폴드 밖이다(outsideFold) — 코어는 초상 인물을 모른다. 컷인 판 · 팝업 그림과 같은 줄이다.
/// </summary>
public sealed partial class StageCommandParser
{
    private void EnqueuePortraitOpenSpec(string characterKey, string variantKey, string emotionKey)
    {
        if (string.IsNullOrWhiteSpace(characterKey))
        {
            Debug.LogWarning("[StageCommandParser] <<portrait>>에 캐릭터 키가 없다. 건너뛴다.");
            return;
        }

        string slotKey = PortraitStudio.SlotKey;

        Emit(new SetupCharRigCommandSpec
        {
            roleKey = slotKey,
            rigPrefab = _charRigPrefab,
            stage = PortraitStudio.StageKey,
            layerDepth = StageLayerLabels.Mid,
            outsideFold = true,
        });

        EnqueueCastCharacterSpec(slotKey, characterKey.Trim(), variantKey, emotionKey, slotOnly: true);

        // 새 리그의 그림 루트는 알파 0으로 선다 — 바로 연다. 대사창이 보일 때만 화면에 나오므로 페이드는 대사창의 것이다.
        Emit(new FadeCommandSpecCharR
        {
            roleKey = slotKey,
            node = CharacterRigNode.CharacterPortraitSprite_Root,
            targetAlpha = NodeTargets.FadeInAlpha,
            duration = 0f,
        });
    }

    // 세우지 않고 닫으면 초상 리그가 없다 — 브리지의 대상 확인이 경고하고 건너뛴다.
    private void EnqueuePortraitCloseSpec()
        => Emit(new FadeCommandSpecCharR
        {
            roleKey = PortraitStudio.SlotKey,
            node = CharacterRigNode.CharacterPortraitSprite_Root,
            targetAlpha = NodeTargets.FadeOutAlpha,
            duration = 0f,
        });
}
