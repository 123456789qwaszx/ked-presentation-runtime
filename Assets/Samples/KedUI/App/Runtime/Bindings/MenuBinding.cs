using UnityEngine;

namespace Ked.Samples.UI
{
    // 04 시스템 메뉴 — 여닫는 것만 UI가. 일시 정지·복귀 정책(입력 소유권)은 흐름이, 재생 단위는 그대로.
    internal sealed class MenuBinding : ScreenBinding
    {
        public MenuBinding(KedUIFlow flow, KedUIServices services)
            : base(flow, services)
        {
            Own(flow.Require<SystemMenuPanel>());
        }

        public void Open()
        {
            IKedSaveSlotPort slots = Services.Slots;

            Flow.PushPanel<SystemMenuPanel>(panel =>
            {
                BindView(panel, ApplyBindings);
                panel.Present(new SystemMenuPanel.Model
                {
                    HasSaveSlots = slots != null,
                    CanSave = slots != null && slots.CanSave,
                    CanSkipEpisode = Services.IsRunning,
                });
            });
        }

        private void ApplyBindings(SystemMenuPanel panel)
        {
            AddBinding(panel,
                p => p.CloseClicked += Flow.ClosePanel,
                p => p.CloseClicked -= Flow.ClosePanel);

            AddBinding(panel,
                p => p.SaveClicked += Flow.OpenSave,
                p => p.SaveClicked -= Flow.OpenSave);

            AddBinding(panel,
                p => p.LoadClicked += Flow.OpenLoad,
                p => p.LoadClicked -= Flow.OpenLoad);

            AddBinding(panel,
                p => p.SettingsClicked += Flow.OpenSettings,
                p => p.SettingsClicked -= Flow.OpenSettings);

            AddBinding(panel,
                p => p.SkipEpisodeClicked += ConfirmSkipEpisode,
                p => p.SkipEpisodeClicked -= ConfirmSkipEpisode);

            AddBinding(panel,
                p => p.TitleClicked += ConfirmReturnToTitle,
                p => p.TitleClicked -= ConfirmReturnToTitle);
        }

        private void ConfirmSkipEpisode()
        {
            Flow.Confirm(new ConfirmPanel.Request
            {
                Title = Text("confirm.skip.title"),
                Body = Text("confirm.skip.body"),
                AcceptLabel = Text("confirm.skip.accept"),
                CancelLabel = Text("common.cancel"),
                OnAccept = () =>
                {
                    Flow.CloseAllPanels();

                    if (Services.Playback.Features.RequestSkipCurrentEpisode())
                        return;

                    Debug.Log("[Ked UI] 지금 건너뛸 수 있는 에피소드가 없다.");
                    Flow.Notice(Text("notice.skipUnavailable.title"), Text("notice.skipUnavailable.body"), NoticePanel.Kind.Failure);
                },
            });
        }

        // 타이틀 복귀는 확인한 뒤에만 호스트를 부른다. 재생 단위를 끝내는 것은 호스트(ExitAsync)다.
        private void ConfirmReturnToTitle()
        {
            Flow.Confirm(new ConfirmPanel.Request
            {
                Title = Text("confirm.title.title"),
                Body = Text("confirm.title.body"),
                AcceptLabel = Text("confirm.title.accept"),
                CancelLabel = Text("confirm.title.cancel"),
                OnAccept = Flow.ReturnToTitle,
            });
        }
    }
}
