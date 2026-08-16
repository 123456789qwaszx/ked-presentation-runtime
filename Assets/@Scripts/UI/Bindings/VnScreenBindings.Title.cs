public sealed partial class VnScreenBindings
{
    private EpisodePlayer _episodePlayer;

    public void ConfigureTitleView(EpisodePlayer episodePlayer)
    {
        _episodePlayer = episodePlayer;
    }

    // TitleUIRoot는 현재 이벤트를 내보내지 않는다(버튼 위젯이 없다).
    // 여기서 붙일 바인딩도 그래서 없다 — 버튼이 돌아오면 AddBinding으로 다시 잇는다.
    private void GoToTitle()
    {
        UI.SwitchRoot<TitleUIRoot>();
    }
}
