using System;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 10 안내 팝업 — 확인 버튼 하나. 확인 팝업과 같은 Panel 규칙(모달). 버튼은 Resolved로 알리기만 하고,
    /// 닫기와 Esc의 뜻은 흐름이 정한다(KedUIFlow.HandleNoticeResolved · HandleCancel).
    /// 무엇을 알리는지(Message)와 번호만 받는다. 글자와 표시는 이 창이 정한다(ILocalizedView).
    /// 성공 표시(체크)는 실제로 끝난 일에만 쓴다(저장 완료). 실패는 체크 대신 경고 표시와 실제 이유.
    /// </summary>
    [UIViewKey("Ked.NoticePanel")]
    public sealed class NoticePanel : UIPanel<NoticePanel.Refs>, ILocalizedView
    {
        public enum Refs
        {
            SafeAreaRoot,
            NoticeDim,
            NoticePopup,
            NoticeTitle,
            NoticeCheck,
            NoticeWarning,
            NoticeBody,
            NoticeAccept,
            NoticeAcceptLabel,
        }

        public enum Message
        {
            Saved,
            SaveFailed,
            LoadFailed,
            ForkFailed,
            SkipUnavailable,
        }

        public sealed class Model
        {
            public Message Message;
            public int SlotNumber;      // 저장 완료 본문의 번호(1부터)
        }

        public event Action Resolved;

        private Model _model;
        private Localizer _texts;

        protected override void OnInitialize()
        {
            KedUIUtil.OnClick(View.Button(Refs.NoticeAccept), () => Resolved?.Invoke());

            Styled.AddText(View.Text(Refs.NoticeBody), UITextRole.Body, UITextSizePolicy.Prefab);
        }

        public void Present(Model model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            Redraw();

            KedUIUtil.Focus(View.Button(Refs.NoticeAccept));
        }

        public void Relocalize(Localizer localizer)
        {
            _texts = localizer;
            Redraw();
        }

        private void Redraw()
        {
            if (_model == null)
                return;

            string key = KeyOf(_model.Message);
            bool success = _model.Message == Message.Saved;

            KedUIUtil.SetText(View.Text(Refs.NoticeTitle), Text(key + ".title"));
            KedUIUtil.SetText(View.Text(Refs.NoticeBody), success
                ? KedUIUtil.Format(_texts, key + ".body", KedUIUtil.TwoDigits(_model.SlotNumber))
                : Text(key + ".body"));
            KedUIUtil.SetText(View.Text(Refs.NoticeAcceptLabel), Text("common.ok"));

            KedUIUtil.SetActive(View.Image(Refs.NoticeCheck), success);
            KedUIUtil.SetActive(View.Image(Refs.NoticeWarning), !success);
        }

        private static string KeyOf(Message message) => message switch
        {
            Message.Saved => "notice.saved",
            Message.SaveFailed => "notice.saveFailed",
            Message.LoadFailed => "notice.loadFailed",
            Message.ForkFailed => "notice.forkFailed",
            _ => "notice.skipUnavailable",
        };

        private string Text(string key) => KedUIUtil.Text(_texts, key);
    }
}
