using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Ked.Progression;

// 여러 대역이 한 줄에 사건을 남긴다. 순서 검증용.
internal sealed class EventLog
{
    private readonly List<string> _items = new();

    public void Add(string item)
    {
        lock (_items) _items.Add(item);
    }

    public string[] Snapshot()
    {
        lock (_items) return _items.ToArray();
    }
}

// 노드 하나를 재생하면 줄 LinesPerNode개를 지나간다고 본다.
// 진짜 호스트처럼 줄마다 백로그와 롤백 기록에 적고(VNYarnLineBoundary),
// 정지하면 롤백 기록을 비운다(ScenePresentation.StopAsync).
internal sealed class FakePresentation : IPlaybackStage
{
    private readonly EventLog _log;
    private readonly RollbackHistory _rollback;
    private readonly BacklogRecorder _backlog;
    private readonly ProgressionReplayState _replay;

    private readonly List<string> _played = new();
    private TaskCompletionSource<bool> _playing;

    public bool BlockPlays;
    public int LinesPerNode = 2;

    public int BeginCount;
    public int PrepareReplayCount;
    public int StopCount;

    public FakePresentation(EventLog log, RollbackHistory rollback, BacklogRecorder backlog, ProgressionReplayState replay)
    {
        _log = log;
        _rollback = rollback;
        _backlog = backlog;
        _replay = replay;
    }

    public string[] Played
    {
        get { lock (_played) return _played.ToArray(); }
    }

    public void BeginScene()
    {
        BeginCount++;
        _log.Add("begin");
    }

    public Task PlayEpisodeAsync(string nodeName)
    {
        lock (_played) _played.Add(nodeName);
        _log.Add("play:" + nodeName);

        for (int i = 0; i < LinesPerNode; i++)
        {
            var meta = new YarnLineMeta(nodeName, $"{nodeName}#{i}", "", $"{nodeName} line {i}");
            _backlog.Record(meta);
            _rollback.AddRollbackPoint(meta);
        }

        _replay.ReachNode(nodeName);

        if (!BlockPlays)
            return Task.CompletedTask;

        // 진짜 재생처럼 정지가 끝내 준다. 이어지는 실행은 비동기로 돈다.
        var playing = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _playing = playing;
        return playing.Task;
    }

    public void PrepareReplay()
    {
        PrepareReplayCount++;
        _log.Add("prepare-replay");
    }

    public Task StopAsync()
    {
        StopCount++;
        _log.Add("stop");
        _rollback.ClearRollbackPoints();

        TaskCompletionSource<bool> playing = _playing;
        _playing = null;
        playing?.TrySetResult(true);

        if (!HoldStop)
            return Task.CompletedTask;

        // 정지 정리가 아직 끝나지 않은 상태를 붙잡아 둔다.
        _stopGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        return _stopGate.Task;
    }

    // true면 다음 정지가 ReleaseStop 전까지 끝나지 않는다.
    public bool HoldStop;
    private TaskCompletionSource<bool> _stopGate;

    public void ReleaseStop()
    {
        HoldStop = false;
        _stopGate?.TrySetResult(true);
        _stopGate = null;
    }
}

internal sealed class FakeOptions : IChapterOptionsView
{
    public sealed class Ask
    {
        public string[] Labels;
        public bool[] Selectable;
        public int Hidden;
    }

    private readonly EventLog _log;
    private readonly List<Ask> _asks = new();
    private TaskCompletionSource<int> _pending;

    // 값이 있으면 묻는 즉시 그 번호로 답한다. 없으면 Answer/Cancel을 기다린다.
    public int? AutoAnswer = 0;
    public int CancelCount;

    public FakeOptions(EventLog log) => _log = log;

    public Ask[] Asks
    {
        get { lock (_asks) return _asks.ToArray(); }
    }

    public Task<int> ShowAsync(IReadOnlyList<ResolvedOption> options, int hiddenCount)
    {
        var ask = new Ask
        {
            Labels = options.Select(o => o.Option.ChoiceLabel).ToArray(),
            Selectable = options.Select(o => o.IsSelectable).ToArray(),
            Hidden = hiddenCount,
        };

        lock (_asks) _asks.Add(ask);
        _log.Add("ask:" + string.Join("|", ask.Labels));

        if (AutoAnswer.HasValue)
            return Task.FromResult(AutoAnswer.Value);

        var pending = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending = pending;
        return pending.Task;
    }

    public void Answer(int index)
    {
        TaskCompletionSource<int> pending = _pending ?? throw new InvalidOperationException("묻고 있는 선택지가 없다.");
        _pending = null;
        pending.SetResult(index);
    }

    public void Cancel()
    {
        CancelCount++;

        TaskCompletionSource<int> pending = _pending;
        _pending = null;
        pending?.TrySetCanceled();
    }
}

internal sealed class FakePersistence : IScenePersistence
{
    private readonly EventLog _log;

    public readonly List<(SceneCommitResult Result, SceneRunOutcome Outcome)> Commits = new();
    public readonly List<string> Entered = new();

    public bool FailEnter;
    public bool FailCommit;

    public FakePersistence(EventLog log) => _log = log;

