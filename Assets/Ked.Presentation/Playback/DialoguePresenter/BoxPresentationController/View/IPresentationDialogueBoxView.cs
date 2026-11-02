using System.Threading;
using TMPro;
using Yarn.Unity;

public interface IPresentationDialogueBoxView
{
    TMP_Text GetLineText();
    TMP_Text GetNameText();

    void ApplySurfaceLayout(DialogueSurfaceLayoutPresetDBSO.Entry entry);

    void ResetPresentationTransform();
    void PrimeText(string text, string characterName, bool hasCharacterName);

    void SetVisibleImmediate(bool visible);
    void PrepareHidden();

    // 페이드. 토큰이 취소되면 그 자리에서 멈추고 최종값(보임·숨김)을 쓰지 않는다.
    // 줄 전환은 그 줄의 시각 토큰(LinePresentationRun.VisualToken)을,
    // box_hide·box_show는 컨트롤러의 수동 전환 토큰을 넘긴다. 줄이 아직 유효한지는 부르는 쪽이 판단한다.
    YarnTask FadeInAsync(float duration, CancellationToken token);
    YarnTask FadeOutAsync(float duration, CancellationToken token);
}