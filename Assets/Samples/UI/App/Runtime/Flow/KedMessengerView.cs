using System.Collections.Generic;
using System.Threading;
using Yarn.Unity;

namespace Ked.Samples.UI
{
    /// <summary>
    /// 본체의 메신저 계약(IPresentationMessengerView)을 재생 화면의 메신저 패널(PlayMessengerPanel) 위에 구현한다.
    /// 기록(MessengerEntry)을 화면 묶음의 말풍선 꼴로 옮겨 넘길 뿐이다 — 기록은 본체(MessengerLog)가 들고 있다.
    /// 입력 대기 표시(•••)는 계약 밖이다 — 대사창의 다음 표시처럼 화면 흐름(KedUIFlow.Play)이 건다.
    /// </summary>
    internal sealed class KedMessengerView : IPresentationMessengerView
    {
        private readonly PlayMessengerPanel _panel;
        private readonly List<PlayMessengerBubble> _bubbles = new();

        public KedMessengerView(PlayMessengerPanel panel)
        {
            _panel = panel;
        }

        public void Render(IReadOnlyList<MessengerEntry> entries, bool animateLast)
        {
            _bubbles.Clear();

            for (int i = 0; i < entries.Count; i++)
            {
                MessengerEntry entry = entries[i];

                _bubbles.Add(new PlayMessengerBubble
                {
                    speaker = entry.Speaker,
                    text = entry.Text,
                    mine = entry.Side == MessengerSide.Right,
                });
            }

            _panel.Render(_bubbles, animateLast);
        }

        public void SetVisibleImmediate(bool visible) => _panel.SetVisibleImmediate(visible);

        public void PrepareHidden() => _panel.PrepareHidden();

        public async YarnTask FadeInAsync(float duration, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return;

            _panel.PrepareHidden();

            await Effects
                .FadeAlphaAsync(_panel.Group, 0f, 1f, duration, token)
                .SuppressCancellationThrow();

            if (token.IsCancellationRequested)
                return;

            _panel.SetShown(true);
        }

        public async YarnTask FadeOutAsync(float duration, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return;

            await Effects
                .FadeAlphaAsync(_panel.Group, _panel.Group.alpha, 0f, duration, token)
                .SuppressCancellationThrow();

            if (token.IsCancellationRequested)
                return;

            _panel.SetShown(false);
        }
    }
}
