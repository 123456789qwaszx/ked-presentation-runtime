using UnityEngine;
using Yarn.Unity;

public sealed class YarnUIBridge : MonoBehaviour
{
    private LinePresenter _linePresenter;
    private EllipsisBreathTypewriter _ellipsisBreathTypewriter;
    private DialogueTextRouter _dialogueTextRouter;
    
    public bool IsDialogueBoxReady => _dialogueTextRouter.LineText != null;

    public void Initialize(LinePresenter linePresenter, EllipsisBreathTypewriter ellipsisBreathTypewriter, DialogueTextRouter dialogueTextRouter)
    {
        _linePresenter = linePresenter;
        _ellipsisBreathTypewriter = ellipsisBreathTypewriter;
        _dialogueTextRouter = dialogueTextRouter;
    }
    
    public void WithProtagonist() => Show(DialogueUIRoot.DialogueBoxKind.WithPortrait);
    public void HasCharNameBox() => Show(DialogueUIRoot.DialogueBoxKind.NoPortrait);
    public void LetterBox() => Show(DialogueUIRoot.DialogueBoxKind.LetterBox);
    public void OnlyText() => Show(DialogueUIRoot.DialogueBoxKind.OnlyText);


    private void Show(DialogueUIRoot.DialogueBoxKind kind)
    {
        DialogueUIRoot dialogueUI = UIManager.Instance.GetUI<DialogueUIRoot>();
        if (UIManager.Instance.CurSceneRoot != dialogueUI)
            UIManager.Instance.SwitchRoot<DialogueUIRoot>();
        
        dialogueUI.ShowBox(kind);

        IDialogueBoxView box = dialogueUI.GetBox(kind);
        _dialogueTextRouter.Bind(box);

        ApplyRouterTarget();
    }
    
    private void ApplyRouterTarget()
    {
        //_linePresenter.lineText = _dialogueTextRouter.LineText;

        if (!_dialogueTextRouter.HasName)
        {
            _linePresenter.showCharacterNameInLine = false;
            _linePresenter.characterNameText = null;
        }
        else
        {
            _linePresenter.showCharacterNameInLine = true;
            _linePresenter.characterNameText = _dialogueTextRouter.NameText;
        }
        
        _ellipsisBreathTypewriter.SetTextView(_dialogueTextRouter.LineText);
    }
    
    
    public void CloseAllDialogue()
    {
        DialogueUIRoot dialogueUI = UIManager.Instance.GetUI<DialogueUIRoot>();
        dialogueUI.HideAllBoxes();

        _dialogueTextRouter.Clear();

        //_linePresenter.lineText = null;
        _linePresenter.characterNameText = null;
        _linePresenter.showCharacterNameInLine = false;
        
        _ellipsisBreathTypewriter.SetTextView(null);
    }
}