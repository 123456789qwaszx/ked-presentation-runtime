public sealed partial class VNScreenBindings
{
    private EpisodePlayer _episodePlayer;

    public void ConfigureTitleView(EpisodePlayer episodePlayer)
    {
        _episodePlayer = episodePlayer;
    }

    private void GoToTitle()
    {
        UI.SwitchRoot<TitleUIRoot>(root =>
        {
            BindMain(root, ApplyBindings);
        });
    }

    private void ApplyBindings(TitleUIRoot root)
    {
    }
}