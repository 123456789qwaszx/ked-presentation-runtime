public sealed partial class VnScreenBindings
{
    public void GoToLobby()
    {
        UI.SwitchRoot<LobbyUIRoot>(root =>
        {
            BindMain(root, BindLobbyRoot);
        });
    }

    private void BindLobbyRoot(LobbyUIRoot root)
    {
        AddBinding(
            root,
            r => r.OnOpenStory += OpenStorySelectFlow,
            r => r.OnOpenStory -= OpenStorySelectFlow);

        AddBinding(
            root,
            r => r.OnNextBroadcastRequested += OnNextBroadcastRequested,
            r => r.OnNextBroadcastRequested -= OnNextBroadcastRequested);
    }

    private void OnNextBroadcastRequested()
    {
    }

    private void OpenStorySelectFlow()
    {
        GoToChapterSelection();
    }
}