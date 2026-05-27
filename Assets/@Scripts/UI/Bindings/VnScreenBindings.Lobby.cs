public sealed partial class VnScreenBindings
{
    public void GoToLobby()
    {
        UI.SwitchRoot<LobbyUIRoot>(root =>
        {
            BindMain(root, BindLobbyRoot);
        });
    }

    private void BindLobbyRoot(LobbyUIRoot lobbyRoot)
    {
        BindEvent(
            lobbyRoot,
            l => l.OnOpenStory += OpenStorySelectFlow,
            l => l.OnOpenStory -= OpenStorySelectFlow);

        BindEvent(
            lobbyRoot,
            l => l.OnNextBroadcastRequested += OnNextBroadcastRequested,
            l => l.OnNextBroadcastRequested -= OnNextBroadcastRequested);
    }

    private void OnNextBroadcastRequested()
    {
    }

    private void OpenStorySelectFlow()
    {
        GoToChapterSelection();
    }
}