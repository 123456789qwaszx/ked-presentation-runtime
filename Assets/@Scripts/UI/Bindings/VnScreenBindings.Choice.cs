using System.Collections.Generic;

public sealed partial class VnScreenBindings
{
    private void OpenChoicePanel(IReadOnlyList<string> choices)
    {
        ChoicePanel existing = UI.GetUI<ChoicePanel>();

        if (existing != null)
        {
            existing.Present(choices);
            return;
        }

        UI.PushPanel<ChoicePanel>(panel =>
        {
            BindPanel(panel, ApplyBindings);
            panel.Present(choices);
        });
    }

    private void ApplyBindings(ChoicePanel panel)
    {
        AddBinding(panel,
            p => p.OnChoiceSelected += HandleChoiceSelected,
            p => p.OnChoiceSelected -= HandleChoiceSelected);

        AddBinding(panel,
            p => p.OnCloseRequested += ClosePanel,
            p => p.OnCloseRequested -= ClosePanel);
    }

    private void HandleChoiceSelected(int index)
    {
    }
}