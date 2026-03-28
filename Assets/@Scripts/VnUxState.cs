public sealed class VnUxState
{
    public bool ChoicesVisible { get; private set; }
    public bool BacklogVisible { get; private set; }
    public bool HudHidden { get; private set; }

    public void SetChoicesVisible(bool visible) => ChoicesVisible = visible;
    public void SetBacklogVisible(bool visible) => BacklogVisible = visible;
    public void SetHudHidden(bool v) => HudHidden = v;
}