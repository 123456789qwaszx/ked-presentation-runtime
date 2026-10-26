using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ked.Progression;
using static Chapters;

// 호스트 SceneRunner 회귀 하네스.
// 예전 Ked.Progression/Tests/EditMode/SceneRunnerTests(코어 안 SceneRunner 대상)를
// 지금의 호스트 구현(ProgressionHost/SceneRunner) 기준으로 다시 세운 것이다.
internal static class Program
{
    private static int _passed;
    private static readonly List<string> Failed = new();

    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }

    private static void Same<T>(IEnumerable<T> actual, IEnumerable<T> expected, string message)
    {
        T[] a = actual.ToArray();
        T[] e = expected.ToArray();

        if (!a.SequenceEqual(e))
            throw new Exception($"{message}\n  기대: [{string.Join(", ", e)}]\n  실제: [{string.Join(", ", a)}]");
    }

    private static async Task<T> Throws<T>(Func<Task> body) where T : Exception
    {
        try { await body(); }
        catch (T error) { return error; }
        catch (Exception other) { throw new Exception($"{typeof(T).Name}를 기대했으나 {other.GetType().Name}: {other.Message}"); }

        throw new Exception($"{typeof(T).Name}를 기대했으나 예외가 없었다.");
    }

    // 비동기로 이어지는 실행을 기다린다. 끝내 오지 않으면 실패.
    private static async Task Until(Func<bool> condition, string what)
    {
        var clock = Stopwatch.StartNew();

        while (!condition())
        {
            if (clock.ElapsedMilliseconds > 3000)
                throw new Exception("시간 초과: " + what);

            await Task.Delay(2);
        }
    }

    private static async Task Test(string name, Func<Task> body)
    {
        UnityEngine.Debug.Logs.Clear();
        UnityEngine.Debug.Errors.Clear();

        try
        {
            await body();
            _passed++;
            Console.WriteLine("PASS " + name);
        }
        catch (Exception error)
        {
            Failed.Add(name);
            Console.WriteLine("FAIL " + name + "\n  " + error.Message);
        }
    }

    private static SceneRunSession NewSession(ChapterDefinition chapter) =>
        SceneRunSession.StartNew(new SceneProgress(chapter, chapter.CreateEntryState()));

    private static string Choices(SceneCommitResult result) =>
        string.Join(" ", result.Choices.Select(c => $"{c.FromEpisodeId}:{c.OptionIndex}"));

    // Unity처럼 한 스레드에서 돈다. await 뒤 이어지는 실행은 큐에 쌓였다가
    // 지금 돌고 있는 동기 코드가 끝난 뒤에 돈다 — 스레드 풀에서 돌리면
    // Unity에서는 생기지 않는 경합이 생겨 검증이 흐려진다.
    public static int Main()
    {
        int exitCode = 1;
        MainThread.Run(async () => exitCode = await RunAllAsync());
        return exitCode;
    }

    private static async Task<int> RunAllAsync()
    {
        #region 정상 진행

        await Test("장면을 넘는 선택은 진입 장면을 확정한다", async () =>
        {
            var rig = new Rig();

            SceneRunResult result = await rig.Runner.RunAsync(NewSession(TwoScenes()), CancellationToken.None);

            Check(result.Outcome == SceneRunOutcome.SceneEnded, "결과가 SceneEnded가 아니다");
            Check(result.ExitState.CurrentEpisodeId == "ep-2", "다음 장면 시작점이 ep-2가 아니다");
            Same(rig.Log.Snapshot(), new[]
            {
                "stop", "begin", "enter:ep-1",
                "play:N_ep-1", "ask:out",
                "commit:SceneEnded:1",
            }, "장면 하나의 순서가 다르다");
            Check(Choices(rig.Persistence.Commits.Single().Result) == "ep-1:0", "확정된 선택이 다르다");
        });

        await Test("장면 안 자동 간선은 묻지 않고, 기록은 남긴다", async () =>
        {
            var rig = new Rig();
            ChapterDefinition chapter = Build("ep-1",
                Node("ep-1", "scene-1", Auto("ep-1b")),
                Node("ep-1b", "scene-1", Choice("ep-2", "out")),
                Node("ep-2", "scene-2"));

            await rig.Runner.RunAsync(NewSession(chapter), CancellationToken.None);

            Same(rig.Presentation.Played, new[] { "N_ep-1", "N_ep-1b" }, "재생 순서가 다르다");
            Check(rig.Options.Asks.Length == 1 && rig.Options.Asks[0].Labels.Single() == "out", "자동 간선에서 물었다");
            Check(Choices(rig.Persistence.Commits.Single().Result) == "ep-1:0 ep-1b:0", "자동 간선이 기록되지 않았다");
        });

        await Test("다음 간선이 없으면 챕터가 끝나고 그 장면을 확정한다", async () =>
        {
            var rig = new Rig();
            ChapterDefinition chapter = Build("ep-1",
                Node("ep-1", "scene-1", Choice("ep-1b", "go")),
                Node("ep-1b", "scene-1"));

            SceneRunResult result = await rig.Runner.RunAsync(NewSession(chapter), CancellationToken.None);

            Check(result.Outcome == SceneRunOutcome.ChapterEnded, "결과가 ChapterEnded가 아니다");
            Check(result.ExitState.CurrentEpisodeId == "ep-1b", "마지막 위치가 다르다");
            var commit = rig.Persistence.Commits.Single();
            Check(commit.Outcome == SceneRunOutcome.ChapterEnded && Choices(commit.Result) == "ep-1:0", "챕터 끝 확정이 다르다");
        });

        await Test("드라이버: 장면 경계마다 정지 → 시작 → 진입 기록 → 재생 → 확정", async () =>
        {
            var rig = new Rig();
            ChapterDefinition chapter = TwoScenes();

            rig.Driver.Start(chapter, chapter.CreateEntryState());
            await Until(() => !rig.Driver.IsRunning, "챕터 끝");

            Same(rig.Log.Snapshot(), new[]
            {
                "stop", "begin", "enter:ep-1", "play:N_ep-1", "ask:out", "commit:SceneEnded:1",
                "stop", "begin", "enter:ep-2", "play:N_ep-2", "commit:ChapterEnded:0",
            }, "챕터 순서가 다르다");
            Check(!rig.DriverLog.Has("E", "FAULTED"), "드라이버가 실패를 기록했다");
            Check(rig.Driver.CurrentState == null, "끝난 드라이버가 상태를 쥐고 있다");
        });

        await Test("장면 진입 때 백로그 경계와 롤백 좌표가 함께 0에서 시작한다", async () =>
        {
            var rig = new Rig();
            ChapterDefinition chapter = TwoScenes();

            rig.Driver.Start(chapter, chapter.CreateEntryState());
            await Until(() => !rig.Driver.IsRunning, "챕터 끝");

            IReadOnlyList<DialogueLogEntry> backlog = rig.Backlog.Entries;
            Check(backlog.Count == 4, "백로그는 장면을 넘어도 이어져야 한다");
            Check(!rig.Backlog.IsInCurrentScene(backlog[1]), "앞 장면 줄이 현재 장면으로 읽힌다");
            Check(rig.Backlog.GetCurrentSceneHistoryIndex(backlog[2]) == 0, "현재 장면 첫 줄의 백로그 좌표가 0이 아니다");

            Check(rig.Rollback.Points.Count == 2, "롤백 기록이 장면 진입에서 비워지지 않았다");
            Check(rig.Rollback.Points[0].nodeName == "N_ep-2" && rig.Rollback.Points[0].historyIndex == 0,
                "현재 장면 첫 줄의 롤백 좌표가 0이 아니다");
            Check(rig.Backlog.GetCurrentSceneHistoryIndex(backlog[3]) == rig.Rollback.Points[1].historyIndex,
                "백로그 좌표와 롤백 좌표가 어긋났다");
        });

        #endregion

        #region 저장 경로 복원

        await Test("복원 경로는 첫 장면만 소비하고, 기록이 끝나면 다시 묻는다", async () =>
        {
            var rig = new Rig();
            ChapterDefinition chapter = WithinSceneChoice();
            rig.Replay.SeekUntilNode = "N_ep-1b"; // 저장한 줄이 ep-1b 안에 있다

            rig.Driver.Start(chapter, chapter.CreateEntryState(), new[] { new ScenePathStep("ep-1", 1) });
            await Until(() => !rig.Driver.IsRunning, "챕터 끝");

            Check(rig.Replay.BeginLoadReplayCount == 1, "로드 시크가 한 번 열려야 한다");
            Check(rig.Options.Asks.Length == 1 && rig.Options.Asks[0].Labels.Single() == "out",
                "기록된 선택(ep-1)을 다시 물었거나, 기록 뒤 선택(ep-1b)을 묻지 않았다");
            Check(Choices(rig.Persistence.Commits[0].Result) == "ep-1:1 ep-1b:0", "첫 장면 확정에 복원 선택이 빠졌다");
            Check(rig.Persistence.Commits.Count == 2 && rig.Persistence.Commits[1].Result.Choices.Count == 0, "둘째 장면이 다르다");
            Check(UnityEngine.Debug.Errors.Count == 0, "오류가 기록됐다: " + string.Join(" / ", UnityEngine.Debug.Errors));
        });

        await Test("그래프와 맞지 않는 경로는 실행을 시작하지 않는다", async () =>
        {
            var rig = new Rig();
            ChapterDefinition chapter = TwoScenes();

            rig.Driver.Start(chapter, chapter.CreateEntryState(), new[] { new ScenePathStep("ep-1", 5) });
            await Until(() => !rig.Driver.IsRunning, "실행 종료");

            Check(rig.DriverLog.Has("E", "FAULTED"), "실패가 기록되지 않았다");
            Check(rig.Presentation.BeginCount == 0 && rig.Persistence.Entered.Count == 0, "부분 복원한 채로 장면을 열었다");
            Check(rig.Replay.BeginLoadReplayCount == 0, "로드 시크가 열렸다");
        });

        await Test("기록이 바닥난 시크는 오류를 남기고 일반 진행으로 돌아간다", async () =>
        {
            var rig = new Rig();
            ChapterDefinition chapter = TwoScenes();
            rig.Replay.SeekUntilNode = null; // 저장한 줄에 끝내 닿지 못한다

            rig.Driver.Start(chapter, chapter.CreateEntryState(), Array.Empty<ScenePathStep>());
            await Until(() => !rig.Driver.IsRunning, "챕터 끝");

            Check(rig.Replay.BeginLoadReplayCount == 1, "빈 경로도 복원 시작이다");
            Check(rig.Replay.ClearSeekCount == 1 && !rig.Replay.IsSeekingActive, "시크가 꺼지지 않았다");
            Check(UnityEngine.Debug.Errors.Any(e => e.Contains("Seeking")), "오류가 남지 않았다");
            Check(rig.Options.Asks.Length == 1 && rig.Persistence.Commits.Count == 2, "일반 진행으로 이어지지 않았다");
        });

        await Test("시크가 기록보다 먼저 끝나면 남은 기록은 버리고 묻는다", async () =>
        {
            var rig = new Rig();
            ChapterDefinition chapter = WithinSceneChoice();
            rig.Replay.SeekUntilNode = "N_ep-1"; // 저장한 줄이 기록된 선택보다 앞에 있다(대본이 바뀐 경우)

            rig.Driver.Start(chapter, chapter.CreateEntryState(), new[] { new ScenePathStep("ep-1", 1) });
            await Until(() => !rig.Driver.IsRunning, "챕터 끝");

            Check(rig.Options.Asks.Length == 2, "시크가 끝난 뒤에도 기록된 선택을 몰래 소비했다");
            Same(rig.Options.Asks[0].Labels, new[] { "a", "b" }, "ep-1 선택을 묻지 않았다");
            Check(Choices(rig.Persistence.Commits[0].Result) == "ep-1:0 ep-1b:0", "버린 기록이 확정에 남았다");
        });

        #endregion

        #region 리플레이와 롤백

        await Test("재생 중 리플레이: 같은 장면 root부터, 확정·재진입 없이, 요청은 한 번만", async () =>
        {
            var rig = new Rig();
            rig.Presentation.BlockPlays = true;
            SceneRunSession session = NewSession(TwoScenes());
            using var cancellation = new CancellationTokenSource();

            Task<SceneRunResult> run = rig.Runner.RunAsync(session, cancellation.Token);
            await Until(() => rig.Presentation.Played.Length == 1, "첫 재생");

            rig.Presentation.HoldStop = true;
            Task first = rig.Runner.RequestReplayAsync(session);
            Task second = rig.Runner.RequestReplayAsync(session);

            Check(second.IsCompleted, "겹친 요청이 무시되지 않았다");
            await Task.Delay(20);
            Check(!first.IsCompleted && rig.Presentation.PrepareReplayCount == 0,
                "재생 정지가 끝나기 전에 리플레이를 시작했다");

            rig.Presentation.ReleaseStop();
            await first;
            await Until(() => rig.Presentation.Played.Length == 2, "리플레이 재생");

            Check(rig.Presentation.PrepareReplayCount == 1, "리플레이 준비가 한 번이 아니다");
            Check(!session.ReplayPending, "리플레이 요청이 정리되지 않았다");
            Check(session.Progress.CurrentEpisodeId == session.Progress.RootEpisodeId, "root로 돌아가지 않았다");
            Check(rig.Presentation.BeginCount == 1 && rig.Persistence.Entered.Count == 1, "장면을 다시 열었다");
            Check(rig.Persistence.Commits.Count == 0, "리플레이가 장면을 확정했다");

            cancellation.Cancel();
            await rig.Runner.StopAsync();
            await Throws<OperationCanceledException>(() => run);
        });

        await Test("선택 대기 중 리플레이: 취소된 질문은 기록되지 않는다", async () =>
        {
            var rig = new Rig();
            rig.Options.AutoAnswer = null;
            SceneRunSession session = NewSession(TwoScenes());

            Task<SceneRunResult> run = rig.Runner.RunAsync(session, CancellationToken.None);
            await Until(() => rig.Options.Asks.Length == 1, "첫 질문");

            await rig.Runner.RequestReplayAsync(session);
            await Until(() => rig.Options.Asks.Length == 2, "리플레이 뒤 질문");

            Check(rig.Options.CancelCount >= 1, "선택 UI가 취소되지 않았다");
            Same(rig.Presentation.Played, new[] { "N_ep-1", "N_ep-1" }, "root부터 다시 재생하지 않았다");
            Check(rig.Persistence.Commits.Count == 0, "리플레이가 장면을 확정했다");

            rig.Options.Answer(0);
            SceneRunResult result = await run;

            Check(result.Outcome == SceneRunOutcome.SceneEnded, "리플레이 뒤 진행이 이어지지 않았다");
            Check(Choices(rig.Persistence.Commits.Single().Result) == "ep-1:0", "취소된 질문이 선택으로 남았다");
        });

        await Test("답과 리플레이 요청이 겹치면 리플레이가 이긴다", async () =>
        {
            var rig = new Rig();
            rig.Options.AutoAnswer = null;
            SceneRunSession session = NewSession(TwoScenes());

            Task<SceneRunResult> run = rig.Runner.RunAsync(session, CancellationToken.None);
            await Until(() => rig.Options.Asks.Length == 1, "첫 질문");

            rig.Options.Answer(0);                              // 답은 도착했지만 아직 소비되지 않았다
            await rig.Runner.RequestReplayAsync(session);       // 같은 틈에 롤백이 들어온다
            await Until(() => rig.Options.Asks.Length == 2, "리플레이 뒤 질문");

            Check(rig.Persistence.Commits.Count == 0, "리플레이 요청 뒤에 도착한 답으로 장면을 넘겼다");
            Same(rig.Presentation.Played, new[] { "N_ep-1", "N_ep-1" }, "root부터 다시 재생하지 않았다");

            rig.Options.Answer(0);
            await run;
        });

        await Test("롤백 — 표적 뒤에 본 이벤트 에피소드는 시청 기록에서도 지워진다", async () =>
        {
            var rig = new Rig();
            rig.Options.AutoAnswer = null;

            EpisodeNodeDto withEvent = Node("ep-1b", "scene-1", Choice("ep-2", "out"));
            withEvent.EventKey = "saw_secret";

            ChapterDefinition chapter = Build("ep-1",
                Node("ep-1", "scene-1", Choice("ep-1b", "a"), Choice("ep-1c", "b")),
                withEvent,
                Node("ep-1c", "scene-1", Choice("ep-2", "out")),
                Node("ep-2", "scene-2"));

            rig.Driver.Start(chapter, chapter.CreateEntryState());
            await Until(() => rig.Options.Asks.Length == 1, "ep-1 질문");
            rig.Options.Answer(0);                                   // a → 이벤트가 있는 ep-1b를 본다
            await Until(() => rig.Options.Asks.Length == 2, "ep-1b 질문");

            rig.MarkRollback(historyIndex: 0, untilNode: "N_ep-1"); // ep-1b를 보기 전으로
            await rig.Driver.RequestReplayAsync();
            await Until(() => rig.Options.Asks.Length == 3, "롤백 뒤 질문");

            rig.Options.Answer(1);                                   // 이번에는 b → ep-1c
            await Until(() => rig.Options.Asks.Length == 4, "ep-1c 질문");
            rig.Options.Answer(0);
            await Until(() => !rig.Driver.IsRunning, "챕터 끝");

            SceneCommitResult first = rig.Persistence.Commits[0].Result;
            Check(Choices(first) == "ep-1:1 ep-1c:0", "확정된 선택이 다르다");
            Check(first.WatchedEpisodeIds.Count == 0, "되감은 길의 이벤트가 확정됐다: " + string.Join(",", first.WatchedEpisodeIds));
        });

        await Test("롤백 — 표적 앞의 선택은 지워지고 다시 묻는다", async () =>
        {
            var rig = new Rig();
            rig.Options.AutoAnswer = null;
            ChapterDefinition chapter = WithinSceneChoice();

            rig.Driver.Start(chapter, chapter.CreateEntryState());
            await Until(() => rig.Options.Asks.Length == 1, "ep-1 질문");
            rig.Options.Answer(1);                                   // b — 앵커는 ep-1의 마지막 줄(1)
            await Until(() => rig.Options.Asks.Length == 2, "ep-1b 질문");

            rig.MarkRollback(historyIndex: 0, untilNode: "N_ep-1"); // ep-1의 첫 줄 = 선택보다 앞
            await rig.Driver.RequestReplayAsync();
            await Until(() => rig.Options.Asks.Length == 3, "롤백 뒤 질문");

            Same(rig.Options.Asks[2].Labels, new[] { "a", "b" }, "ep-1 선택을 다시 묻지 않았다");
            Check(rig.Driver.PendingPath.Count == 0, "표적 뒤 선택이 남았다");
            Check(rig.Persistence.Commits.Count == 0, "롤백이 장면을 확정했다");

            rig.Options.Answer(0);
            await Until(() => rig.Options.Asks.Length == 4, "ep-1b 질문");
            rig.Options.Answer(0);
            await Until(() => !rig.Driver.IsRunning, "챕터 끝");

            Check(Choices(rig.Persistence.Commits[0].Result) == "ep-1:0 ep-1b:0", "다시 고른 선택으로 확정되지 않았다");
        });

        await Test("롤백 — 표적 뒤에 남는 선택은 기록으로 다시 지나간다", async () =>
        {
            var rig = new Rig();
            rig.Options.AutoAnswer = null;
            ChapterDefinition chapter = WithinSceneChoice();

            rig.Driver.Start(chapter, chapter.CreateEntryState());
            await Until(() => rig.Options.Asks.Length == 1, "ep-1 질문");
            rig.Options.Answer(1);                                    // b — 앵커 1
            await Until(() => rig.Options.Asks.Length == 2, "ep-1b 질문");

            rig.MarkRollback(historyIndex: 2, untilNode: "N_ep-1b"); // ep-1b의 첫 줄 = 선택보다 뒤
            await rig.Driver.RequestReplayAsync();
            await Until(() => rig.Options.Asks.Length == 3, "롤백 뒤 질문");

            Same(rig.Options.Asks[2].Labels, new[] { "out" }, "기록된 ep-1 선택을 다시 물었다");
            Same(rig.Driver.PendingPath.Select(c => $"{c.FromEpisodeId}:{c.OptionIndex}"), new[] { "ep-1:1" },
                "기록된 선택이 그대로 소비되지 않았다");
            Same(rig.Presentation.Played, new[] { "N_ep-1", "N_ep-1b", "N_ep-1", "N_ep-1b" }, "root부터 다시 지나가지 않았다");

            rig.Options.Answer(0);
            await Until(() => !rig.Driver.IsRunning, "챕터 끝");

            Check(Choices(rig.Persistence.Commits[0].Result) == "ep-1:1 ep-1b:0", "확정된 선택이 다르다");
        });

        #endregion

        #region 정지

        await Test("재생 중 정지는 확정하지 않는다", async () =>
        {
            var rig = new Rig();
            rig.Presentation.BlockPlays = true;
            ChapterDefinition chapter = TwoScenes();

            rig.Driver.Start(chapter, chapter.CreateEntryState());
            await Until(() => rig.Presentation.Played.Length == 1, "첫 재생");

            await rig.Driver.StopAsync();

            Check(!rig.Driver.IsRunning, "정지 뒤에도 실행 중이다");
            Check(rig.Persistence.Commits.Count == 0, "정지가 장면을 확정했다");
            Check(rig.DriverLog.Has("I", "CANCELLED") && !rig.DriverLog.Has("E", "FAULTED"), "취소로 끝나지 않았다");
        });

        await Test("선택 대기 중 정지는 확정하지 않는다", async () =>
        {
            var rig = new Rig();
            rig.Options.AutoAnswer = null;
            ChapterDefinition chapter = TwoScenes();

            rig.Driver.Start(chapter, chapter.CreateEntryState());
            await Until(() => rig.Options.Asks.Length == 1, "첫 질문");

            await rig.Driver.StopAsync();

            Check(!rig.Driver.IsRunning, "정지 뒤에도 실행 중이다");
            Check(rig.Persistence.Commits.Count == 0, "정지가 장면을 확정했다");
            Check(rig.DriverLog.Has("I", "CANCELLED") && !rig.DriverLog.Has("E", "FAULTED"), "취소로 끝나지 않았다");
        });

        #endregion

        #region 노드 재생 호스트 — 같은 재생 단위 수명(PlaybackUnit)을 쓰는 두 번째 호스트

        await Test("노드 재생: 열기 순서, 끝까지 간 노드는 마지막 화면을 남긴다", async () =>
        {
            var rig = new Rig();
            rig.Backlog.Record(new YarnLineMeta("old", "old#0", "", "이전 단위의 줄"));
            var host = new NodePlaybackHost(rig.Unit);

            await host.PlayAsync("N_solo");

            Same(rig.Log.Snapshot(), new[] { "stop", "begin", "play:N_solo" }, "노드 하나의 순서가 다르다");
            Check(!host.IsRunning, "끝난 뒤에도 실행 중이다");
            Check(!rig.Backlog.IsInCurrentScene(rig.Backlog.Entries[0]), "단위를 열 때 백로그 경계가 서지 않았다");
            Check(rig.Rollback.Points.Count == 2 && rig.Rollback.Points[0].nodeName == "N_solo", "롤백 기록이 이 단위의 것이 아니다");
        });

        await Test("노드 재생: 리플레이는 단위를 다시 열지 않고 루트부터 다시 돈다, 롤백 표적은 소비된다", async () =>
        {
            var rig = new Rig();
            rig.Presentation.BlockPlays = true;
            var host = new NodePlaybackHost(rig.Unit);

            Task run = host.PlayAsync("N_solo");
            await Until(() => rig.Presentation.Played.Length == 1, "첫 재생");

            rig.MarkRollback(historyIndex: 0, untilNode: "N_solo");
            Task first = host.RequestReplayAsync();
            Task second = host.RequestReplayAsync();
            await Task.WhenAll(first, second);
            await Until(() => rig.Presentation.Played.Length == 2, "리플레이 재생");

            Check(rig.Presentation.BeginCount == 1, "리플레이가 단위를 다시 열었다");
            Check(rig.Presentation.PrepareReplayCount == 1, "리플레이 준비가 한 번이 아니다");
            Check(rig.Presentation.StopCount == 2, $"정지 횟수가 다르다(열기 + 리플레이): {rig.Presentation.StopCount}");
            Check(!rig.Rollback.TakeRollbackTarget(out _), "롤백 표적이 소비되지 않았다");

            await host.ExitAsync();
            await run;

            Check(!host.IsRunning, "끝내기 뒤에도 실행 중이다");
            Check(rig.Presentation.StopCount == 3, "끝내기가 멈추지 않았다");
        });

        await Test("노드 재생: 리플레이 요청과 끝내기가 겹쳐도 한 구간에 한 번만 멈춘다", async () =>
        {
            var rig = new Rig();
            rig.Presentation.BlockPlays = true;
            var host = new NodePlaybackHost(rig.Unit);

            Task run = host.PlayAsync("N_solo");
            await Until(() => rig.Presentation.Played.Length == 1, "첫 재생");

            rig.Presentation.HoldStop = true;
            Task replay = host.RequestReplayAsync();
            Task exit = host.ExitAsync();
            rig.Presentation.ReleaseStop();
            await Task.WhenAll(replay, exit, run);

            Check(rig.Presentation.StopCount == 2, $"스코프가 두 번 끝났다: 정지 {rig.Presentation.StopCount}회");
            Check(rig.Presentation.PrepareReplayCount == 0, "끝내기 뒤에 리플레이를 시작했다");
            Check(!host.IsRunning, "끝난 뒤에도 실행 중이다");
        });

        await Test("노드 재생: 여는 중 끝내기가 오면 열지 않는다", async () =>
        {
            var rig = new Rig();
            rig.Presentation.HoldStop = true;
            var host = new NodePlaybackHost(rig.Unit);

            Task run = host.PlayAsync("N_solo");
            Task exit = host.ExitAsync();
            rig.Presentation.ReleaseStop();
            await Task.WhenAll(exit, run);

            Check(rig.Presentation.BeginCount == 0 && rig.Presentation.Played.Length == 0, "끝내기 뒤에 단위를 열었다");
            Check(rig.Presentation.StopCount == 1, $"정지 횟수가 다르다: {rig.Presentation.StopCount}");
            Check(!host.IsRunning, "끝난 뒤에도 실행 중이다");
        });

        #endregion

        #region 실패

        await Test("확정 저장이 실패하면 다음 장면으로 가지 않는다", async () =>
        {
            var rig = new Rig();
            rig.Persistence.FailCommit = true;
            ChapterDefinition chapter = TwoScenes();

            rig.Driver.Start(chapter, chapter.CreateEntryState());
            await Until(() => !rig.Driver.IsRunning, "실행 종료");

            Check(rig.DriverLog.Has("E", "FAULTED"), "실패가 기록되지 않았다");
            Check(rig.Presentation.BeginCount == 1, "실패 뒤 다음 장면을 열었다");

            var direct = new Rig();
            direct.Persistence.FailCommit = true;
            await Throws<IOException>(() => direct.Runner.RunAsync(NewSession(chapter), CancellationToken.None));
        });

        await Test("진입 기록이 실패하면 재생하지 않는다", async () =>
        {
            var rig = new Rig();
            rig.Persistence.FailEnter = true;

            await Throws<IOException>(() => rig.Runner.RunAsync(NewSession(TwoScenes()), CancellationToken.None));

            Check(rig.Presentation.Played.Length == 0, "진입 실패 뒤 재생했다");
            Check(rig.Persistence.Commits.Count == 0, "진입 실패 뒤 확정했다");
        });

        await Test("잠긴 선택지와 범위 밖 번호는 거부된다", async () =>
        {
            var stats = new[]
            {
                new StatDto { Key = "courage", DisplayName = "용기", Type = "Number", Initial = 0, Minimum = 0, Maximum = 5 },
            };

            EpisodeOptionDto deep = Choice("ep-2", "deep");
            deep.Conditions.Add(new ConditionDto { Kind = "Stat", Key = "courage", Op = "GreaterOrEqual", IntValue = 2 });
            deep.LockedReasonText = "용기 부족";

            EpisodeOptionDto side = Choice("ep-3", "side");
            side.VisibleConditions.Add(new ConditionDto { Kind = "Stat", Key = "courage", Op = "GreaterOrEqual", IntValue = 1 });

            ChapterDefinition chapter = Build("ep-1", stats,
                Node("ep-1", "scene-1", deep, side, Choice("ep-3", "back")),
                Node("ep-2", "scene-2"),
                Node("ep-3", "scene-3"));

            var locked = new Rig();
            locked.Options.AutoAnswer = 0;
            await Throws<InvalidOperationException>(() => locked.Runner.RunAsync(NewSession(chapter), CancellationToken.None));

            FakeOptions.Ask ask = locked.Options.Asks.Single();
            Same(ask.Labels, new[] { "deep", "back" }, "보이는 선택지가 다르다");
            Same(ask.Selectable, new[] { false, true }, "잠김 판정이 다르다");
            Check(ask.Hidden == 1, "숨겨진 선택지 수가 다르다");
            Check(locked.Persistence.Commits.Count == 0, "거부된 선택으로 확정했다");

            var outOfRange = new Rig();
            outOfRange.Options.AutoAnswer = 5;
            await Throws<ArgumentOutOfRangeException>(() => outOfRange.Runner.RunAsync(NewSession(chapter), CancellationToken.None));
        });

        #endregion

        Console.WriteLine();
        Console.WriteLine($"{_passed} passed, {Failed.Count} failed.");

        return Failed.Count == 0 ? 0 : 1;
    }
}

// 한 스레드짜리 SynchronizationContext. Unity 메인 스레드의 대역이다.
internal sealed class MainThread : SynchronizationContext
{
    private readonly System.Collections.Concurrent.BlockingCollection<(SendOrPostCallback Callback, object State)> _queue = new();

    public override void Post(SendOrPostCallback d, object state) => _queue.Add((d, state));

    public override void Send(SendOrPostCallback d, object state) =>
        throw new NotSupportedException("동기 Send는 쓰지 않는다.");

    public static void Run(Func<Task> body)
    {
        SynchronizationContext previous = Current;
        var context = new MainThread();
        SetSynchronizationContext(context);

        try
        {
            Task task = body();
            task.ContinueWith(_ => context._queue.CompleteAdding(), TaskScheduler.Default);

            foreach ((SendOrPostCallback callback, object state) in context._queue.GetConsumingEnumerable())
                callback(state);

            task.GetAwaiter().GetResult();
        }
        finally
        {
            SetSynchronizationContext(previous);
        }
    }
}
