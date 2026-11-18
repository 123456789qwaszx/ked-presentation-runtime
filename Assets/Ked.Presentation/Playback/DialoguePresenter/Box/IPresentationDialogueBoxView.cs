using System.Threading;
using TMPro;
using UnityEngine;
using Yarn.Unity;

public interface IPresentationDialogueBoxView
{
    TMP_Text GetLineText();
    TMP_Text GetNameText();

    void ApplySurfaceLayout(DialogueSurfaceLayoutPresetDBSO.Entry entry);

    void ResetPresentationTransform();
    void PrimeText(string text, string characterName, bool hasCharacterName);

    // 초상 칸 — 촬영소가 찍은 그림(PortraitStudio.Texture)을 보인다. null이면 칸을 끈다.
    // 대사창은 리그를 모른다: 받는 것은 그림 한 장과 그 크기다. 칸은 대사창과 함께 보이고 숨는다(docs/PLAN_portrait-capture.md).
    // size는 찍힌 범위의 크기(무대 캔버스 px)다 — 텍스처의 px 수는 화면 배율을 따라 바뀌므로 칸의 크기로 쓰지 않는다.
    void SetPortrait(Texture portrait, Vector2 size);

    void SetVisibleImmediate(bool visible);
    void PrepareHidden();

    // 페이드. 토큰이 취소되면 그 자리에서 멈추고 최종값(보임·숨김)을 쓰지 않는다.
    // 줄 전환이 그 줄의 시각 토큰(LinePresentationRun.VisualToken)을 넘긴다. 줄이 아직 유효한지는 부르는 쪽이 판단한다.
    YarnTask FadeInAsync(float duration, CancellationToken token);
    YarnTask FadeOutAsync(float duration, CancellationToken token);
}