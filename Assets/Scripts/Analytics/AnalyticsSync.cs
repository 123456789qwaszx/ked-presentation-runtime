using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public interface IAnalyticsTransport
{
    Task<AnalyticsApiResult<AnalyticsPlaythroughDto>> CreateOrGetPlaythroughAsync(
        string chapterKey, string clientPlaythroughId);
    Task<AnalyticsApiResult<bool>> ReplaceChoicesAsync(
        long playthroughId, IReadOnlyList<AnalyticsChoiceItemDto> choices);
}

// 메모리에 회차별 최신 확정 경로만 보류한다. 로컬 저장의 성공 여부를 바꾸지 않는다.
// Update에서 Tick을 호출한다. 앱 종료 후 과거 회차 재전송을 보장하는 outbox가 아니다.
public sealed class AnalyticsSync : IDisposable
{
    private sealed class Pending
    {
        public LocalSaveFile Snapshot;
        public long? ServerId;
        public DateTime RetryAt;
        public int Failures;
    }

    private readonly IAnalyticsTransport _api;
    private readonly Action<string> _log;
    private readonly Func<DateTime> _clock;
    private readonly Dictionary<string, Pending> _pending = new(StringComparer.Ordinal);
    private bool _running;
    private bool _disposed;

    public AnalyticsSync(IAnalyticsTransport api, Action<string> log = null, Func<DateTime> clock = null)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _log = log;
        _clock = clock ?? (() => DateTime.UtcNow);
    }

    public void Observe(LocalSaveFile snapshot)
    {
        if (_disposed || snapshot == null) return;
        if (!_pending.TryGetValue(snapshot.PlaythroughId, out Pending pending))
        {
            pending = new Pending();
            _pending.Add(snapshot.PlaythroughId, pending);
        }
        pending.Snapshot = PlaythroughSession.Copy(snapshot);
    }

    public void Tick()
    {
        if (_disposed || _running) return;
        Pending pending = _pending.Values.FirstOrDefault(p => p.RetryAt <= _clock());
        if (pending == null) return;
        _running = true; // async가 즉시 완료되어도 중복 전송되지 않는다.
        _ = SendAsync(pending, pending.Snapshot);
    }

    private async Task SendAsync(Pending pending, LocalSaveFile snapshot)
    {
        try
        {
            if (!pending.ServerId.HasValue)
            {
                var registration = await _api.CreateOrGetPlaythroughAsync(snapshot.ChapterId, snapshot.PlaythroughId);
                if (_disposed) return;
                if (!registration.IsSuccess || registration.Body == null
                    || registration.Body.PlaythroughId <= 0
                    || registration.Body.ClientPlaythroughId != snapshot.PlaythroughId)
                {
                    RetryLater(pending, "회차 등록 실패");
                    return;
                }
                pending.ServerId = registration.Body.PlaythroughId;
            }
            var result = await _api.ReplaceChoicesAsync(pending.ServerId.Value, Flatten(snapshot));
            if (_disposed) return;
            if (!result.IsSuccess)
            {
                // 개발 DB 초기화 등으로 서버 회차가 사라졌으면 다음 시도에서 재등록한다.
                if (result.Status == 404) pending.ServerId = null;
                RetryLater(pending, "선택 전송 실패");
                return;
            }
            pending.Failures = 0;
            pending.RetryAt = DateTime.MinValue;
            // 전송 중 새 snapshot이 왔다면 그것을 남겨 다음 Tick에서 처리한다.
            if (ReferenceEquals(pending.Snapshot, snapshot))
                _pending.Remove(snapshot.PlaythroughId);
        }
        catch (Exception error)
        {
            if (!_disposed) RetryLater(pending, error.Message);
        }
        finally { _running = false; }
    }

    private void RetryLater(Pending pending, string reason)
    {
        pending.Failures = Math.Min(7, pending.Failures + 1);
        pending.RetryAt = _clock().AddSeconds(Math.Min(300, 5 * Math.Pow(2, pending.Failures - 1)));
        try { _log?.Invoke("[통계] 로컬 진행을 유지하고 전송을 보류합니다: " + reason); }
        catch { /* 진단 출력 실패도 게임 진행에 영향을 주지 않는다. */ }
    }

    private static List<AnalyticsChoiceItemDto> Flatten(LocalSaveFile snapshot)
    {
        var choices = new List<AnalyticsChoiceItemDto>();
        if (snapshot.Scenes == null) return choices;
        foreach (SceneRecord scene in snapshot.Scenes)
        {
            if (scene?.Path == null) continue;
            foreach (SavedChoice choice in scene.Path)
            {
                if (choice == null) continue;
                choices.Add(new AnalyticsChoiceItemDto
                {
                    EpisodeKey = choice.FromEpisodeId,
                    OptionIndex = choice.OptionIndex,
                });
            }
        }
        return choices;
    }

    public void Dispose()
    {
        _disposed = true;
        _pending.Clear();
    }
}