    public void EnterScene(string chapterId, ChapterState entryState)
    {
        _log.Add("enter:" + entryState.CurrentEpisodeId);

        if (FailEnter)
            throw new IOException("진입 기록 실패");

        Entered.Add(entryState.CurrentEpisodeId);
    }

    public void CommitScene(string chapterId, SceneCommitResult result, SceneRunOutcome outcome)
    {
        _log.Add($"commit:{outcome}:{result.Choices.Count}");

        if (FailCommit)
            throw new IOException("디스크가 찼다");

        Commits.Add((result, outcome));
    }
}

internal sealed class FakeLog : IProgressionLog
{
    private readonly List<string> _lines = new();

    public void Info(string message) { lock (_lines) _lines.Add("I " + message); }
    public void Warning(string message) { lock (_lines) _lines.Add("W " + message); }
    public void Error(string message) { lock (_lines) _lines.Add("E " + message); }

    public bool Has(string prefix, string text)
    {
        lock (_lines) return _lines.Any(l => l.StartsWith(prefix + " ", StringComparison.Ordinal) && l.Contains(text));
    }
}

// SceneRunner 하나와 그 협력자 전부. 드라이버도 같은 runner로 세운다.
internal sealed class Rig
{
    public readonly EventLog Log = new();
    public readonly RollbackHistory Rollback = new();
    public readonly BacklogRecorder Backlog = new();
    public readonly ProgressionReplayState Replay = new();
    public readonly FakeLog DriverLog = new();

    public readonly FakePresentation Presentation;
    public readonly FakeOptions Options;
    public readonly FakePersistence Persistence;

    public readonly PlaybackUnit Unit;
    public readonly SceneRunner Runner;
    public readonly ProgressionDriver Driver;

    public Rig()
    {
        Presentation = new FakePresentation(Log, Rollback, Backlog, Replay);
        Options = new FakeOptions(Log);
        Persistence = new FakePersistence(Log);

        Unit = new PlaybackUnit(Presentation, Backlog, Rollback);
        Runner = new SceneRunner(Unit, Options, Replay, Persistence);
        Driver = new ProgressionDriver(Runner, DriverLog);
    }

    // 진짜 VNFeatureController.RequestRollbackTo가 하는 일 중 장면 루프에 닿는 부분.
    public void MarkRollback(int historyIndex, string untilNode)
    {
        if (!Rollback.TryGetRollbackPoint(historyIndex, out RollbackPoint target))
            throw new InvalidOperationException($"롤백 포인트 {historyIndex}가 없다.");

        Rollback.MarkRollbackTarget(target);
        Rollback.ClearRollbackPoints();
        Replay.BeginRollbackSeek(untilNode);
    }
}

// 챕터는 진짜 로더로 만든다 — 호스트가 실제로 지나는 길이고, 불변식 검사도 함께 받는다.
internal static class Chapters
{
    public static string NodeOf(string episodeId) => "N_" + episodeId;

    public static EpisodeNodeDto Node(string episodeId, string sceneId, params EpisodeOptionDto[] next) => new()
    {
        EpisodeId = episodeId,
        Title = episodeId,
        DialogueEntryId = NodeOf(episodeId),
        SceneId = sceneId,
        EventKey = "",
        NextOptions = next.ToList(),
    };

    public static EpisodeOptionDto Choice(string target, string label) => new()
    {
        TargetEpisodeId = target,
        ChoiceLabel = label,
        StatChanges = new List<StatChangeDto>(),
        VisibleConditions = new List<ConditionDto>(),
        Conditions = new List<ConditionDto>(),
        LockedReasonText = "",
    };

    public static EpisodeOptionDto Auto(string target)
    {
        EpisodeOptionDto option = Choice(target, "");
        option.Auto = true;
        return option;
    }

    public static ChapterDefinition Build(string start, IEnumerable<StatDto> stats, params EpisodeNodeDto[] nodes)
    {
        var dto = new ChapterProgressionDto
        {
            ChapterId = "chapter",
            DisplayName = "Chapter",
            StartEpisodeId = start,
            Stats = stats?.ToList() ?? new List<StatDto>(),
            Nodes = nodes.ToList(),
        };

        ProgressionLoadResult result = ProgressionLoader.Load(dto);

        if (!result.IsValid)
            throw new InvalidOperationException("테스트 챕터가 로드되지 않는다:\n" + string.Join("\n", result.Diagnostics));

        return result.Chapter;
    }

    public static ChapterDefinition Build(string start, params EpisodeNodeDto[] nodes) => Build(start, null, nodes);

    // scene-1: ep-1 ─[out]→ scene-2: ep-2(끝)
    public static ChapterDefinition TwoScenes() => Build("ep-1",
        Node("ep-1", "scene-1", Choice("ep-2", "out")),
        Node("ep-2", "scene-2"));

    // scene-1: ep-1 ─[a|b]→ ep-1b ─[out]→ scene-2: ep-2(끝)
    public static ChapterDefinition WithinSceneChoice() => Build("ep-1",
        Node("ep-1", "scene-1", Choice("ep-1b", "a"), Choice("ep-1b", "b")),
        Node("ep-1b", "scene-1", Choice("ep-2", "out")),
        Node("ep-2", "scene-2"));
}
