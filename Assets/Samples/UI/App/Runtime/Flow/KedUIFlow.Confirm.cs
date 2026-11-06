using System;

namespace Ked.Samples.UI
{
    public sealed partial class KedUIFlow
    {
        // 수락하면 할 일은 부르는 자리가 넘기고, 이 창의 묶음에 건다 — 창이 닫히면 묶음과 함께 사라진다.
        private void OpenConfirm(ConfirmPanel.Question question, Action onAccept, int slotNumber = 0)
        {
            _ui.PushPanel<ConfirmPanel>(Presentation<ConfirmPanel>(), panel =>
            {
                BindView(panel, p => ApplyBindings(p, onAccept));
                panel.Present(
                    new ConfirmPanel.Model
                    {
                        Question = question,
                        SlotNumber = slotNumber
                    });
            });
        }

        private void ApplyBindings(ConfirmPanel panel, Action onAccept)
        {
            // 먼저 닫고 그다음 할 일 진행. 그 일이 새 Panel을 열 수 있게.
            Action<bool> handleResolved = accepted =>
            {
                ClosePanel();

                if (accepted)
                    onAccept();
            };

            AddBinding(panel,
                p => p.Resolved += handleResolved,
                p => p.Resolved -= handleResolved);
        }
    }
}
