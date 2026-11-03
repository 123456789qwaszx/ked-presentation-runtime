using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ked.Samples.UI
{
    // 08 백로그 — 기록은 재생 쪽 백로그를 그대로 읽는다. 줄로 가는 길은 둘이고 서로 다른 호스트 동작이다.
    //   지금 장면의 줄   → 되감기(PlaybackFeatureController.RequestBacklogJump → IPlaybackHost.RequestReplayAsync)
    //   확정된 과거 장면 → 그 지점에서 새 회차로 갈라지기(IKedSaveSlotPort.ForkFromBacklogAsync). 확인을 받는다.
    // 갈 수 없는 줄(지금 줄 · 연출 단독의 과거 장면 등)은 이동 버튼이 없다.
    // 보이스가 있는 줄은 다시 듣기 버튼이 있다 — 장면을 가리지 않고, 패널은 열어 둔다.
    public sealed partial class KedUIFlow
    {
        private readonly List<DialogueLogEntry> _backlogEntries = new();

        private void SubscribeBacklog()
        {
            _backlog.CloseClicked += ClosePanel;
            _backlog.JumpClicked += HandleBacklogJump;
            _backlog.VoiceClicked += HandleBacklogVoice;
        }

        private void OpenBacklog()
        {
            _backlogEntries.Clear();

            IReadOnlyList<DialogueLogEntry> entries = _playback?.Features?.Backlogs;
            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                    _backlogEntries.Add(entries[i]);
            }

            var rows = new List<BacklogRow.Model>(_backlogEntries.Count);
            int last = _backlogEntries.Count - 1;

            for (int i = 0; i < _backlogEntries.Count; i++)
            {
                DialogueLogEntry entry = _backlogEntries[i];

                rows.Add(new BacklogRow.Model
                {
                    Speaker = _speakers?.Find(entry.lineId),
                    Text = entry.rawText,
                    CanJump = CanActOn(entry),
                    HasVoice = IsRunning && _playback.Features.CanReplayVoice(entry),
                    IsCurrent = i == last,
                    ShowDivider = i < last,
                });
            }

            PushPanel<BacklogPanel>(panel => panel.Present(rows));
        }

        private bool CanActOn(DialogueLogEntry entry)
        {
            if (!IsRunning)
                return false;

            if (_playback.Features.CanJumpTo(entry))
                return true;

            return _slotPort != null && SafeCanFork(entry);
        }

        private bool SafeCanFork(DialogueLogEntry entry)
        {
            try
            {
                return _slotPort.CanForkFrom(entry);
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
                if (_playback.Features.CanJumpTo(entry))
                {
                    if (!_playback.Features.RequestBacklogJump(entry))
                        return;

                    CloseAllPanels();
                    await _playback.Host.RequestReplayAsync();
                    return;
                }

                // 과거 장면: 새 회차가 생긴다 — 확인한 뒤에만.
                if (_slotPort == null || !SafeCanFork(entry))
                    return;

                Confirm(new ConfirmPanel.Request
                {
                    Title = Text("confirm.fork.title"),
                    Body = Text("confirm.fork.body"),
                    AcceptLabel = Text("confirm.fork.accept"),
                    CancelLabel = Text("common.cancel"),
                    OnAccept = () => Fork(entry),
                });
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

            _playback.Features.ReplayVoice(_backlogEntries[index]);
        }

        private async void Fork(DialogueLogEntry entry)
        {
            try
            {
                await _slotPort.ForkFromBacklogAsync(entry, () =>
                {
                    StopFastForward();
                    CloseAllPanels();
                });
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                Notice(Text("notice.forkFailed.title"), Text("notice.forkFailed.body"), NoticePanel.Kind.Failure);
            }
        }
    }
}
