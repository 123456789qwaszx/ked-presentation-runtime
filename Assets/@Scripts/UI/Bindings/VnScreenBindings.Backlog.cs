public sealed partial class VnScreenBindings
{
    private void OpenBacklogPanel()
    {
        if (_uxState.BacklogVisible)
            return;

        _uxState.SetBacklogVisible(true);

        UI.PushPanel<BacklogPanel>(panel =>
        {
            BindPanel(panel, BindBacklogPanel);
            panel.Present(_vnFeatures.Backlogs);
        });
    }

    private void BindBacklogPanel(BacklogPanel panel)
    {
        AddBinding(
            panel,
            p => p.OnCloseRequested += CloseBacklogPanel,
            p => p.OnCloseRequested -= CloseBacklogPanel);
    }

    private void CloseBacklogPanel()
    {
        _uxState.SetBacklogVisible(false);

        BacklogPanel panel = UI.GetUI<BacklogPanel>();

        if (panel != null)
            Unbind(panel);

        UI.PopPanel();
    }
}