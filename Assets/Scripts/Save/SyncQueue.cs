using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ked.Save
{
    // 서버로 보낼 것을 쌓아 두는 큐 (M7-3). 파일 하나 — saves/sync_queue.json.
    //
    // 규칙 셋:
    //   · seq 발급과 적재는 **한 번의 파일 쓰기**다 — 죽어도 어긋난 상태가 안 남는다 (C1).
    //   · 비우는 조건은 "200을 받았다"뿐이다 — accepted*와 보낸 수를 비교하지 않는다.
    //     그 값이 보낸 수보다 작은 것은 서버의 정상 동작(재도달 흡수, D-011)이다.
    //   · 보내는 동안에도 게임은 계속 쌓는다 — 그래서 비울 때 "전부"가 아니라
    //     **보냈던 그 배치만** 지운다(CaptureBatch → Acknowledge).
    public sealed class SyncQueue
    {
        private readonly string _path;
        private SyncQueueFile _file;

        public SyncQueue(string path)
        {
            _path = path;
            _file = ReadOrNew(path);
        }

        private static SyncQueueFile ReadOrNew(string path)
        {
            string json = AtomicFile.ReadAllTextOrNull(path);

            if (json != null)
            {
                try
                {
                    SyncQueueFile file = SaveJson.Deserialize<SyncQueueFile>(json);

                    if (file != null)
                        return file;
                }
                catch (Exception error)
                {
                    // 큐를 잃으면 그 사이의 선택 이력이 서버에 못 가지만, 세이브(스냅샷)는
                    // slot{n}.json에 따로 있어 게임은 이어진다. 여기서 던져 부팅을 막는 것보다
                    // 잃은 것을 로그로 밝히고 계속 가는 쪽이 맞다.
                    Debug.LogWarning($"[동기화] sync_queue.json 을 읽지 못했다 — 새 큐로 시작.\n{error}");
                }
            }

            return new SyncQueueFile();
        }

        public long? PlaythroughId => _file.PlaythroughId;
        public long? BaseRevision => _file.BaseRevision;
        public bool HasPending => _file.PendingChoices.Count > 0 || _file.PendingEvents.Count > 0;

        public void SetPlaythroughId(long playthroughId)
        {
            _file.PlaythroughId = playthroughId;
            Persist();
        }

        public void EnqueueChoice(string episodeId, int optionIndex, string chosenAtUtc)
        {
            _file.PendingChoices.Add(new PendingChoice
            {
                Seq = _file.NextSeq,
                EpisodeId = episodeId,
                OptionIndex = optionIndex,
                ChosenAt = chosenAtUtc,
            });

            // 발급과 적재가 같은 객체 변경이고, 아래 Persist 한 번에 같이 눕는다.
            _file.NextSeq++;

            Persist();
        }

        public void EnqueueEvent(string episodeId, string occurredAtUtc)
        {
            _file.PendingEvents.Add(new PendingEvent
            {
                EpisodeId = episodeId,
                OccurredAt = occurredAtUtc,
            });

            Persist();
        }

        // 지금 쌓인 것의 사본. 전송이 나가 있는 동안 원본에 새 항목이 붙어도
        // 이 사본은 변하지 않는다 — 성공 시 지울 범위가 이 사본이다.
        public SyncBatch CaptureBatch()
        {
            return new SyncBatch(
                new List<PendingChoice>(_file.PendingChoices),
                new List<PendingEvent>(_file.PendingEvents));
        }

        // 200을 받았다 — 보냈던 배치를 지우고 서버가 알려 준 revision을 적는다.
        // 배치에 없던(전송 중에 쌓인) 항목은 남는다 — 다음 동기화가 가져간다.
        public void Acknowledge(SyncBatch batch, long newRevision)
        {
            foreach (PendingChoice sent in batch.Choices)
                _file.PendingChoices.RemoveAll(p => p.Seq == sent.Seq);

            foreach (PendingEvent sent in batch.Events)
            {
                // 이벤트에는 seq가 없다 — (에피소드, 시각)이 사실상의 신원이다.
                PendingEvent match = _file.PendingEvents.Find(p =>
                    p.EpisodeId == sent.EpisodeId && p.OccurredAt == sent.OccurredAt);

                if (match != null)
                    _file.PendingEvents.Remove(match);
            }

            _file.BaseRevision = newRevision;

            Persist();
        }

        private void Persist()
        {
            AtomicFile.WriteAllText(_path, SaveJson.SerializePretty(_file));
        }
    }

    // CaptureBatch의 결과 — 전송 한 번에 실을 것.
    public sealed class SyncBatch
    {
        public readonly List<PendingChoice> Choices;
        public readonly List<PendingEvent> Events;

        public SyncBatch(List<PendingChoice> choices, List<PendingEvent> events)
        {
            Choices = choices;
            Events = events;
        }

        public bool IsEmpty => Choices.Count == 0 && Events.Count == 0;
    }
}
