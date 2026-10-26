using System;
using UnityEngine;

namespace Ked.Samples.UI
{
    // 04 시스템 메뉴 — 여닫는 것만 UI가. 일시 정지·복귀 정책(입력 소유권)은 흐름이, 재생 단위는 그대로.
    public sealed partial class KedUIFlow
    {
        private void SubscribeMenu()
        {
            _menu.CloseClicked += ClosePanel;
            _menu.SaveClicked += () => OpenSlots(SlotMode.Save);
            _menu.LoadClicked += () => OpenSlots(SlotMode.Load);
            _menu.SettingsClicked += OpenSettings;
            _menu.SkipEpisodeClicked += ConfirmSkipEpisode;
            _menu.TitleClicked += ConfirmReturnToTitle;
        }

        private void OpenMenu()
        {
            PushPanel<SystemMenuPanel>(panel => panel.Present(new SystemMenuPanel.Model
            {
                HasSaveSlots = _slotPort != null,
                CanSave = _slotPort != null && _slotPort.CanSave,
                CanSkipEpisode = IsRunning,
            }));
        }

        private void ConfirmSkipEpisode()
        {
            Confirm(new ConfirmPanel.Request
            {
                Title = Text("confirm.skip.title"),
                Body = Text("confirm.skip.body"),
                AcceptLabel = Text("confirm.skip.accept"),
                CancelLabel = Text("common.cancel"),
                OnAccept = () =>
                {
                    CloseAllPanels();

                    if (_playback.Features.RequestSkipCurrentEpisode())
                        return;

                    Debug.Log("[Ked UI] 지금 건너뛸 수 있는 에피소드가 없다.");
                    Notice(Text("notice.skipUnavailable.title"), Text("notice.skipUnavailable.body"), NoticePanel.Kind.Failure);
                },
            });
        }

        // 타이틀 복귀는 확인한 뒤에만 호스트를 부른다. 재생 단위를 끝내는 것은 호스트(ExitAsync)다.
        private void ConfirmReturnToTitle()
        {
            Confirm(new ConfirmPanel.Request
            {
                Title = Text("confirm.title.title"),
                Body = Text("confirm.title.body"),
                AcceptLabel = Text("confirm.title.accept"),
                CancelLabel = Text("confirm.title.cancel"),
                OnAccept = ReturnToTitle,
            });
        }

        private async void ReturnToTitle()
        {
            try
            {
                CloseAllPanels();

                if (_playback?.Host != null)
                    await _playback.Host.ExitAsync();

                GoToTitle();
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                GoToTitle();
            }
        }
    }
}
