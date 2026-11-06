namespace Ked.Samples.UI
{
    public sealed partial class KedUIFlow
    {
        private void OpenMenu()
        {
            _ui.PushPanel<SystemMenuPanel>(Presentation<SystemMenuPanel>(), panel =>
            {
                BindView(panel, ApplyBindings);
                RefreshMenu(panel);
            });
        }

        private void ApplyBindings(SystemMenuPanel panel)
        {
            AddBinding(panel,
                p => p.CloseClicked += ClosePanel,
                p => p.CloseClicked -= ClosePanel);

            AddBinding(panel,
                p => p.SaveClicked += OpenSave,
                p => p.SaveClicked -= OpenSave);

            AddBinding(panel,
                p => p.LoadClicked += OpenLoad,
                p => p.LoadClicked -= OpenLoad);

            AddBinding(panel,
                p => p.SettingsClicked += OpenSettings,
                p => p.SettingsClicked -= OpenSettings);

            AddBinding(panel,
                p => p.SkipEpisodeClicked += HandleSkipEpisodeClicked,
                p => p.SkipEpisodeClicked -= HandleSkipEpisodeClicked);

            AddBinding(panel,
                p => p.TitleClicked += HandleTitleClicked,
                p => p.TitleClicked -= HandleTitleClicked);
        }

        #region Handlers

        private void HandleSkipEpisodeClicked()
        {
            OpenConfirm(ConfirmPanel.Question.SkipEpisode, () =>
            {
                CloseAllPanels();

                if (Features.RequestSkipCurrentEpisode())
                    return;

                OpenNotice(NoticePanel.Message.SkipUnavailable);
            });
        }

        private void HandleTitleClicked()
        {
            OpenConfirm(ConfirmPanel.Question.ReturnToTitle, ReturnToTitle);
        }

        #endregion

        #region Refresh

        private void RefreshMenu(SystemMenuPanel panel)
        {
            panel.Present(new SystemMenuPanel.Model
            {
                HasSaveSlots = _saves != null,
                CanSave = _saves != null && _saves.CanSave,
                CanSkipEpisode = IsRunning,
            });
        }

        #endregion
    }
}
