using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ked.Samples.UI
{
    // 08 백로그 — 기록은 재생 쪽 백로그를 그대로 읽는다. 줄로 가는 길은 둘이고 서로 다른 호스트 동작이다.
    //   지금 장면의 줄   → 되감기(PlaybackFeatureController.RequestBacklogJump → ProgressionLauncher.RequestReplayAsync)
    //   확정된 과거 장면 → 그 지점에서 새 회차로 갈라지기(ManualSaveFlow.ForkFromBacklogAsync). 확인을 받는다.
    // 갈 수 없는 줄(지금 줄 등)은 이동 버튼이 없다.
    // 보이스가 있는 줄은 다시 듣기 버튼이 있다 — 장면을 가리지 않고, 패널은 열어 둔다.
    // 재생 중인 줄은 켜짐 그림이고 다시 누르면 멈춘다. 재생이 저절로 끝나면 TickBacklog가 그림을 끈다.
    // 패널을 닫아도 보이스는 이어지고, 다음 줄에 들어가면 끝난다(줄 소속 이벤트).
    public sealed partial class KedUIFlow
    {
        private readonly List<DialogueLogEntry> _backlogEntries = new();
        private YarnSpeakerNames _backlogSpeakers;

        // 다시 듣는 줄의 순번. 없으면 -1. 패널을 다시 열어도 같은 줄을 켜 두려고 순번으로 든다.
        private int _backlogVoiceSequence = -1;

        private void ConfigureBacklog()
        {
            _backlogSpeakers = new YarnSpeakerNames(_playback.DialogueRunner);
        }

        // 다시 듣던 보이스가 저절로 끝났거나(다음 줄 · 롤백 포함) 다른 보이스가 덮었으면 그림을 끈다.
        private void TickBacklog()
        {
            if (_backlogVoiceSequence < 0)
                return;

            if (IsRunning && Features.IsVoiceReplaying)
                return;

            _backlogVoiceSequence = -1;
            _ui.GetUI<BacklogPanel>().ShowVoicePlaying(-1);
        }

        private void OpenBacklog()
        {
            _backlogEntries.Clear();

            IReadOnlyList<DialogueLogEntry> entries = Features?.Backlogs;
            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                    _backlogEntries.Add(entries[i]);
            }

            var rows = new List<BacklogRow.Model>(_backlogEntries.Count);
            int last = _backlogEntries.Count - 1;
            bool running = IsRunning;
            bool replaying = running && Features.IsVoiceReplaying;

            for (int i = 0; i < _backlogEntries.Count; i++)
            {
                DialogueLogEntry entry = _backlogEntries[i];

                rows.Add(new BacklogRow.Model
                {
                    Speaker = _backlogSpeakers?.Find(entry.lineId),
                    Text = entry.rawText,
                    CanJump = CanActOnBacklogLine(entry),
                    HasVoice = running && Features.CanReplayVoice(entry),
                    VoicePlaying = replaying && entry.lineSequence == _backlogVoiceSequence,
                    IsCurrent = i == last,
                    ShowDivider = i < last,
                });
            }

            _ui.PushPanel<BacklogPanel>(Presentation<BacklogPanel>(), panel =>
            {
                BindView(panel, ApplyBacklogBindings);
                panel.Present(rows);
            }, Unbind);
        }

        private void ApplyBacklogBindings(BacklogPanel panel)
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

        private bool CanActOnBacklogLine(DialogueLogEntry entry)
        {
            if (!IsRunning)
                return false;

            if (Features.CanJumpTo(entry))
                return true;

            return SafeCanFork(entry);
        }

        private bool SafeCanFork(DialogueLogEntry entry)
        {
            try
            {
                return _saves.CanForkFrom(entry);
            }
            catch (Exception error)
            {
                Debug.LogWarning($"[Ked UI] 갈라지기 가능 여부를 읽지 못했다 — {error.Message}");
                return false;
            }
        }

        private async void HandleBacklogJump(int index)
        {
            try
            {
                if (index < 0 || index >= _backlogEntries.Count || !IsRunning)
                    return;

                DialogueLogEntry entry = _backlogEntries[index];

                // 지금 장면: 확정 전이라 되감기로 간다.
                if (Features.CanJumpTo(entry))
                {
                    if (!Features.RequestBacklogJump(entry))
                        return;

                    CloseAllPanels();
                    await _launcher.RequestReplayAsync();
                    return;
                }

                // 과거 장면: 새 회차가 생긴다 — 확인한 뒤에만.
                if (!SafeCanFork(entry))
                    return;

                Confirm(ConfirmPanel.Question.Fork, () => ForkFromBacklog(entry));
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
                _backlogVoiceSequence = Features.ReplayVoice(entry) ? entry.lineSequence : -1;
            }

            _ui.GetUI<BacklogPanel>().ShowVoicePlaying(_backlogVoiceSequence >= 0 ? index : -1);
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
                Notice(NoticePanel.Message.ForkFailed);
            }
        }
    }
}
