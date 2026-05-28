using UnityEngine;

public sealed partial class VnScreenBindings
{
    private void OpenEpisodeSkipConfirmPanel()
    {
        string summary = "현재까지의 스토리(최근):";

        UI.PushPanel<SkipConfirmPanel>(panel =>
        {
            BindPanel(panel, ApplyBindings);

            panel.Present(
                title: "에피소드를 스킵할까요?",
                body: summary,
                confirmLabel: "스킵하고 완료",
                cancelLabel: "취소");
        });
    }

    private void ApplyBindings(SkipConfirmPanel panel)
    {
        AddBinding(panel,
            p => p.ConfirmClicked += HandleEpisodeSkipConfirmed,
            p => p.ConfirmClicked -= HandleEpisodeSkipConfirmed);

        AddBinding(panel,
            p => p.CloseClicked += ClosePanel,
            p => p.CloseClicked -= ClosePanel);
    }

    private void HandleEpisodeSkipConfirmed()
    {
        CloseAllPanels();

        string episodeId = _episodeSelectionSystem._selectionState.SelectedEpisodeId;
        
        if(episodeId == null)
            Debug.LogWarning("[VN] Episode skip confirmed but selected episode id is empty.");

        _vnRuntimeBridge.ForceCompleteEpisodeNow(episodeId);
        _episodeSelectionSystem.MarkEpisodeCompleted(episodeId);

        // 임시
        GoToLobby();
    }
}