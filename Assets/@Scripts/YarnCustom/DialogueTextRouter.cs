using TMPro;
using UnityEngine;

public interface IDialogueTextTarget
{
    TMP_Text LineText { get; }
    TMP_Text NameText { get; }
    bool HasName { get; }
    CanvasGroup CanvasGroup { get; }
}

public sealed class DialogueTextRouter : MonoBehaviour
{
    public TMP_Text LineText { get; private set; }
    public TMP_Text NameText { get; private set; }
    public bool HasName => NameText != null;

    public void Bind(IDialogueTextTarget box)
    {
        LineText = box.LineText;
        NameText = box.NameText;
    }

    public void Clear()
    {
        LineText = null;
        NameText = null;
    }
}

public sealed class DialogueBoxCurrentState
{
    public DialogueBoxKind? BoxKind { get; private set; }
    public IDialogueTextTarget Box { get; private set; }
    public bool IsVisible { get; private set; }

    public void Commit(
        DialogueBoxKind kind,
        IDialogueTextTarget box,
        DialogueBoxTransitionKind transitionKind)
    {
        BoxKind = kind;
        Box = box;
        IsVisible = transitionKind != DialogueBoxTransitionKind.Hide;
    }

    public void Reset()
    {
        BoxKind = null;
        Box = null;
        IsVisible = false;
    }
}

public sealed class LinePresentationAdvanceState
{
    private bool _isRollbackSeeking;
    private bool _hasActiveLine;
    //private bool _isTransitioning;
    //private bool _isTypewriterRunning;
    private bool _isLineFullyShown = true;
    
    public bool IsRollbackSeeking => _isRollbackSeeking;

    public bool IsLineFullyShown => _hasActiveLine && _isLineFullyShown;

    public bool CanRequestNextLine => _hasActiveLine && _isLineFullyShown;

    public bool CanRequestHurryUp => _hasActiveLine && !_isLineFullyShown;

    public void EnterLine()
    {
        _hasActiveLine = true;
        //_isTransitioning = true;
        //_isTypewriterRunning = false;
        _isLineFullyShown = false;
    }

    public void EndTransition()
    {
        if (!_hasActiveLine)
            return;

        //_isTransitioning = false;
    }

    public void BeginTypewriter()
    {
        if (!_hasActiveLine)
            return;

        //_isTypewriterRunning = true;
        _isLineFullyShown = false;
    }

    public void CompleteLineDisplay()
    {
        if (!_hasActiveLine)
            return;

        //_isTransitioning = false;
        //_isTypewriterRunning = false;
        _isLineFullyShown = true;
    }

    public void EnterRollbackSeek()
    {
        _isRollbackSeeking = true;
        _hasActiveLine = true;
        _isLineFullyShown = false;
    }

    public void ExitRollbackSeek()
    {
        _isRollbackSeeking = false;
    }
    public void DismissLine()
    {
        _hasActiveLine = false;
        //_isTransitioning = false;
        //_isTypewriterRunning = false;
        _isLineFullyShown = true;
    }

    public void Reset()
    {
        _hasActiveLine = false;
        //_isTransitioning = false;
        //_isTypewriterRunning = false;
        _isLineFullyShown = true;
    }
}