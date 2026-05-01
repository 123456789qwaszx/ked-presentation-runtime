using UnityEngine;
using Yarn.Unity;

public sealed class YarnUIBridge : MonoBehaviour
{
    private LinePresenter _linePresenter;
    private EllipsisBreathTypewriter _ellipsisBreathTypewriter;
    private DialogueTextRouter _dialogueTextRouter;
    private DialogueBoxHost _dialogueBoxResolver;
    
    public bool IsDialogueBoxReady => _dialogueTextRouter.LineText != null;

    public void Initialize(
        LinePresenter linePresenter,
        EllipsisBreathTypewriter ellipsisBreathTypewriter,
        DialogueTextRouter dialogueTextRouter,
        DialogueBoxHost dialogueBoxResolver)
    {
        _linePresenter = linePresenter;
        _ellipsisBreathTypewriter = ellipsisBreathTypewriter;
        _dialogueTextRouter = dialogueTextRouter;
        _dialogueBoxResolver = dialogueBoxResolver;
    }
    
    // public void BindAuto(DialogueBoxKind kind, bool hasCharacterName)
    // {
    //     IDialogueTextTarget box = _dialogueBoxResolver.ResolveTarget(kind);
    //
    //     _dialogueTextRouter.Bind(box);
    //
    //     ApplyRouterTarget(hasCharacterName);
    // }
    
    public void CloseAllDialogue()
    {
        _dialogueBoxResolver.HideAll();

        _dialogueTextRouter.Clear();

        _linePresenter.lineText = null;
        _linePresenter.characterNameText = null;
        _linePresenter.characterNameContainer = null;
        _linePresenter.showCharacterNameInLine = false;

        _ellipsisBreathTypewriter.SetTextView(null);
    }

    // private void ApplyRouterTarget(bool hasCharacterName)
    // {
    //     _linePresenter.lineText = _dialogueTextRouter.LineText;
    //
    //     if (hasCharacterName && _dialogueTextRouter.HasName)
    //     {
    //         _linePresenter.showCharacterNameInLine = false;
    //         _linePresenter.characterNameText = _dialogueTextRouter.NameText;
    //         _linePresenter.characterNameContainer = _dialogueTextRouter.NameText.gameObject;
    //     }
    //     else
    //     {
    //         _linePresenter.showCharacterNameInLine = false;
    //         _linePresenter.characterNameText = null;
    //         _linePresenter.characterNameContainer = null;
    //     }
    //
    //     _ellipsisBreathTypewriter.SetTextView(_dialogueTextRouter.LineText);
    // }
}