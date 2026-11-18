using System;
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
    // 대사창은 리그를 모른다: 받는 것은 그림 한 장과 칸의 자리다. 칸은 대사창과 함께 보이고 숨는다(docs/PLAN_portrait-capture.md).
    // readCell은 칸의 자리와 크기(캔버스 px — 대사창 그림의 왼쪽 아래 귀 기준)를 낸다. 칸이 보이는 동안 프레임마다 다시 읽는다 —
    // 값의 집(구도 프리셋)을 재생 중에 고치면 바로 따라간다. 텍스처의 px 수는 화면 배율을 따라 바뀌므로 칸의 크기로 쓰지 않는다.
    void SetPortrait(Texture portrait, Func<Rect> readCell);

    void SetVisibleImmediate(bool visible);
    void PrepareHidden();

    // 페이드. 토큰이 취소되면 그 자리에서 멈추고 최종값(보임·숨김)을 쓰지 않는다.
    // 줄 전환이 그 줄의 시각 토큰(LinePresentationRun.VisualToken)을 넘긴다. 줄이 아직 유효한지는 부르는 쪽이 판단한다.
    YarnTask FadeInAsync(float duration, CancellationToken token);
    YarnTask FadeOutAsync(float duration, CancellationToken token);
}