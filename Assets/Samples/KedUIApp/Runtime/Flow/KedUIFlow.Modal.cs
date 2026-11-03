using System;

namespace Ked.Samples.UI
{
    // 09 확인 · 10 안내 — 모달. 맨 위 Panel만 입력을 받는 UIPF 규칙과 딤으로 아래를 막는다. Esc는 확인 → 취소, 안내 → 확인(HandleCancel).
    // 요청 하나에 콜백은 한 번(화면이 보장). 먼저 팝업을 닫고 그다음 콜백을 부른다 — 콜백이 새 Panel을 열 수 있게.
    public sealed partial class KedUIFlow
    {
        private readonly ConfirmPanel _confirmPanel;
        private readonly NoticePanel _noticePanel;

        private void Confirm(ConfirmPanel.Request request)
        {
            PushPanel<ConfirmPanel>(panel =>
            {
                BindView(panel, ApplyConfirmBindings);
                panel.Present(request);
            });
        }

        private void Notice(string title, string body, NoticePanel.Kind kind, Action onClosed = null)
        {
            NoticePanel.Request request = new()
            {
                Title = title,
                Body = body,
                AcceptLabel = Text("common.ok"),
                Kind = kind,
                OnClosed = onClosed,
            };

            PushPanel<NoticePanel>(panel =>
            {
                BindView(panel, ApplyNoticeBindings);
                panel.Present(request);
            });
        }

        private void ApplyConfirmBindings(ConfirmPanel panel)
        {
            AddBinding(panel,
                p => p.Resolved += HandleConfirmResolved,
                p => p.Resolved -= HandleConfirmResolved);
        }

        private void ApplyNoticeBindings(NoticePanel panel)
        {
            AddBinding(panel,
                p => p.Resolved += HandleNoticeResolved,
                p => p.Resolved -= HandleNoticeResolved);
        }

        private void HandleConfirmResolved(ConfirmPanel.Request request, bool accepted)
        {
            if (IsTop(_confirmPanel))
                ClosePanel();

            if (accepted)
                request.OnAccept?.Invoke();
            else
                request.OnCancel?.Invoke();
        }

        private void HandleNoticeResolved(NoticePanel.Request request)
        {
            if (IsTop(_noticePanel))
                ClosePanel();

            request.OnClosed?.Invoke();
        }
    }
}
