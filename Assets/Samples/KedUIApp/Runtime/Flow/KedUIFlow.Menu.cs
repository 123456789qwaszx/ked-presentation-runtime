using UnityEngine;

namespace Ked.Samples.UI
{
    // 04 시스템 메뉴 — 여닫는 것만 UI가. 일시 정지·복귀 정책(입력 소유권)은 흐름이, 재생 단위는 그대로.
    public sealed partial class KedUIFlow
    {
        private void OpenMenu()
        {
            PushPanel<SystemMenuPanel>(panel =>
            {
                BindView(panel, ApplyMenuBindings);
                panel.Present(new SystemMenuPanel.Model
                {
                    HasSaveSlots = _saves != null,
                    CanSave = _saves != null && _saves.CanSave,
                    CanSkipEpisode = IsRunning,
                });
            });
        }

        private void ApplyMenuBindings(SystemMenuPanel panel)
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
                p => p.SkipEpisodeClicked += ConfirmSkipEpisode,
                p => p.SkipEpisodeClicked -= ConfirmSkipEpisode);

            AddBinding(panel,
                p => p.TitleClicked += ConfirmReturnToTitle,
                p => p.TitleClicked -= ConfirmReturnToTitle);
        }

        private void ConfirmSkipEpisode()
        {
            Confirm(ConfirmPanel.Question.SkipEpisode, () =>
            {
                CloseAllPanels();

                if (Features.RequestSkipCurrentEpisode())
                    return;

                Debug.Log("[Ked UI] 지금 건너뛸 수 있는 에피소드가 없다.");
                Notice(NoticePanel.Message.SkipUnavailable);
            });
        }

        // 타이틀 복귀는 확인한 뒤에만 호스트를 부른다. 재생 단위를 끝내는 것은 호스트(ExitAsync)다.
        private void ConfirmReturnToTitle()
        {
            Confirm(ConfirmPanel.Question.ReturnToTitle, ReturnToTitle);
        }
    }
}
