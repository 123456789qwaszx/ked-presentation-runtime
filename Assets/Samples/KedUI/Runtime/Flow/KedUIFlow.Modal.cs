namespace Ked.Samples.UI
{
    // 09 확인 · 10 안내 — 모달. 맨 위 Panel만 입력을 받는 UIPF 규칙과 딤으로 아래를 막는다.
    // 요청 하나에 콜백은 한 번(화면이 보장). 흐름은 먼저 팝업을 닫고 그다음 콜백을 부른다 — 콜백이 새 Panel을 열 수 있게.
    public sealed partial class KedUIFlow
    {
        private void SubscribeModals()
        {
            _confirm.Resolved += HandleConfirmResolved;
            _notice.Resolved += HandleNoticeResolved;
        }

        private void Confirm(ConfirmPanel.Request request)
        {
            PushPanel<ConfirmPanel>(panel => panel.Present(request));
        }

        private void Notice(string title, string body, NoticePanel.Kind kind, System.Action onClosed = null)
        {
            NoticePanel.Request request = new()
            {
                Title = title,
                Body = body,
                AcceptLabel = Text("common.ok"),
                Kind = kind,
                OnClosed = onClosed,
            };

            PushPanel<NoticePanel>(panel => panel.Present(request));
        }

        private void HandleConfirmResolved(ConfirmPanel.Request request, bool accepted)
        {
            if (_ui.TopPanel == _confirm)
                ClosePanel();

            if (accepted)
                request.OnAccept?.Invoke();
            else
                request.OnCancel?.Invoke();
        }

        private void HandleNoticeResolved(NoticePanel.Request request)
        {
            if (_ui.TopPanel == _notice)
                ClosePanel();

            request.OnClosed?.Invoke();
        }
    }
}
