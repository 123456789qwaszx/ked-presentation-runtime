using System;

namespace Ked.Samples.UI
{
    public sealed partial class KedUIFlow
    {
        private Action _confirmAccepted;

        private void OpenConfirm(ConfirmPanel.Question question, Action onAccept, int slotNumber = 0)
        {
            _confirmAccepted = onAccept;

            _ui.PushPanel<ConfirmPanel>(Presentation<ConfirmPanel>(), panel =>
            {
                BindView(panel, ApplyBindings);
                panel.Present(
                    new ConfirmPanel.Model
                    {
                        Question = question,
                        SlotNumber = slotNumber
                    });
            });
        }

        private void ApplyBindings(ConfirmPanel panel)
        {
            AddBinding(panel,
                p => p.Resolved += HandleConfirmResolved,
                p => p.Resolved -= HandleConfirmResolved);
        }

        #region Handlers

        private void HandleConfirmResolved(bool accepted)
        {
            Action accept = _confirmAccepted;
            _confirmAccepted = null;

            if (_ui.TopPanel is ConfirmPanel)
                ClosePanel();

            if (accepted)
                accept?.Invoke();
        }

        #endregion
    }
}
