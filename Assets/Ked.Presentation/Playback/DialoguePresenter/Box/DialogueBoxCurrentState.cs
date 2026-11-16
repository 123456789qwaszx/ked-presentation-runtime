public sealed class DialogueBoxCurrentState
{
    public IPresentationDialogueBoxView Box { get; private set; }
    public DialogueBoxKind? BoxKind { get; private set; }
    public bool IsVisible { get; private set; }

    public void Commit(
        DialogueBoxKind boxKind,
        IPresentationDialogueBoxView box,
        DialogueBoxTransitionKind transitionKind)
    {
        BoxKind = boxKind;
        Box = box;
        IsVisible = transitionKind != DialogueBoxTransitionKind.Hide;
    }

    // 종류는 그대로 두고 감추기만 한다 — 다음 줄이 FadeIn으로 연다.
    public void MarkHidden()
    {
        IsVisible = false;
    }

    public void Reset()
    {
        Box = null;
        BoxKind = null;
        IsVisible = false;
    }
}