public sealed partial class VnScreenBindings
{
    #region EpisodeSelectionPanel
    
    private void OpenEpisodeConfirmPanel()
    {
        string episodeId = _episodeSelectionSystem.SelectionState.SelectedEpisodeId;
        string dialogueEntryId = _episodeSelectionSystem.GetSelectedDialogueEntryId();
        string summary = $"현재 에피소드: {episodeId}\n" +
                         $"실행 엔트리: {dialogueEntryId}";

        UI.PushPanel<ConfirmPanel>(panel =>
        {
            BindPanel(panel, openEpisode =>
                {
                    AddBinding(openEpisode,
                        p => p.ConfirmClicked += HandleEpisodeStartConfirmed,
                        p => p.ConfirmClicked -= HandleEpisodeStartConfirmed);

                    AddBinding(openEpisode,
                        p => p.CloseClicked += ClosePanel,
                        p => p.CloseClicked -= ClosePanel);
                });

            panel.Present(
                title: "에피소드를 시작할까요??",
                body: summary,
                confirmLabel: "확인",
                cancelLabel: "취소");
        });
    }

    private void HandleEpisodeStartConfirmed()
    {
        string dialogueEntryId = _episodeSelectionSystem.GetSelectedDialogueEntryId();
        if (string.IsNullOrEmpty(dialogueEntryId))
            return;
        
        CloseAllPanels();
        
        _episodePlayer.StartGame(dialogueEntryId);
    }
    #endregion
}