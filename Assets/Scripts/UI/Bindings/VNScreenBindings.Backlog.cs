public sealed partial class VNScreenBindings
{
    private void OpenBacklogPanel()
    {
        UI.PushPanel<BacklogPanel>(panel =>
        {
            BindPanel(panel, ApplyBindings);
            panel.Present(_vnFeatures.Backlogs);
        });
    }

    private void ApplyBindings(BacklogPanel panel)
    {
        AddBinding(panel,
            p => p.OnCloseRequested += ClosePanel,
            p => p.OnCloseRequested -= ClosePanel);
    }
}