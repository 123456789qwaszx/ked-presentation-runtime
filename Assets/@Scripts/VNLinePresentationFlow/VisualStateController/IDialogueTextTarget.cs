using TMPro;
using Yarn.Unity;

public interface IPresentationDialogueBoxView
{
    TMP_Text GetLineText();
    
    void ResetPresentationTransform();
    void PrimeText(string text, string characterName, bool hasCharacterName);
    
    void SetVisibleImmediate(bool visible);
    
    void PrepareHidden();


    YarnTask FadeInAsync(float duration, LinePresentationRun run);
    YarnTask FadeOutAsync(float duration, LinePresentationRun run);
}