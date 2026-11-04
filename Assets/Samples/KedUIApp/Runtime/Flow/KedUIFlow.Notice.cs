namespace Ked.Samples.UI
{
    public sealed partial class KedUIFlow
    {
        private void OpenNotice(NoticePanel.Message message, int slotNumber = 0)
        {
            _ui.PushPanel<NoticePanel>(Presentation<NoticePanel>(), panel =>
            {
                BindView(panel, ApplyBindings);
                panel.Present(
                    new NoticePanel.Model
                    {
                        Message = message, 
                        SlotNumber = slotNumber
                    });
            });
        }

        private void ApplyBindings(NoticePanel panel)
        {
            AddBinding(panel,
                p => p.Resolved += HandleNoticeResolved,
                p => p.Resolved -= HandleNoticeResolved);
        }

        #region Handlers

        private void HandleNoticeResolved()
        {
            if (_ui.TopPanel is NoticePanel)
                ClosePanel();
        }

        #endregion
    }
}
