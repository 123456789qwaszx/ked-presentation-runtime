using UnityEngine;
using Yarn.Unity;

public sealed class YarnUIBridge : MonoBehaviour
{
    [SerializeField] private LinePresenter linePresenter;
    [SerializeField] private EllipsisBreathTypewriter typewriter;
    [SerializeField] private DialogueUIRoot dialogueUI;
    
    public DialogueTextRouter DialogueTextRouter { get; } = new();
    
    public void WithProtagonist() => Show(DialogueUIRoot.DialogueBoxKind.WithPortrait);
    public void HasCharNameBox() => Show(DialogueUIRoot.DialogueBoxKind.NoPortrait);
    public void LetterBox() => Show(DialogueUIRoot.DialogueBoxKind.LetterBox);


    private void Show(DialogueUIRoot.DialogueBoxKind kind)
    {
        if (UIManager.Instance.CurSceneRoot != dialogueUI)
            UIManager.Instance.SwitchRoot<DialogueUIRoot>();
        
        dialogueUI.ShowBox(kind);

        IDialogueBoxView box = dialogueUI.GetBox(kind);
        DialogueTextRouter.Bind(box);

        ApplyRouterTarget();
    }
    
    private void ApplyRouterTarget()
    {
        linePresenter.lineText = DialogueTextRouter.LineText;

        if (!DialogueTextRouter.HasName)
        {
            linePresenter.showCharacterNameInLine = false;
            linePresenter.characterNameText = null;
        }
        else
        {
            linePresenter.showCharacterNameInLine = true;
            linePresenter.characterNameText = DialogueTextRouter.NameText;
        }
        
        typewriter.SetTextView(DialogueTextRouter.LineText);
    }
    
    
    public void CloseAllDialogue()
    {
        dialogueUI.HideAllBoxes();

        DialogueTextRouter.Clear();

        linePresenter.lineText = null;
        linePresenter.characterNameText = null;
        linePresenter.showCharacterNameInLine = false;
        
        typewriter.SetTextView(null);
    }
}