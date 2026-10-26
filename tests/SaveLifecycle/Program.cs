using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Ked.Progression;
using Ked.Progression.Save;

// 저장의 호스트(연출) 겹 회귀 하네스. 저장 정책 자체는 원본 저장소의 tests/ProgressionSave가 본다.
internal static class Program
{
    private const string ContentVersion = "test-v1";
    private const string Timestamp = "2026-09-13T00:00:00.0000000Z";
    private static readonly string Root = Path.Combine(Path.GetTempPath(), "save-lifecycle-" + Guid.NewGuid().ToString("N"));
    private static int _passed;
    private static readonly List<string> Failed = new();

    private static string Dir() => Path.Combine(Root, Guid.NewGuid().ToString("N"));
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Throws<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new Exception("Expected " + typeof(T).Name); }
    private static Task Run(Action action) { action(); return Task.CompletedTask; }

    private static async Task Test(string name, Func<Task> body)
    {
        UnityEngine.Debug.Warnings.Clear();
        try { await body(); _passed++; Console.WriteLine("PASS " + name); }
        catch (Exception error) { Failed.Add(name); Console.WriteLine("FAIL " + name + "\n  " + error.Message); }
    }

    private static PlaythroughSave Save(string id, int scene = 1) => new()
    {
        PlaythroughId = id,
        ContentVersion = ContentVersion,
        ChapterId = "chapter",
        CurrentEpisodeId = "scene" + scene,
        Stats = new Dictionary<string, int> { ["score"] = scene },
        SavedAtUtc = Timestamp,
    };

    private static DialogueLogEntry Line(int sequence, string lineId = "line", string node = "node") => new()
    {
        lineId = lineId, nodeName = node, lineSequence = sequence, rawText = lineId + sequence,
    };

    private static LineTarget Target(string lineId = "line", int occurrence = 1) =>
        new() { NodeName = "node", LineId = lineId, Occurrence = occurrence };

    private static ChapterState State(string episode = "scene1") =>
        ChapterState.CreateInitial(Array.Empty<StatDefinition>(), episode);

    private static SaveCoordinator Coordinator(ILocalSaveStore store) => new(store, ContentVersion, () => 0);

    private static ScenarioDefinition Scenario() =>
        new("scenario", "", "chapter", new[]
        {
            new ChapterDefinition("chapter", "", "scene1", null, new[] { new EpisodeNode("scene1", "", "node") }),
        });

    private static SceneCommitResult Completion() => new(State("scene2"), Array.Empty<CommittedChoice>(), Array.Empty<string>());

    private static ProgressionLauncher Launcher(ILocalSaveStore store, ProgressionDriver driver, ProgressionReplayState replay, BacklogRecorder backlog = null) =>
        new(driver, Scenario(), Coordinator(store), backlog ?? new BacklogRecorder(), replay);

    public static async Task<int> Main()
    {
        try
        {
            #region 코덱

            await Test("코덱: 장면·회차·재생 계획이 왕복한다", () => Run(() =>
            {
                var choices = new[] { new VNChoiceRecord(0, 0, 1, "choice-line") };
                List<VNChoiceRecord> scene = PresentationMemory.ReadUnitChoices(PresentationMemory.UnitChoices(choices));
                Check(scene.Single().selectedOptionLineId == "choice-line", "장면 Yarn 선택이 왕복하지 않았다");

                var backlog = new[] { Line(0), Line(1, "other") };
                List<DialogueLogEntry> read = PresentationMemory.ReadBacklog(PresentationMemory.Backlog(backlog));
                Check(read.Count == 2 && read[1].lineId == "other" && read[1].lineSequence == 1, "백로그가 왕복하지 않았다");

                PresentationResumePoint plan = PresentationMemory.ReadResumePoint(PresentationMemory.ResumePoint(Target("L", 2), choices));
                Check(plan.Target.LineId == "L" && plan.Target.Occurrence == 2 && plan.YarnChoices.Single().selectedOptionIndex == 1, "재생 계획이 왕복하지 않았다");

                Check(PresentationMemory.ReadUnitChoices("").Count == 0 && PresentationMemory.ReadBacklog("").Count == 0, "빈 문자열은 빈 기록이어야 한다");
            }));

            await Test("코덱: 깨진 문자열과 잘못된 백로그 순번은 거부된다", () => Run(() =>
            {
                Throws<InvalidDataException>(() => PresentationMemory.ReadResumePoint(""));
                Throws<InvalidDataException>(() => PresentationMemory.ReadResumePoint("{not json"));
                Throws<InvalidDataException>(() => PresentationMemory.ReadResumePoint("{\"target\":{\"nodeName\":\"n\",\"lineId\":\"\",\"occurrence\":1}}"));
                Throws<InvalidDataException>(() => PresentationMemory.ReadResumePoint("{\"target\":{\"nodeName\":\"n\",\"lineId\":\"l\",\"occurrence\":0}}"));
                Throws<InvalidDataException>(() => PresentationMemory.ReadBacklog(PresentationMemory.Backlog(new[] { Line(1), Line(1) })));
                Throws<InvalidDataException>(() => PresentationMemory.ReadBacklog(PresentationMemory.Backlog(new[] { Line(2), Line(1) })));
            }));

            #endregion

            #region 브리지

            await Test("브리지: 장면 경계에서 호스트 상태를 싣는다", () => Run(() =>
            {
                var store = new LocalFileSaveStore(Dir());
                var backlog = new BacklogRecorder();
                var choiceHistory = new ChoiceHistory();
                var save = Coordinator(store);
                var bridge = new ProgressionSaveBridge(save, backlog, choiceHistory);

                backlog.Record(new YarnLineMeta { lineId = "old-line", nodeName = "old-node", rawText = "old" });
                backlog.MarkSceneBoundary();
                choiceHistory.RestoreChoices(new[] { new VNChoiceRecord(0, 0, 2, "picked") });

                save.BeginNewPlaythrough();
                bridge.EnterScene("chapter", State());
                backlog.Record(new YarnLineMeta { lineId = "new-line", nodeName = "node", rawText = "new" });
                bridge.CommitScene("chapter", Completion(), SceneRunOutcome.SceneEnded);

                PlaythroughSave snapshot = store.LoadActive();
                SceneRecord scene = snapshot.Scenes.Single();

                Check(scene.Checkpoint.HostSerialStart == 1, "진입 경계가 백로그 순번으로 실리지 않았다");
                Check(scene.HostSerialEnd == 2, "확정 경계가 백로그 순번으로 실리지 않았다");
                Check(PresentationMemory.ReadUnitChoices(scene.HostState).Single().selectedOptionLineId == "picked", "장면의 Yarn 선택이 실리지 않았다");
                List<DialogueLogEntry> saved = PresentationMemory.ReadBacklog(snapshot.HostState);
                Check(saved.Count == 2 && saved[0].lineId == "old-line" && saved[1].lineId == "new-line", "회차 백로그가 실리지 않았다");
            }));

            #endregion

            #region 런처

            await Test("런처: 재생 계획이 없으면 장면 루트에서 이어한다", () => Run(() =>
            {
                var store = new LocalFileSaveStore(Dir());
                PlaythroughSave file = Save("A");
                file.HostState = PresentationMemory.Backlog(new[] { Line(0, "old") });
                store.Create(file);
                store.SetActive("A");

                var driver = new ProgressionDriver();
                var replay = new ProgressionReplayState();
                var backlog = new BacklogRecorder();
                Launcher(store, driver, replay, backlog).Resume();

                Check(driver.Starts == 1 && driver.LastRestorePath == null, "루트 이어하기를 장면 중간 복원으로 다뤘다");
                Check(replay.PreparedLoads == 1 && replay.StagedPlan == null, "옛 재생 계획이 지워지지 않았다");
                Check(backlog.Entries.Single().lineId == "old" && backlog.NextLineSequence == 1, "백로그가 복원되지 않았다");
            }));

            await Test("런처: 빈 경로라도 재생 계획이 있으면 줄 시크를 연다", () => Run(() =>
            {
                var store = new LocalFileSaveStore(Dir());
                PlaythroughSave file = Save("A");
                file.PendingLoad = new SavedLoadPlan { HostState = PresentationMemory.ResumePoint(Target("line", 2), Array.Empty<VNChoiceRecord>()) };
                store.Create(file);
                store.SetActive("A");

                var driver = new ProgressionDriver();
                var replay = new ProgressionReplayState();
                Launcher(store, driver, replay).Resume();

                Check(driver.LastRestorePath != null && driver.LastRestorePath.Count == 0, "빈 경로가 루트 이어하기로 뭉개졌다");
                Check(replay.StagedPlan?.Target.Occurrence == 2, "줄 좌표가 올라가지 않았다");
            }));

            await Test("런처: 저장 경로는 진행 좌표로 잘려 들어간다", () => Run(() =>
            {
                var store = new LocalFileSaveStore(Dir());
                PlaythroughSave file = Save("A");
                file.PendingLoad = new SavedLoadPlan
                {
                    Path = new List<SavedChoice> { new() { FromEpisodeId = "scene1", OptionIndex = 3 } },
                    HostState = PresentationMemory.ResumePoint(Target(), Array.Empty<VNChoiceRecord>()),
                };
                store.Create(file);
                store.SetActive("A");

                var driver = new ProgressionDriver();
                Launcher(store, driver, new ProgressionReplayState()).Resume();

                Check(driver.LastRestorePath.Single().FromEpisodeId == "scene1" && driver.LastRestorePath.Single().OptionIndex == 3, "경로가 잘못 옮겨졌다");
            }));

            await Test("런처: 깨진 연출 상태는 저장 손상으로 보고 새로 시작한다", () => Run(() =>
            {
                var store = new LocalFileSaveStore(Dir());
                PlaythroughSave file = Save("A");
                file.PendingLoad = new SavedLoadPlan { HostState = "{garbage" };
                store.Create(file);
                store.SetActive("A");

                var driver = new ProgressionDriver();
                var replay = new ProgressionReplayState();
                var launcher = Launcher(store, driver, replay);
                launcher.Resume();

                Check(driver.Starts == 1 && driver.LastRestorePath == null, "새 게임으로 가지 않았다");
                Check(UnityEngine.Debug.Warnings.Any(w => w.Contains("연출 상태")), "경고가 없다");
                Check(replay.StagedPlan == null, "깨진 계획이 올라갔다");
            }));

            await Test("런처: 겹친 전환은 무시되고 이어하기는 로컬에서 시작한다", async () =>
            {
                var driver = new ProgressionDriver { IsRunning = true };
                var stopped = new TaskCompletionSource<bool>(); driver.OnStop = () => stopped.Task;
                var launcher = Launcher(new LocalFileSaveStore(Dir()), driver, new ProgressionReplayState());
                int prepares = 0; Task first = launcher.TransitionAndResumeAsync(() => prepares++);
                await launcher.TransitionAndResumeAsync(() => prepares++);
                stopped.SetResult(true); await first;
                driver.IsRunning = false; launcher.Resume();
                Check(prepares == 1 && driver.Starts == 2, "전환 가드가 깨졌다");
            });

            #endregion

            #region 백로그에서 갈라지기

            await Test("갈라지기 셈법: 줄은 장면 안 등장 순번으로, 물려받는 백로그는 장면 앞까지", () => Run(() =>
            {
                var backlog = new List<DialogueLogEntry> { Line(0, "a"), Line(1, "b"), Line(2, "line"), Line(3, "line"), Line(4, "line", "other") };
                int sceneStart = 2;

                Check(BacklogForkPoints.TryMakeLineTarget(backlog, backlog[3], sceneStart, out LineTarget target), "줄 좌표를 만들지 못했다");
                Check(target.LineId == "line" && target.Occurrence == 2, "같은 줄의 두 번째 등장을 1로 셌다");

                Check(BacklogForkPoints.TryMakeLineTarget(backlog, backlog[2], sceneStart, out target) && target.Occurrence == 1, "첫 등장이 1이 아니다");
                Check(BacklogForkPoints.TryMakeLineTarget(backlog, backlog[4], sceneStart, out target) && target.NodeName == "other" && target.Occurrence == 1, "노드가 다른 줄을 같이 셌다");

                Check(!BacklogForkPoints.TryMakeLineTarget(backlog, Line(9, "missing"), sceneStart, out _), "백로그에 없는 줄에 좌표를 만들었다");
                Check(!BacklogForkPoints.TryMakeLineTarget(backlog, backlog[1], sceneStart, out _), "장면 앞의 줄에 좌표를 만들었다");

                List<DialogueLogEntry> inherited = BacklogForkPoints.Before(backlog, sceneStart);
                Check(inherited.Count == 2 && inherited[1].lineId == "b", "물려받는 백로그가 장면 앞까지가 아니다");
            }));

            await Test("갈라지기: 완료 장면의 줄에서 새 회차를 열면 런처가 그 줄까지 달린다", () => Run(() =>
            {
                var store = new LocalFileSaveStore(Dir());
                var backlog = new BacklogRecorder();
                var save = Coordinator(store);
                var bridge = new ProgressionSaveBridge(save, backlog, new ChoiceHistory());

                // 장면 1: 줄 0,1 — 장면 2: 줄 2
                save.BeginNewPlaythrough();
                bridge.EnterScene("chapter", State());
                backlog.Record(new YarnLineMeta { lineId = "line", nodeName = "node", rawText = "a" });
                backlog.Record(new YarnLineMeta { lineId = "line", nodeName = "node", rawText = "b" });
                bridge.CommitScene("chapter", Completion(), SceneRunOutcome.SceneEnded);
                backlog.MarkSceneBoundary();
                bridge.EnterScene("chapter", State("scene2"));
                backlog.Record(new YarnLineMeta { lineId = "line", nodeName = "node", rawText = "c" });

                PlaythroughSaveSnapshot playthrough = save.Capture();
                DialogueLogEntry second = backlog.Entries[1];
                DialogueLogEntry current = backlog.Entries[2];

                int sceneIndex = playthrough.FindSceneIndexBySerial(second.lineSequence);
                Check(sceneIndex == 0 && playthrough.FindSceneIndexBySerial(current.lineSequence) == -1, "장면 찾기가 틀렸다");

                int sceneStart = playthrough.Scenes[sceneIndex].Checkpoint.HostSerialStart;
                Check(BacklogForkPoints.TryMakeLineTarget(backlog.Entries, second, sceneStart, out LineTarget target) && target.Occurrence == 2, "줄 좌표가 틀렸다");

                new PlaythroughForkService(store, ContentVersion).ForkFromScene(
                    playthrough,
                    sceneIndex,
                    PresentationMemory.Backlog(BacklogForkPoints.Before(backlog.Entries, sceneStart)),
                    PresentationMemory.ResumePoint(target, PresentationMemory.ReadUnitChoices(playthrough.Scenes[sceneIndex].HostState)));

                var driver = new ProgressionDriver();
                var replay = new ProgressionReplayState();
                var restored = new BacklogRecorder();
                Launcher(store, driver, replay, restored).Resume();

                Check(driver.LastRestorePath != null && driver.LastRestorePath.Count == 0, "갈라진 회차가 경로 복원으로 열리지 않았다");
                Check(replay.StagedPlan?.Target.Occurrence == 2, "줄 좌표가 전달되지 않았다");
                Check(restored.Entries.Count == 0 && restored.NextLineSequence == 0, "장면 앞 백로그만 물려받아야 한다(여기선 없음)");
            }));

            #endregion

            Console.WriteLine();
            Console.WriteLine($"{_passed} passed, {Failed.Count} failed.");
            return Failed.Count == 0 ? 0 : 1;
        }
        finally
        {
            if (Directory.Exists(Root)) Directory.Delete(Root, true);
        }
    }
}
