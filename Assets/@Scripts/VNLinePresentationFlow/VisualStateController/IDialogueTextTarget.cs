using TMPro;
using UnityEngine;
using Yarn.Unity;

public interface IDialogueTextTarget
{
    TMP_Text LineText { get; }
    TMP_Text NameText { get; }
    CanvasGroup CanvasGroup { get; }
    bool HasName { get; }
}

public interface IPresentationDialogueBoxView : IDialogueTextTarget
{
    void ResetPresentationTransform();
    void PrimeText(string text, string characterName, bool hasCharacterName);
    
    void SetVisibleImmediate(bool visible);
    
    void PrepareHidden();

    YarnTask FadeInAsync(float duration, LinePresentationRun run);
    YarnTask FadeOutAsync(float duration, LinePresentationRun run);
}