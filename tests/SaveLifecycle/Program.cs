using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Ked.Progression;
using Ked.Progression.Save;

// 저장의 호스트(연출) 겹 회귀 하네스. 저장 정책 자체는 원본 저장소의 tests/ProgressionSave가 본다.
// 연출 기억의 입구(PlaybackMemory)와 그것을 쓰는 두 예제 — 진행 어댑터, 연출 단독 회차 — 를 본다.
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

    private static async Task Until(Func<bool> condition, string what)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        while (!condition())
        {
            if (clock.ElapsedMilliseconds > 3000) throw new Exception("시간 초과: " + what);
            await Task.Delay(2);
        }
    }

    private static PresentationResumePoint SomeResume() =>
        new(new LineTarget { NodeName = "stale", LineId = "stale", Occurrence = 1 }, Array.Empty<VNChoiceRecord>());

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

    private static ProgressionLauncher Launcher(ILocalSaveStore store, ProgressionDriver driver, PresentationRig presentation) =>
        new(driver, Scenario(), Coordinator(store), presentation.Memory);

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
                var presentation = new PresentationRig();
                var save = Coordinator(store);
                var bridge = new ProgressionSaveBridge(save, presentation.Memory);

                presentation.Show("old-line", "old-node");
                presentation.OpenAndTakeResume();
                presentation.Choices.RestoreChoices(new[] { new VNChoiceRecord(0, 0, 2, "picked") });

                save.BeginNewPlaythrough();
                bridge.EnterScene("chapter", State());
                presentation.Show("new-line");
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
                var presentation = new PresentationRig();
                presentation.Unit.StageResume(SomeResume()); // 앞 실행이 남긴 예약
                Launcher(store, driver, presentation).Resume();

                Check(driver.Starts == 1 && driver.LastRestorePath == null, "루트 이어하기를 장면 중간 복원으로 다뤘다");
                Check(presentation.Backlog.Entries.Single().lineId == "old" && presentation.Backlog.NextLineSequence == 1, "백로그가 복원되지 않았다");
                Check(presentation.OpenAndTakeResume() == null, "옛 재생 계획이 지워지지 않았다");
            }));

            await Test("런처: 빈 경로라도 재생 계획이 있으면 줄 시크를 연다", () => Run(() =>
            {
                var store = new LocalFileSaveStore(Dir());
                PlaythroughSave file = Save("A");
                file.PendingLoad = new SavedLoadPlan { HostState = PresentationMemory.ResumePoint(Target("line", 2), Array.Empty<VNChoiceRecord>()) };
                store.Create(file);
                store.SetActive("A");

                var driver = new ProgressionDriver();
                var presentation = new PresentationRig();
                Launcher(store, driver, presentation).Resume();

                Check(driver.LastRestorePath != null && driver.LastRestorePath.Count == 0, "빈 경로가 루트 이어하기로 뭉개졌다");
                Check(presentation.OpenAndTakeResume()?.Target.Occurrence == 2, "줄 좌표가 올라가지 않았다");
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
                Launcher(store, driver, new PresentationRig()).Resume();

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
                var presentation = new PresentationRig();
                var launcher = Launcher(store, driver, presentation);
                launcher.Resume();

                Check(driver.Starts == 1 && driver.LastRestorePath == null, "새 게임으로 가지 않았다");
                Check(UnityEngine.Debug.Warnings.Any(w => w.Contains("연출 상태")), "경고가 없다");
                Check(presentation.OpenAndTakeResume() == null, "깨진 계획이 올라갔다");
            }));

            await Test("런처: 겹친 전환은 무시되고 이어하기는 로컬에서 시작한다", async () =>
            {
                var driver = new ProgressionDriver { IsRunning = true };
                var stopped = new TaskCompletionSource<bool>(); driver.OnStop = () => stopped.Task;
                var launcher = Launcher(new LocalFileSaveStore(Dir()), driver, new PresentationRig());
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
                var presentation = new PresentationRig();
                var save = Coordinator(store);
                var bridge = new ProgressionSaveBridge(save, presentation.Memory);

                // 장면 1: 줄 0,1 — 장면 2: 줄 2
                save.BeginNewPlaythrough();
                presentation.OpenAndTakeResume();
                bridge.EnterScene("chapter", State());
                presentation.Show("line");
                presentation.Show("line");
                bridge.CommitScene("chapter", Completion(), SceneRunOutcome.SceneEnded);
                presentation.OpenAndTakeResume();
                bridge.EnterScene("chapter", State("scene2"));
                presentation.Show("line");

                PlaythroughSaveSnapshot playthrough = save.Capture();
                DialogueLogEntry second = presentation.Backlog.Entries[1];
                DialogueLogEntry current = presentation.Backlog.Entries[2];

                int sceneIndex = playthrough.FindSceneIndexBySerial(second.lineSequence);
                Check(sceneIndex == 0 && playthrough.FindSceneIndexBySerial(current.lineSequence) == -1, "장면 찾기가 틀렸다");

                // ManualSaveFlow.ForkFromBacklogAsync가 하는 일 — 줄 세기는 연출 기억의 몫이다.
                SceneRecord origin = playthrough.Scenes[sceneIndex];
                presentation.Memory.ReadPastUnit(second, origin.Checkpoint.HostSerialStart, origin.HostState,
                    out string inherited, out string resumePoint);
                Check(resumePoint != null, "줄 좌표를 만들지 못했다");

                new PlaythroughForkService(store, ContentVersion).ForkFromScene(playthrough, sceneIndex, inherited, resumePoint);

                var driver = new ProgressionDriver();
                var restored = new PresentationRig();
                Launcher(store, driver, restored).Resume();

                Check(driver.LastRestorePath != null && driver.LastRestorePath.Count == 0, "갈라진 회차가 경로 복원으로 열리지 않았다");
                Check(restored.Backlog.Entries.Count == 0 && restored.Backlog.NextLineSequence == 0, "장면 앞 백로그만 물려받아야 한다(여기선 없음)");
                PresentationResumePoint resume = restored.OpenAndTakeResume();
                Check(resume?.Target.Occurrence == 2 && resume.Target.LineId == "line", "줄 좌표가 전달되지 않았다");
            }));

            #endregion

            #region 연출 기억의 입구

            await Test("연출 기억: 지금 줄까지 꺼내 되돌리면 다음 단위가 그 줄까지 시크한다", () => Run(() =>
            {
                var before = new PresentationRig();
                before.Show("prev");
                before.OpenAndTakeResume();                       // 단위 경계 — prev는 단위 앞 줄
                before.Show("a");
                before.Show("b");
                before.Show("a");
                before.Choices.RestoreChoices(new[] { new VNChoiceRecord(1, 0, 1, "picked") });

                before.Stage.IsSeeking = true;
                Check(!before.Memory.TryCaptureResumePoint(out _, out _), "시크 중에 재개 지점을 꺼냈다");
                before.Stage.IsSeeking = false;

                Check(before.Memory.TryCaptureResumePoint(out string resumePoint, out string preview), "재개 지점을 꺼내지 못했다");
                Check(preview == "a 대사", "미리보기가 지금 줄이 아니다");
                string backlog = before.Memory.CaptureBacklogBeforeUnit();
                Check(PresentationMemory.ReadBacklog(before.Memory.CaptureBacklog()).Count == 4, "백로그 전부가 아니다");

                var after = new PresentationRig();
                after.Memory.Restore(backlog, resumePoint);

                Check(after.Backlog.Entries.Single().lineId == "prev", "단위 앞 백로그만 되돌아와야 한다");
                PresentationResumePoint resume = after.OpenAndTakeResume();
                Check(resume != null && resume.Target.LineId == "a" && resume.Target.Occurrence == 2, "줄 좌표(두 번째 a)가 아니다");
                Check(resume.YarnChoices.Single().selectedOptionLineId == "picked", "선택 기록이 실리지 않았다");
                Check(after.OpenAndTakeResume() == null, "재개 예약이 두 번째 단위에도 걸렸다");
            }));

            await Test("연출 기억: 하나라도 깨져 있으면 아무것도 바꾸지 않는다", () => Run(() =>
            {
                var presentation = new PresentationRig();
                presentation.Show("kept");

                Throws<InvalidDataException>(() => presentation.Memory.Restore(PresentationMemory.Backlog(Array.Empty<DialogueLogEntry>()), "{garbage"));
                Throws<InvalidDataException>(() => presentation.Memory.Restore("{garbage", null));

                Check(presentation.Backlog.Entries.Single().lineId == "kept", "깨진 기억으로 백로그를 바꿨다");
                Check(presentation.OpenAndTakeResume() == null, "깨진 기억으로 재개를 걸었다");

                presentation.Memory.Restore("", null);
                Check(presentation.Backlog.Entries.Count == 0, "빈 백로그 문자열은 빈 백로그다");
            }));

            await Test("연출 기억: 과거 단위의 선택 기록이 깨졌으면 그 단위의 루트에서 시작한다", () => Run(() =>
            {
                var presentation = new PresentationRig();
                presentation.Show("before");
                presentation.Show("line");

                presentation.Memory.ReadPastUnit(presentation.Backlog.Entries[1], 1, "{garbage",
                    out string inherited, out string resumePoint);

                Check(resumePoint == null, "깨진 선택 기록으로 재개 지점을 만들었다");
                Check(PresentationMemory.ReadBacklog(inherited).Single().lineId == "before", "물려받는 백로그가 단위 앞까지가 아니다");
                Check(UnityEngine.Debug.Warnings.Any(w => w.Contains("선택 기록")), "경고가 없다");
            }));

            #endregion

            #region 연출 단독 회차 — 진행 없이 같은 기억으로

            await Test("연출 단독: 줄마다 쓰고, 타이틀로 나가도 남고, 이어하기는 그 줄까지 시크한다", async () =>
            {
                string path = Path.Combine(Dir(), "resume.json");

                var first = new PresentationRig();
                first.Stage.BlockPlays = true;
                var host = new NodePlaybackHost(first.Unit);
                var playthrough = new NodePlaythrough(host, first.Memory, "node", path);

                playthrough.StartNew();
                await Until(() => first.Stage.Plays == 1, "첫 재생");

                first.Show("a");
                playthrough.SaveCurrentLine();
                first.Show("b");
                playthrough.SaveCurrentLine();
                Check(File.Exists(path), "이어하기 파일을 쓰지 않았다");

                await host.ExitAsync();                           // 타이틀로
                await Until(() => !host.IsRunning, "끝내기");
                Check(File.Exists(path), "끝내기로 끊긴 회차의 파일을 지웠다");

                var second = new PresentationRig();               // 다시 켠 것과 같다
                var host2 = new NodePlaybackHost(second.Unit);
                var resumed = new NodePlaythrough(host2, second.Memory, "node", path);

                resumed.Resume();
                await Until(() => second.Stage.Resumes == 1, "재개 시크");

                Check(second.Stage.Resumed.Target.LineId == "b" && second.Stage.Resumed.Target.NodeName == "node", "마지막으로 본 줄이 아니다");
                await Until(() => !host2.IsRunning, "노드 끝");
                Check(!File.Exists(path), "끝까지 본 노드의 파일이 남았다");
            });

            await Test("연출 단독: 다른 노드나 깨진 파일이면 처음부터 시작한다", async () =>
            {
                string path = Path.Combine(Dir(), "resume.json");
                Directory.CreateDirectory(Path.GetDirectoryName(path));

                File.WriteAllText(path, "{garbage");
                var broken = new PresentationRig();
                var brokenHost = new NodePlaybackHost(broken.Unit);
                new NodePlaythrough(brokenHost, broken.Memory, "node", path).Resume();
                await Until(() => broken.Stage.Plays == 1 && !brokenHost.IsRunning, "처음부터 재생");
                Check(broken.Stage.Resumes == 0, "깨진 파일로 재개를 걸었다");

                var source = new PresentationRig();
                source.Stage.BlockPlays = true;
                var sourceHost = new NodePlaybackHost(source.Unit);
                var sourcePlaythrough = new NodePlaythrough(sourceHost, source.Memory, "other-node", path);
                sourcePlaythrough.StartNew();
                await Until(() => source.Stage.Plays == 1, "다른 노드 재생");
                source.Show("x", "other-node");
                sourcePlaythrough.SaveCurrentLine();
                await sourceHost.ExitAsync();

                var mismatch = new PresentationRig();
                var mismatchHost = new NodePlaybackHost(mismatch.Unit);
                new NodePlaythrough(mismatchHost, mismatch.Memory, "node", path).Resume();
                await Until(() => mismatch.Stage.Plays == 1 && !mismatchHost.IsRunning, "처음부터 재생");
                Check(mismatch.Stage.Resumes == 0, "다른 노드의 기억으로 재개를 걸었다");
            });

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
