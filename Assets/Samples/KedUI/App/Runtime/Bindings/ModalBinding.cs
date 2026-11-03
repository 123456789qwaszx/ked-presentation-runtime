using System;

namespace Ked.Samples.UI
{
    // 09 확인 · 10 안내 — 모달. 맨 위 Panel만 입력을 받는 UIPF 규칙과 딤으로 아래를 막는다.
    // 요청 하나에 콜백은 한 번(화면이 보장). 먼저 팝업을 닫고 그다음 콜백을 부른다 — 콜백이 새 Panel을 열 수 있게.
    internal sealed class ModalBinding : ScreenBinding
    {
        private readonly ConfirmPanel _confirm;
        private readonly NoticePanel _notice;

        public ModalBinding(KedUIFlow flow, KedUIServices services)
            : base(flow, services)
        {
            _confirm = Own(flow.Require<ConfirmPanel>());
            _notice = Own(flow.Require<NoticePanel>());
        }

        public void Confirm(ConfirmPanel.Request request)
        {
            Flow.PushPanel<ConfirmPanel>(panel =>
            {
                BindView(panel, ApplyConfirmBindings);
                panel.Present(request);
            });
        }

        public void Notice(string title, string body, NoticePanel.Kind kind, Action onClosed = null)
        {
            NoticePanel.Request request = new()
            {
                Title = title,
                Body = body,
                AcceptLabel = Text("common.ok"),
                Kind = kind,
                OnClosed = onClosed,
            };

            Flow.PushPanel<NoticePanel>(panel =>
            {
                BindView(panel, ApplyNoticeBindings);
                panel.Present(request);
            });
        }

        // Esc: 확인 → 취소, 안내 → 확인.
        public override void Back(UIBase view)
        {
            if (view == _confirm)
                _confirm.Cancel();
            else if (view == _notice)
                _notice.Accept();
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
            if (Flow.IsTop(_confirm))
                Flow.ClosePanel();

            if (accepted)
                request.OnAccept?.Invoke();
            else
                request.OnCancel?.Invoke();
        }

        private void HandleNoticeResolved(NoticePanel.Request request)
        {
            if (Flow.IsTop(_notice))
                Flow.ClosePanel();

            request.OnClosed?.Invoke();
        }
    }
}
