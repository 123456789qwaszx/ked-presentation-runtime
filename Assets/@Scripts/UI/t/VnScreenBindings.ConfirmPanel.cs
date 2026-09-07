using UnityEngine;

public sealed partial class VNScreenBindings
{
    #region EpisodeSelectionPanel
    
    private void OpenSkipConfirmPanel()
    {
        UI.PushPanel<SkipConfirmPanel>(panel =>
        {
            BindPanel(panel, openedPanel =>
            {
                AddBinding(
                    openedPanel,
                    p => p.ConfirmClicked += HandleSkipConfirmed,
                    p => p.ConfirmClicked -= HandleSkipConfirmed);

                AddBinding(
                    openedPanel,
                    p => p.CloseClicked += ClosePanel,
                    p => p.CloseClicked -= ClosePanel);
            });

            panel.Present(
                title: "현재 에피소드를 건너뛸까요?",
                body:
                "현재 재생 중인 에피소드의 남은 대사를 빠르게 진행합니다.\n" +
                "Yarn 선택지가 나오면 선택을 기다립니다.",
                confirmLabel: "스킵",
                cancelLabel: "취소");
        });
    }

    private void HandleSkipConfirmed()
    {
        ClosePanel();

        if (!_vnFeatures.RequestSkipCurrentEpisode())
            Debug.Log("[스킵] 현재 건너뛸 수 있는 Episode가 없다.");
    }
    #endregion
}