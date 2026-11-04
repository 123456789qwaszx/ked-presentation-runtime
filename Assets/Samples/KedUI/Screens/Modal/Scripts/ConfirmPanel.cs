using System;
using UnityEngine;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 09 확인 팝업. UIPF Panel로 쌓는다 — Panel 스택의 맨 위만 입력을 받고(아래 Panel은 UIPF가 막는다),
    /// 딤이 화면 전체의 포인터를 막는다. 그래서 "모달"의 입력 차단은 UIPF의 Panel 규칙으로 얻는다.
    /// (UIPF의 Overlay는 아래 화면을 막지 않는 층이라 모달에 쓰지 않는다.)
    ///
    /// 무엇을 묻는지(Question)와 번호만 받는다. 글자는 이 창이 제 글자 표 키로 고른다(ILocalizedView).
    /// 수락하면 무엇을 할지는 부른 쪽(흐름)이 든다. 버튼은 누른 쪽을 Resolved로 알리기만 한다 —
    /// 한 번만 처리하는 것과 Esc의 뜻은 흐름이 정한다(KedUIFlow.OpenConfirm · HandleCancel).
    /// 버튼은 OnInitialize에서 한 번만 묶는다. 다시 열어도 리스너가 늘지 않는다.
    /// </summary>
    [UIViewKey("Ked.ConfirmPanel")]
    public sealed class ConfirmPanel : UIPanel<ConfirmPanel.Refs>, ILocalizedView
    {
        public enum Refs
        {
            SafeAreaRoot,
            ConfirmDim,
            ConfirmPopup,
            ConfirmTitle,
            ConfirmBody,
            ConfirmCancel,
            ConfirmCancelLabel,
            ConfirmAccept,
            ConfirmAcceptLabel,
        }

        public enum Question
        {
            SkipEpisode,
            ReturnToTitle,
            Save,
            Overwrite,
            Load,
            Fork,
        }

        public sealed class Model
        {
            public Question Question;
            public int SlotNumber;      // 저장 · 덮어쓰기 · 불러오기 본문의 번호(1부터)
        }

        /// <summary>요청이 끝났다(수락이면 true). 흐름이 이 Panel을 닫고 부른 쪽의 일을 한다.</summary>
        public event Action<bool> Resolved;

        private Model _model;
        private Localizer _texts;

        protected override void OnInitialize()
        {
            KedUIUtil.OnClick(View.Button(Refs.ConfirmAccept), () => Resolved?.Invoke(true));
            KedUIUtil.OnClick(View.Button(Refs.ConfirmCancel), () => Resolved?.Invoke(false));

            Styled.AddText(View.Text(Refs.ConfirmBody), UITextRole.Body, UITextSizePolicy.Prefab);
        }

        public void Present(Model model)
        {
            _model = model;
            Redraw();

            KedUIUtil.Focus(View.Button(Refs.ConfirmCancel));
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

            string key = KeyOf(_model.Question);
            bool numbered = _model.Question is Question.Save or Question.Overwrite or Question.Load;

            KedUIUtil.SetText(View.Text(Refs.ConfirmTitle), Text(key + ".title"));
            KedUIUtil.SetText(View.Text(Refs.ConfirmBody), numbered
                ? KedUIUtil.Format(_texts, key + ".body", KedUIUtil.TwoDigits(_model.SlotNumber))
                : Text(key + ".body"));
            KedUIUtil.SetText(View.Text(Refs.ConfirmAcceptLabel), Text(key + ".accept"));

            // 타이틀로만 취소가 "계속하기"다.
            KedUIUtil.SetText(View.Text(Refs.ConfirmCancelLabel),
                Text(_model.Question == Question.ReturnToTitle ? "confirm.title.cancel" : "common.cancel"));
        }

        private static string KeyOf(Question question) => question switch
        {
            Question.SkipEpisode => "confirm.skip",
            Question.ReturnToTitle => "confirm.title",
            Question.Save => "confirm.save",
            Question.Overwrite => "confirm.overwrite",
            Question.Load => "confirm.load",
            _ => "confirm.fork",
        };

        private string Text(string key) => KedUIUtil.Text(_texts, key);
    }
}
