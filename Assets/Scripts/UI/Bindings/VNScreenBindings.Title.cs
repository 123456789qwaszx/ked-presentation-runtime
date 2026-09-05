public sealed partial class VNScreenBindings
{
    private ScenePlaybackSession _scenePlaybackSession;

    public void ConfigureTitleView(
        ScenePlaybackSession scenePlaybackSession)
    {
        _scenePlaybackSession = scenePlaybackSession;
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