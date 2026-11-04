using System;

namespace Ked.Samples.UI
{
    // 09 확인 · 10 안내 — 모달. 맨 위 Panel만 입력을 받는 UIPF 규칙과 딤으로 아래를 막는다. Esc는 확인 → 취소, 안내 → 확인(HandleCancel).
    // 무엇을 묻고 알리는지만 넘기고 글자는 창이 고른다. 수락하면 할 일은 부른 쪽이 넘기고 여기서 든다 — 모달이라 한 번에 하나다.
    // 먼저 팝업을 닫고 그다음 그 일을 한다 — 그 일이 새 Panel을 열 수 있게.
    public sealed partial class KedUIFlow
    {
        private Action _confirmAccepted;

        private void Confirm(ConfirmPanel.Question question, Action onAccept, int slotNumber = 0)
        {
            _confirmAccepted = onAccept;

            _ui.PushPanel<ConfirmPanel>(Presentation<ConfirmPanel>(), panel =>
            {
                BindView(panel, ApplyBindings);
                panel.Present(new ConfirmPanel.Model { Question = question, SlotNumber = slotNumber });
            });
        }

        private void Notice(NoticePanel.Message message, int slotNumber = 0)
        {
            _ui.PushPanel<NoticePanel>(Presentation<NoticePanel>(), panel =>
            {
                BindView(panel, ApplyBindings);
                panel.Present(new NoticePanel.Model { Message = message, SlotNumber = slotNumber });
            });
        }

        private void ApplyBindings(ConfirmPanel panel)
        {
            AddBinding(panel,
                p => p.Resolved += HandleConfirmResolved,
                p => p.Resolved -= HandleConfirmResolved);
        }

        private void ApplyBindings(NoticePanel panel)
        {
            AddBinding(panel,
                p => p.Resolved += HandleNoticeResolved,
                p => p.Resolved -= HandleNoticeResolved);
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

        private void HandleNoticeResolved()
        {
            if (_ui.TopPanel is NoticePanel)
                ClosePanel();
        }

        #endregion
    }
}
