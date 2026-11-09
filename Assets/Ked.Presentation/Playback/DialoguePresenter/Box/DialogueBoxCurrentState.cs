public sealed class DialogueBoxCurrentState
{
    public IPresentationLineSurfaceView Box { get; private set; }
    public DialogueBoxKind? BoxKind { get; private set; }
    public bool IsVisible { get; private set; }

    public void Commit(
        DialogueBoxKind boxKind,
        IPresentationLineSurfaceView box,
        DialogueBoxTransitionKind transitionKind)
    {
        BoxKind = boxKind;
        Box = box;
        IsVisible = transitionKind != DialogueBoxTransitionKind.Hide;
    }

    public void Reset()
    {
        Box = null;
        BoxKind = null;
        IsVisible = false;
    }
}