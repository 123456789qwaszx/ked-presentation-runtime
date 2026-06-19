using TMPro;
using UnityEngine;
using Yarn.Unity;

public interface IDialogueTextTarget
{
    TMP_Text LineText { get; }
    TMP_Text NameText { get; }
    bool HasName { get; }
    CanvasGroup CanvasGroup { get; }
}

public interface IPresentationDialogueBoxView : IDialogueTextTarget
{
    void ResetPresentationTransform();

    void PrimeText(VNDialogueLine line);

    void SetVisible(bool visible);

    void PrepareHidden();

    YarnTask FadeInAsync(float duration, LinePresentationRun run);

    YarnTask FadeOutAsync(float duration, LinePresentationRun run);
}