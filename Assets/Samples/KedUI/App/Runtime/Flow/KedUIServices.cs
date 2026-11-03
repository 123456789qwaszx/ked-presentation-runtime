namespace Ked.Samples.UI
{
    // 화면 바인딩들이 같이 쓰는 재생·호스트 쪽과 UI 글자 표. 재생·호스트는 KedUIFlow.Configure에서 이어진다 — 그 전엔 비어 있다.
    internal sealed class KedUIServices
    {
        public KedUIServices(Localizer texts)
        {
            Texts = texts;
        }

        public Localizer Texts { get; }
        public KedPlaybackBindings Playback { get; private set; }
        public IKedTitlePort Title { get; private set; }

        // 저장 슬롯이 없는 호스트에서 null(예제는 늘 넘긴다).
        public IKedSaveSlotPort Slots { get; private set; }
        public IKedStagePort Stage { get; private set; }

        public bool IsRunning => Playback?.Host != null && Playback.Host.IsRunning;

        public void Configure(
            KedPlaybackBindings playback,
            IKedTitlePort title,
            IKedSaveSlotPort slots,
            IKedStagePort stage)
        {
            Playback = playback;
            Title = title;
            Slots = slots;
            Stage = stage;
        }

        // 빨리 넘기기는 버튼으로 켜면(Ctrl과 달리) 스스로 꺼지지 않는다. 회차를 떠나거나 바꿀 때 끈다 —
        // 그러지 않으면 다음 회차의 첫 줄부터 넘어간다. 자동 진행은 옛 UI와 같이 둔다.
        public void StopFastForward()
        {
            if (Playback?.PlaybackState != null && Playback.PlaybackState.IsRapidSkipMode)
                Playback.Features?.EndRapidSkip();
        }

        public string Text(string key) => KedUIUtil.Text(Texts, key);
        public string Format(string key, params object[] args) => KedUIUtil.Format(Texts, key, args);
    }
}
