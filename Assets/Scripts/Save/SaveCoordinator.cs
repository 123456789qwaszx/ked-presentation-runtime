using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using UnityEngine;

namespace Ked.Save
{
    // 드라이버의 보고를 받아 적는 자리 (M7-5). 저장 층에서 유일하게 진행 층을 아는 클래스다.
    //
    // 커밋 보고 한 건 = 로컬 저장 + 큐 적재 + 동기화 시동, 이 순서.
    // 로컬(1·2)이 끝난 뒤에야 서버(3)가 나서고, 3은 실패해도 1·2가 이미 진실이다 —
    // "로컬이 진실, 서버는 사본"을 코드 순서로 적은 것이다.
    //
    // 여기의 어떤 예외도 밖(진행 루프)으로 나가지 않는다 — 저장이 망가져도 게임은 돈다.
    public sealed class SaveCoordinator
    {
        private readonly ISaveStore _localStore;
        private readonly SyncQueue _queue;
        private readonly ServerSyncSaveStore _server;
        private readonly int _slotNo;

        // playSeconds = 저장돼 있던 값 + 이번 실행에서 흐른 시간. 초 단위 통계면 충분해서
        // 일시정지·백그라운드를 빼는 정밀한 계측은 하지 않는다(realtimeSinceStartup 기준).
        private int _basePlaySeconds;
        private float _attachedAt;

        // server가 null이면 로컬 저장·큐 적재만 한다 — 서버 주소를 안 준 조립(에디터에서
        // 서버 없이 도는 경우)의 모양이고, 큐는 쌓이므로 주소를 주면 그때 다 나간다.
        public SaveCoordinator(
            ISaveStore localStore,
            SyncQueue queue,
            ServerSyncSaveStore server, 
            int slotNo)
        {
            _localStore = localStore;
            _queue = queue;
            _server = server;
            _slotNo = slotNo;
        }

        public void Attach(ProgressionDriver driver)
        {
            driver.ChoiceCommitted += OnChoiceCommitted;
            driver.EpisodeWatched += OnEpisodeWatched;

            _attachedAt = Time.realtimeSinceStartup;
        }

        // 런처의 resumeProvider (M7, D-017). 로컬 세이브가 있으면 그 지점 — 없으면 null(새 게임).
        public ProgressionResumePoint GetResumePoint()
        {
            LocalSaveFile save = _localStore.Load(_slotNo);

            if (save == null)
                return null;

            _basePlaySeconds = save.PlaySeconds;

            return new ProgressionResumePoint(save.ChapterId, save.CurrentEpisodeId, save.Stats);
        }

        // 앱 시작 시 한 번 — 지난 실행이 남긴 큐를 민다 (M7 완료 기준: 큐는 살아남는다).
        public Task SyncPendingAsync() =>
            _server == null 
                ? Task.CompletedTask 
                : _server.TrySyncAsync(_slotNo);

        private void OnChoiceCommitted(ChoiceCommitReport report)
        {
            try
            {
                string now = NowUtc();

                // 1. 로컬 스냅샷 — 커밋 직후의 상태 그대로 (C5: 종료 훅에 기대지 않는다).
                _localStore.Save(new LocalSaveFile
                {
                    SlotNo = _slotNo,
                    ChapterId = report.ChapterId,
                    CurrentEpisodeId = report.NewState.CurrentEpisodeId,
                    Stats = CopyStats(report.NewState.Stats),
                    PlaySeconds = CurrentPlaySeconds(),
                    SavedAtUtc = now,
                });

                // 2. 이력 큐 — seq는 큐가 발급한다.
                _queue.EnqueueChoice(report.FromEpisodeId, report.OptionIndex, now);
            }
            catch (Exception error)
            {
                Debug.LogError($"[저장] 커밋 기록 실패 — 진행은 계속한다.\n{error}");
                return;
            }

            // 3. 서버로. await 하지 않는다 — 진행 루프는 저장을 기다리지 않고,
            //    TrySyncAsync가 겹침·예외를 스스로 감당한다.
            if (_server != null)
                _ = _server.TrySyncAsync(_slotNo);
        }

        private void OnEpisodeWatched(EpisodeWatchReport report)
        {
            try
            {
                // 스냅샷은 안 쓴다 — 시청 완료로는 상태가 변하지 않았다. 이력만 적는다.
                // 서버는 episodeId로 EventKey를 스스로 찾고(M3), 재도달은 흡수한다(D-011) —
                // 재개 직후 같은 에피소드를 다시 완주해 중복 보고가 가도 안전한 이유다.
                _queue.EnqueueEvent(report.EpisodeId, NowUtc());
            }
            catch (Exception error)
            {
                Debug.LogError($"[저장] 이벤트 기록 실패 — 진행은 계속한다.\n{error}");
                return;
            }

            if (_server != null)
                _ = _server.TrySyncAsync(_slotNo);
        }

        private int CurrentPlaySeconds() =>
            _basePlaySeconds + (int)(Time.realtimeSinceStartup - _attachedAt);

        private static string NowUtc() =>
            DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);

        // 상태의 딕셔너리를 그대로 들지 않고 복사한다 — 직렬화가 나중(비동기 전송)에
        // 일어나도 "커밋 시점의 값"이 실리게. (ProgressionState는 불변이라 사실 안전하지만,
        // 파일 DTO가 라이브 객체를 참조하는 습관 자체를 만들지 않는다.)
        private static Dictionary<string, int> CopyStats(IReadOnlyDictionary<string, int> stats)
        {
            var copy = new Dictionary<string, int>(stats.Count, StringComparer.Ordinal);

            foreach (KeyValuePair<string, int> pair in stats)
                copy[pair.Key] = pair.Value;

            return copy;
        }
    }
}