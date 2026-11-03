using Yarn.Unity;

namespace Ked.Samples.UI
{
    /// <summary>재생 쪽에서 UI가 직접 쓰는 것. 조립(PresentationAssembler)이 만든 것을 호스트(SampleBootstrap)가 모아 넘긴다.</summary>
    public sealed class KedPlaybackBindings
    {
        public PlaybackFeatureController Features;
        public DialogueAdvanceDispatcher AdvanceDispatcher;
        public LinePresentationFlow LineFlow;
        public AdvanceInputPoller InputPoller;

        // 상태 읽기(퀵메뉴의 켜짐 표시)와 설정(자동 대기·빠르게 배속).
        public PlaybackRuntimeState PlaybackState;
        public EllipsisBreathTypewriter Typewriter;
        public AudioSystem Audio;

        // 백로그의 화자 이름(대본의 "이름: 대사"에서 읽는다).
        public DialogueRunner DialogueRunner;
    }
}
