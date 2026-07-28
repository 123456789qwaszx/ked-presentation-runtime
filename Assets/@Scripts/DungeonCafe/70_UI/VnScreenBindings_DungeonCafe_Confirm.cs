using Yarn.Unity;

// 범용 확인 창.
public sealed partial class VnScreenBindings
{
    private bool _hasConfirmResult;

    private async YarnTask PresentConfirmAsync(string title, string body, string confirmLabel)
    {
        _hasConfirmResult = false;

        UI.PushPanel<ConfirmPanel>(panel =>
        {
            BindPanel(panel, ApplyConfirmBindings);

            panel.Present(
                title: title,
                body: body,
                confirmLabel: confirmLabel,
                cancelLabel: string.Empty);
        });

        await AsyncWait.UntilAsync(() => _hasConfirmResult);

        ClosePanel();
    }

    private void ApplyConfirmBindings(ConfirmPanel panel)
    {
        AddBinding(panel,
            p => p.ConfirmClicked += HandleConfirmClicked,
            p => p.ConfirmClicked -= HandleConfirmClicked);
    }

    private void HandleConfirmClicked() => _hasConfirmResult = true;
}