using System.Collections.Generic;

public sealed partial class VnScreenBindings
{
    private void OpenChoicePanel(IReadOnlyList<string> choices)
    {
        _uxState.SetChoicesVisible(true);

        ChoicePanel existing = UI.GetUI<ChoicePanel>();

        if (existing != null)
        {
            existing.Present(choices);
            return;
        }

        UI.PushPanel<ChoicePanel>(panel =>
        {
            BindPanel(panel, BindChoicePanel);
            panel.Present(choices);
        });
    }

    private void BindChoicePanel(ChoicePanel panel)
    {
        AddBinding(
            panel,
            p => p.OnChoiceSelected += HandleChoiceSelected,
            p => p.OnChoiceSelected -= HandleChoiceSelected);

        AddBinding(
            panel,
            p => p.OnCloseRequested += CloseChoicePanel,
            p => p.OnCloseRequested -= CloseChoicePanel);
    }

    private void HandleChoiceSelected(int index)
    {
    }

    private void CloseChoicePanel()
    {
        _uxState.SetChoicesVisible(false);

        ChoicePanel panel = UI.GetUI<ChoicePanel>();

        if (panel != null)
            Unbind(panel);

        UI.PopPanel();
    }
}