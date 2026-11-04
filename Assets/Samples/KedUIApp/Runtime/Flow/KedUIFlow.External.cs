using Ked.Progression;

namespace Ked.Samples.UI
{
    // 바깥(Unity 입력 · 재생 · 진행)이 흐름을 부르는 입구. 몸은 각 화면 파일의 private 처리에 있고, 여기는 잇기만 한다.
    public sealed partial class KedUIFlow
    {
        // KedUIHost가 Esc를 읽어 부른다.
        public void OnCancelPressed() => HandleCancel();

        // KedUIHost가 매 프레임 부른다 — 재생 상태에 바뀜을 알리는 이벤트가 없다.
        public void SyncPlaybackState() => RefreshPlaybackState();

        // 시작 · 타이틀 복귀.
        public void GoToTitle() => HandleGoToTitle();

        // IPresentationView — 재생이 시작될 때 본체가 부른다.
        public void GoToPresentationView() => OpenPlay();

        // IChapterEndView — 챕터가 끝까지 갔을 때 SceneRunner가 부른다.
        void IChapterEndView.Show(ChapterDefinition chapter, EpisodeNode lastEpisode, ChapterState state)
            => HandleChapterEnd(chapter, lastEpisode, state);
    }
}
