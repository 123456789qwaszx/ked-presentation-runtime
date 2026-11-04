using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ked.Samples.UI
{
    public sealed partial class KedUIFlow
    {
        private readonly List<DialogueLogEntry> _backlogEntries = new();
        private YarnSpeakerNames _backlogSpeakers;

        // 다시 듣는 줄의 순번. 없으면 -1. 패널을 다시 열어도 같은 줄을 켜 두기 위함
        private int _backlogVoiceSequence = -1;

        private void ConfigureBacklog()
        {
            _backlogSpeakers = new YarnSpeakerNames(_dialogueRunner);
        }

        private void OpenBacklog()
        {
            _ui.PushPanel<BacklogPanel>(Presentation<BacklogPanel>(), panel =>
            {
                BindView(panel, ApplyBindings);
                RefreshBacklog(panel);
            });
        }

        private void ApplyBindings(BacklogPanel panel)
        {
            AddBinding(panel,
                p => p.CloseClicked += ClosePanel,
                p => p.CloseClicked -= ClosePanel);

            AddBinding(panel,
                p => p.JumpClicked += HandleBacklogJump,
                p => p.JumpClicked -= HandleBacklogJump);

            AddBinding(panel,
                p => p.VoiceClicked += HandleBacklogVoice,
                p => p.VoiceClicked -= HandleBacklogVoice);
        }

        #region Handlers

        private async void HandleBacklogJump(int index)
        {
            try
            {
                if (index < 0 || index >= _backlogEntries.Count || !IsRunning)
                    return;

                DialogueLogEntry entry = _backlogEntries[index];

                // 같은 장면: 확정 전이라 되감기.
                if (Features.CanJumpTo(entry))
                {
                    if (!Features.RequestBacklogJump(entry))
                        return;

                    CloseAllPanels();
                    await _launcher.RequestReplayAsync();
                    return;
                }

                // 과거 장면: 새 회차가 생긴다 — 확인한 뒤에만.
                if (!_saves.CanForkFrom(entry))
                    return;

                OpenConfirm(ConfirmPanel.Question.Fork, () => ForkFromBacklog(entry));
            }
            catch (Exception error)
            {
                Debug.LogException(error);
            }
        }

        private void HandleBacklogVoice(int index)
        {
            if (index < 0 || index >= _backlogEntries.Count || !IsRunning)
                return;

            DialogueLogEntry entry = _backlogEntries[index];

            if (entry.lineSequence == _backlogVoiceSequence && Features.IsVoiceReplaying)
            {
                Features.StopVoiceReplay();
                _backlogVoiceSequence = -1;
            }
            else
            {
                int curSequence = Features.ReplayVoice(entry)
                    ? entry.lineSequence 
                    : -1;

                _backlogVoiceSequence = curSequence;
            }

            _ui.GetUI<BacklogPanel>().ShowVoicePlaying(_backlogVoiceSequence >= 0 
                ? index 
                : -1);
        }

        private async void ForkFromBacklog(DialogueLogEntry entry)
        {
            try
            {
                await _saves.ForkFromBacklogAsync(entry, () =>
                {
                    StopFastForward();
                    CloseAllPanels();
                });
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                OpenNotice(NoticePanel.Message.ForkFailed);
            }
        }

        #endregion

        #region Refresh

        private void RefreshBacklog(BacklogPanel panel)
        {
            _backlogEntries.Clear();

            IReadOnlyList<DialogueLogEntry> entries = Features.Backlogs;

            for (int i = 0; i < entries.Count; i++)
                _backlogEntries.Add(entries[i]);

            List<BacklogRow.Model> rows = new(_backlogEntries.Count);

            for (int i = 0; i < _backlogEntries.Count; i++)
            {
                DialogueLogEntry entry = _backlogEntries[i];

                rows.Add(new BacklogRow.Model
                {
                    Speaker = _backlogSpeakers?.Find(entry.lineId),
                    Text = entry.rawText,
                    CanJump = Features.CanJumpTo(entry) || _saves.CanForkFrom(entry),
                    HasVoice = Features.CanReplayVoice(entry),
                    VoicePlaying = Features.IsVoiceReplaying && entry.lineSequence == _backlogVoiceSequence,
                    IsCurrent = i == _backlogEntries.Count - 1,
                    ShowDivider = i < _backlogEntries.Count - 1,
                });
            }

            panel.Present(rows);
        }

        // 다시 듣던 보이스가 저절로 끝났거나(다음 줄 · 롤백 포함) 다른 보이스가 덮었으면 그림을 끈다.
        // 보이스가 끝났다는 이벤트가 없어 매 프레임 본다.
        private void TickBacklog()
        {
            if (_backlogVoiceSequence < 0)
                return;

            if (IsRunning && Features.IsVoiceReplaying)
                return;

            _backlogVoiceSequence = -1;
            _ui.GetUI<BacklogPanel>().ShowVoicePlaying(-1);
        }
        
        #endregion
    }
}
