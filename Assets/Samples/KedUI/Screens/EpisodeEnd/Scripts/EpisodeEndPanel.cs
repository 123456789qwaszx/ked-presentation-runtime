using System;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 14 끝 카드 — 회차가 끝까지 갔을 때(에피소드 끝 · 챕터 끝). UIPF Panel이라 재생 화면(마지막 장면)이 딤 아래 그대로 보인다.
    /// 머리글 · 이름 · 설명 한 줄 · 결과(스탯 같은 것 — 없으면 감춘다)와 버튼 하나 "타이틀로" — 다음 회차가 없다.
    ///
    /// 끝을 정하는 것도, 언제 여는지도 앱이 정한다. 화면은 그리기와 사건 전달만.
    /// 닫기 버튼이 없다 — 닫으면 끝난 재생 화면에 남는다(앱도 Esc로 닫지 않는다).
    /// 버튼은 한 번만 받는다(타이틀로 나가는 동안 다시 눌러도 사건이 또 나가지 않는다).
    /// </summary>
    [UIViewKey("Ked.EpisodeEndPanel")]
    public sealed class EpisodeEndPanel : UIPanel<EpisodeEndPanel.Refs>
    {
        public enum Refs
        {
            SafeAreaRoot,
            EndDim,
            EndCard,
            EndKicker,
            EndAccent,
            EndTitle,
            EndDivider,
            EndBody,
            EndResultHeading,
            EndResult,
            EndTitleReturn,
            EndTitleReturnLabel,
        }

        public sealed class Model
        {
            /// <summary>머리글 — "에피소드 끝" · "챕터 끝".</summary>
            public string Kicker;

            /// <summary>끝난 것의 이름.</summary>
            public string Title;

            /// <summary>이름 아래 한 줄(마지막 에피소드 · 안내).</summary>
            public string Body;

            /// <summary>결과 — 줄바꿈으로 여러 줄. 비면 결과 칸(제목 포함)을 감춘다.</summary>
            public string Result;
        }

        public event Action TitleClicked;

        private bool _pending;

        protected override void OnInitialize()
        {
            KedUIUtil.OnClick(View.Button(Refs.EndTitleReturn), HandleTitle);

            Styled.AddText(View.Text(Refs.EndBody), UITextRole.Body, UITextSizePolicy.Prefab);
            Styled.AddText(View.Text(Refs.EndResult), UITextRole.Body, UITextSizePolicy.Prefab);
        }

        public void Present(Model model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            KedUIUtil.SetText(View.Text(Refs.EndKicker), model.Kicker);
            KedUIUtil.SetText(View.Text(Refs.EndTitle), model.Title);
            KedUIUtil.SetText(View.Text(Refs.EndBody), model.Body);

            bool hasResult = !string.IsNullOrWhiteSpace(model.Result);
            KedUIUtil.SetActive(View.Text(Refs.EndResultHeading), hasResult);
            KedUIUtil.SetActive(View.Text(Refs.EndResult), hasResult);
            KedUIUtil.SetText(View.Text(Refs.EndResult), hasResult ? model.Result : string.Empty);

            _pending = true;
            KedUIUtil.SetButton(View.Button(Refs.EndTitleReturn), true, true);
            KedUIUtil.Focus(View.Button(Refs.EndTitleReturn));
        }

        private void HandleTitle()
        {
            if (!_pending)
                return;

            _pending = false;
            KedUIUtil.SetButton(View.Button(Refs.EndTitleReturn), true, false);

            TitleClicked?.Invoke();
        }

        private void OnDisable()
        {
            _pending = false;
        }
    }
}
