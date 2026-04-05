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
    
    public void BindAuto(DialogueUIRoot.DialogueBoxKind kind, bool hasCharacterName)
    {
        DialogueUIRoot dialogueUI = UIManager.Instance.GetUI<DialogueUIRoot>();
        if (UIManager.Instance.CurSceneRoot != dialogueUI)
            UIManager.Instance.SwitchRoot<DialogueUIRoot>();

        dialogueUI.ShowBox(kind);

        IDialogueBoxView box = dialogueUI.GetBox(kind);
        _dialogueTextRouter.Bind(box);

        ApplyRouterTarget(hasCharacterName);
    }
    
    public void CloseAllDialogue()
    {
        DialogueUIRoot dialogueUI = UIManager.Instance.GetUI<DialogueUIRoot>();
        dialogueUI.HideAllBoxes();

        _dialogueTextRouter.Clear();

        _linePresenter.lineText = null;
        _linePresenter.characterNameText = null;
        _linePresenter.characterNameContainer = null;
        _linePresenter.showCharacterNameInLine = false;

        _ellipsisBreathTypewriter.SetTextView(null);
    }

    private void ApplyRouterTarget(bool hasCharacterName)
    {
        _linePresenter.lineText = _dialogueTextRouter.LineText;

        if (hasCharacterName && _dialogueTextRouter.HasName)
        {
            _linePresenter.showCharacterNameInLine = false;
            _linePresenter.characterNameText = _dialogueTextRouter.NameText;
            _linePresenter.characterNameContainer = _dialogueTextRouter.NameText.gameObject;
        }
        else
        {
            _linePresenter.showCharacterNameInLine = false;
            _linePresenter.characterNameText = null;
            _linePresenter.characterNameContainer = null;
        }

        _ellipsisBreathTypewriter.SetTextView(_dialogueTextRouter.LineText);
    }
}