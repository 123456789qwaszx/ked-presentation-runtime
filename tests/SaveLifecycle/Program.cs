using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

internal static class Program
{
    private const string ContentVersion = "test-v1";
    private const string Timestamp = "2026-09-13T00:00:00.0000000Z";
    private static readonly string Root = Path.Combine(Path.GetTempPath(), "save-tests-" + Guid.NewGuid().ToString("N"));
    private static int _passed;

    private static string Dir() => Path.Combine(Root, Guid.NewGuid().ToString("N"));
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Throws<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new Exception("Expected " + typeof(T).Name); }
    private static Task Run(Action action) { action(); return Task.CompletedTask; }
    private static async Task Test(string name, Func<Task> body)
    { await body(); _passed++; Console.WriteLine("PASS " + name); }

    private static LocalSaveFile Save(string id, int scene = 1) => new()
    {
        PlaythroughId = id,
        ContentVersion = ContentVersion,
        ChapterId = "chapter",
        CurrentEpisodeId = "scene" + scene,
        Stats = new Dictionary<string, int> { ["score"] = scene },
        SavedAtUtc = Timestamp,
    };

    private static SceneCheckpoint Checkpoint(int seconds = 0) => new()
    {
        ChapterId = "chapter",
        EpisodeId = "scene1",
        Stats = new Dictionary<string, int>(),
        BacklogSerialStart = 0,
        PlaySecondsAtEntry = seconds,
        EnteredAtUtc = Timestamp,
    };

    private static SaveSlotEntry SlotEntry(string id, string key = null) => new()
    {
        Id = id,
        DataKey = key,
        Label = "slot",
        Preview = "line",
        ChapterId = "chapter",
        SavedAtUtc = Timestamp,
        PlaySeconds = 3,
    };

    private static SaveSlotData SlotData(string id, string line = "L") => new()
    {
        Id = id,
        ContentVersion = ContentVersion,
        Checkpoint = Checkpoint(3),
        LoadPlan = new SavedLoadPlan
        {
            Target = new SaveLineTarget { NodeName = "node", LineId = line, Occurrence = 1 },
        },
        PlaySeconds = 3,
    };

    private static Ked.Progression.ChapterState State(string episode = "scene1") =>
        Ked.Progression.ChapterState.CreateInitial(Array.Empty<Ked.Progression.StatDefinition>(), episode);

    private static Ked.Progression.ChapterState StateWithScore(int score) =>
        Ked.Progression.ChapterState.CreateInitial(
            new[]
            {
                new Ked.Progression.StatDefinition(
                    "score",
                    "Score",
                    Ked.Progression.StatType.Number,
                    score,
                    0,
                    100),
            },
            "scene1");

    private static SceneEntryReport Entry() => new("chapter", State(), 0);
    private static SceneCommitReport Completion() => new("chapter", Array.Empty<Ked.Progression.CommittedChoice>(),
        Array.Empty<VNChoiceRecord>(), Array.Empty<string>(), State("scene2"),
        Array.Empty<DialogueLogEntry>(), 0, false);

    public static async Task Main()
    {
        try
        {
            await Test("Reads do not create files and one session owns each playthrough", () => Run(() =>
            {
                string dir = Dir(); var store = new LocalFileSaveStore(dir); store.Initialize();
                Check(store.Open("A") == null && store.LoadActive() == null, "empty read");
                Check(store.LoadSaveSlotIndex().Slots.Count == 0 && !Directory.Exists(dir), "read wrote files");
                PlaythroughSession session = store.Create(Save("A"));
                Check(ReferenceEquals(session, store.Open("A")), "duplicate session");
            }));

            await Test("Commit failure changes neither disk nor memory", () => Run(() =>
            {
                string dir = Dir(); bool fail = false;
                var store = new LocalFileSaveStore(dir, (path, json) =>
                { if (fail) throw new IOException(); AtomicFile.WriteAllText(path, json); });
                PlaythroughSession session = store.Create(Save("A"));
                session.Commit(Save("A", 2)); fail = true;
                Throws<IOException>(() => session.Commit(Save("A", 3)));
                Check(session.Read().Snapshot.CurrentEpisodeId == "scene2", "memory advanced");
                Check(new LocalFileSaveStore(dir).LoadPlaythrough("A").CurrentEpisodeId == "scene2", "disk advanced");
            }));

            await Test("Malformed playthrough data is rejected at the read boundary", () => Run(() =>
            {
                string dir = Dir(); string path = Path.Combine(dir, "playthroughs-v3", "A.json");
                LocalSaveFile malformed = Save("A"); malformed.Scenes = null;
                AtomicFile.WriteAllText(path, SaveJson.Serialize(new PlaythroughFile
                { FormatVersion = SaveFormat.PlaythroughVersion, Snapshot = malformed }));
                Throws<InvalidDataException>(() => new LocalFileSaveStore(dir).LoadPlaythrough("A"));
            }));

            await Test("A different content version is not resumed", () => Run(() =>
            {
                var store = new LocalFileSaveStore(Dir()); store.Create(Save("A")); store.SetActive("A");
                Check(new SaveCoordinator(store, "test-v2").LoadActiveResumePoint() == null, "incompatible save resumed");
            }));

            await Test("Save slot index is light and overwrite is atomic", () => Run(() =>
            {
                string dir = Dir(); bool failIndex = false;
                var store = new LocalFileSaveStore(dir, (path, json) =>
                { if (failIndex && Path.GetFileName(path) == "save-slots.json") throw new IOException(); AtomicFile.WriteAllText(path, json); });
                store.WriteSaveSlot(SlotEntry("slot"), SlotData("slot", "first"));
                SaveSlotIndexFile index = store.LoadSaveSlotIndex();
                Check(index.Slots.Single().DataKey != null && SaveJson.Serialize(index).Contains("checkpoint") == false, "body leaked into index");
                failIndex = true;
                Throws<IOException>(() => store.WriteSaveSlot(SlotEntry("slot"), SlotData("slot", "second")));
                Check(new LocalFileSaveStore(dir).LoadSaveSlot("slot").LoadPlan.Target.LineId == "first", "failed overwrite replaced slot");
                failIndex = false;
                store.WriteSaveSlot(SlotEntry("slot"), SlotData("slot", "second"));
                Check(store.LoadSaveSlot("slot").LoadPlan.Target.LineId == "second", "overwrite failed");
                Check(Directory.GetFiles(Path.Combine(dir, "save-slot-data")).Length == 1, "orphan body retained");
            }));

            await Test("Legacy local bookmark files remain readable", () => Run(() =>
            {
                string dir = Dir(); string key = "legacybody";
                AtomicFile.WriteAllText(Path.Combine(dir, "bookmarks.json"), SaveJson.Serialize(new
                {
                    bookmarks = new[] { new
                    {
                        id = "legacy", snapshotKey = key, label = "old", preview = "line",
                        chapterId = "chapter", createdAtUtc = Timestamp, playSecondsAtBookmark = 3,
                    } },
                }));
                AtomicFile.WriteAllText(Path.Combine(dir, "bookmark-snapshots", key + ".json"), SaveJson.Serialize(new
                {
                    id = "legacy", checkpoint = Checkpoint(3), load = SlotData("legacy").LoadPlan,
                    scenes = Array.Empty<SceneRecord>(), backlog = Array.Empty<DialogueLogEntry>(),
                    playSecondsAtBookmark = 3,
                }));
                var store = new LocalFileSaveStore(dir);
                SaveSlotEntry entry = store.LoadSaveSlotIndex().Slots.Single();
                Check(entry.Label == "old" && store.LoadSaveSlot("legacy").LoadPlan.Target.LineId == "L",
                    "legacy slot was not converted");
            }));

            await Test("A different content version blocks a manual slot before transition", () => Run(() =>
            {
                var store = new LocalFileSaveStore(Dir());
                SaveSlotData data = SlotData("slot"); data.ContentVersion = "test-v2";
                store.WriteSaveSlot(SlotEntry("slot"), data);
                var save = new SaveCoordinator(store, ContentVersion);
                Throws<InvalidOperationException>(() => save.LoadSaveSlot("slot"));
                Check(store.ActiveId == null, "incompatible slot changed active save");
            }));

            await Test("Manual slot loads as a new playthrough and keeps its source", async () =>
            {
                var store = new LocalFileSaveStore(Dir());
                store.WriteSaveSlot(SlotEntry("slot"), SlotData("slot"));
                var save = new SaveCoordinator(store, ContentVersion);
                SaveSlotEntry slot = store.LoadSaveSlotIndex().Slots.Single();
                save.ForkFromSaveSlot(slot, save.LoadSaveSlot(slot.Id));
                string loadedId = store.ActiveId;
                store.Open(loadedId).Commit(Save(loadedId, 8));
                Check(store.LoadSaveSlot("slot").LoadPlan.Target.LineId == "L", "slot followed autosave");
            });

            await Test("New game keeps the old active save until its first file succeeds", async () =>
            {
                string dir = Dir(); bool fail = false;
                var store = new LocalFileSaveStore(dir, (path, json) =>
                { if (fail) throw new IOException(); AtomicFile.WriteAllText(path, json); });
                store.Create(Save("A")); store.SetActive("A"); var save = new SaveCoordinator(store, ContentVersion);
                save.LoadActiveResumePoint(); save.BeginNewPlaythrough(); string id = save.PlaythroughId;
                fail = true; Throws<IOException>(() => save.ReportSceneEntered(Entry()));
                Check(store.ActiveId == "A", "old active was lost");
                fail = false; save.ReportSceneEntered(Entry());
                Check(store.ActiveId == id, "new game was not activated");
            });

            await Test("Active pointer failure preserves the old resume and permits retry", async () =>
            {
                string dir = Dir(); bool fail = false;
                var store = new LocalFileSaveStore(dir, (path, json) =>
                { if (fail && Path.GetFileName(path) == "active.json") throw new IOException(); AtomicFile.WriteAllText(path, json); });
                store.Create(Save("A")); store.SetActive("A"); var save = new SaveCoordinator(store, ContentVersion);
                save.LoadActiveResumePoint(); save.BeginNewPlaythrough(); string id = save.PlaythroughId;
                fail = true; Throws<IOException>(() => save.ReportSceneEntered(Entry()));
                Check(store.ActiveId == "A", "old active was lost");
                fail = false; save.ReportSceneEntered(Entry()); save.ReportSceneCommitted(Completion());
                Check(store.ActiveId == id && store.LoadActive().CurrentEpisodeId == "scene2", "retry failed");
            });

            await Test("Completed scene fork preserves replay path and elapsed time", async () =>
            {
                var store = new LocalFileSaveStore(Dir()); LocalSaveFile file = Save("A");
                file.PlaySeconds = 20;
                file.Scenes.Add(new SceneRecord
                {
                    Checkpoint = Checkpoint(12), BacklogSerialEnd = 0,
                    Path = new List<SavedChoice> { new() { FromEpisodeId = "scene1", OptionIndex = 1 } },
                });
                store.Create(file); store.SetActive("A"); var save = new SaveCoordinator(store, ContentVersion);
                save.LoadActiveResumePoint();
                save.ForkFromScene(new SaveForkTarget(0,
                    new SaveLineTarget { NodeName = "node", LineId = "L", Occurrence = 1 }));
                LocalSaveFile fork = store.LoadActive();
                Check(fork.PlaythroughId != "A" && fork.PendingLoad.Path.Single().OptionIndex == 1
                    && fork.PlaySeconds == 12, "fork state lost");
            });

            await Test("Retention keeps only active autosave because slots are self-contained", () => Run(() =>
            {
                var store = new LocalFileSaveStore(Dir());
                store.Create(Save("active")); store.Create(Save("source")); store.SetActive("active");
                store.WriteSaveSlot(SlotEntry("slot"), SlotData("slot"));
                Check(store.CollectUnusedPlaythroughs() == 1 && store.LoadPlaythrough("source") == null, "old autosave retained");
                Check(store.LoadActive() != null && store.LoadSaveSlot("slot") != null, "live data removed");
            }));

            await Test("Old server metadata is ignored and not written again", () => Run(() =>
            {
                string dir = Dir(); string path = Path.Combine(dir, "playthroughs-v3", "A.json");
                JObject legacy = JObject.FromObject(new
                {
                    formatVersion = SaveFormat.PlaythroughVersion,
                    snapshot = JObject.Parse(SaveJson.Serialize(Save("A"))),
                    localCommitVersion = 7,
                    sync = new { baseRevision = 42 },
                });
                AtomicFile.WriteAllText(path, legacy.ToString());
                PlaythroughSession session = new LocalFileSaveStore(dir).Open("A"); session.Commit(Save("A", 2));
                JObject written = JObject.Parse(File.ReadAllText(path));
                Check(written["localCommitVersion"] == null && written["sync"] == null, "server fields survived");
            }));

            await Test("Active resume keeps the scene entry snapshot and replay data", () => Run(() =>
            {
                var store = new LocalFileSaveStore(Dir());
                LocalSaveFile file = Save("A");
                file.Scenes.Add(new SceneRecord
                {
                    Checkpoint = Checkpoint(),
                    BacklogSerialEnd = 1,
                });
                file.Backlog.Add(new DialogueLogEntry
                {
                    lineId = "old-line",
                    lineSerial = 0,
                    nodeName = "old-node",
                    rawText = "old",
                });
                store.Create(file);
                store.SetActive("A");

                var save = new SaveCoordinator(store, ContentVersion);
                save.LoadActiveResumePoint();
                save.ReportSceneEntered(new SceneEntryReport("chapter", StateWithScore(20), 1));

                save.UpdateResumePoint(
                    new[] { new Ked.Progression.CommittedChoice("scene1", 2) },
                    new[] { new VNChoiceRecord(0, 0, 1, "choice-line") },
                    new SaveLineTarget { NodeName = "node", LineId = "line", Occurrence = 2 });

                LocalSaveFile snapshot = store.LoadActive();
                Check(snapshot.CurrentEpisodeId == "scene1", "resume left the scene entry");
                Check(snapshot.Stats["score"] == 20, "effective state was stored as entry state");
                Check(snapshot.Scenes.Count == 1, "completed scenes changed");
                Check(snapshot.Backlog.Count == 1 && snapshot.Backlog[0].lineId == "old-line",
                    "current scene backlog leaked into the snapshot");
                Check(snapshot.PendingLoad.Path.Single().OptionIndex == 2, "progression path missing");
                Check(snapshot.PendingLoad.YarnChoices.Single().selectedOptionLineId == "choice-line",
                    "Yarn choices missing");
                Check(snapshot.PendingLoad.Target.Occurrence == 2, "line occurrence missing");
            }));

            await Test("Failed active resume update keeps the previous disk and memory snapshot", () => Run(() =>
            {
                string dir = Dir();
                bool fail = false;
                var store = new LocalFileSaveStore(dir, (path, json) =>
                {
                    if (fail && path.EndsWith(".json", StringComparison.Ordinal))
                        throw new IOException();

                    AtomicFile.WriteAllText(path, json);
                });
                var save = new SaveCoordinator(store, ContentVersion);
                save.BeginNewPlaythrough();
                save.ReportSceneEntered(Entry());
                save.UpdateResumePoint(
                    Array.Empty<Ked.Progression.CommittedChoice>(),
                    Array.Empty<VNChoiceRecord>(),
                    new SaveLineTarget { NodeName = "node", LineId = "first", Occurrence = 1 });

                fail = true;
                Throws<IOException>(() => save.UpdateResumePoint(
                    Array.Empty<Ked.Progression.CommittedChoice>(),
                    Array.Empty<VNChoiceRecord>(),
                    new SaveLineTarget { NodeName = "node", LineId = "second", Occurrence = 1 }));

                Check(store.Open(save.PlaythroughId).Read().Snapshot.PendingLoad.Target.LineId == "first",
                    "failed update changed memory");
                Check(new LocalFileSaveStore(dir).LoadPlaythrough(save.PlaythroughId)
                        .PendingLoad.Target.LineId == "first",
                    "failed update changed disk");
            }));

            await Test("Scene commit clears the active line resume plan", () => Run(() =>
            {
                var store = new LocalFileSaveStore(Dir());
                var save = new SaveCoordinator(store, ContentVersion);
                save.BeginNewPlaythrough();
                save.ReportSceneEntered(Entry());
                save.UpdateResumePoint(
                    Array.Empty<Ked.Progression.CommittedChoice>(),
                    Array.Empty<VNChoiceRecord>(),
                    new SaveLineTarget { NodeName = "node", LineId = "line", Occurrence = 1 });

                save.ReportSceneCommitted(Completion());

                Check(store.LoadActive().PendingLoad == null, "committed scene kept a line resume plan");
            }));

            await Test("Resume without a load plan starts from the scene root", () => Run(() =>
            {
                var store = new LocalFileSaveStore(Dir());
                store.Create(Save("A"));
                store.SetActive("A");

                var driver = new ProgressionDriver();
                var replay = new ProgressionReplayState();
                var launcher = new ProgressionLauncher(
                    driver,
                    new Yarn.Unity.DialogueRunner(),
                    new UnityEngine.TextAsset(),
                    new SaveCoordinator(store, ContentVersion),
                    new BacklogRecorder(),
                    replay);

                launcher.Resume();

                Check(driver.Starts == 1 && driver.LastRestorePath == null,
                    "scene root resume was treated as mid-scene restore");
                Check(replay.PreparedLoads == 1 && replay.StagedTarget == null,
                    "old staged load plan was not cleared");
            }));

            await Test("Resume with an empty path still opens line seek", () => Run(() =>
            {
                var store = new LocalFileSaveStore(Dir());
                LocalSaveFile file = Save("A");
                file.PendingLoad = new SavedLoadPlan
                {
                    Target = new SaveLineTarget
                    {
                        NodeName = "node",
                        LineId = "line",
                        Occurrence = 2,
                    },
                };
                store.Create(file);
                store.SetActive("A");

                var driver = new ProgressionDriver();
                var replay = new ProgressionReplayState();
                var launcher = new ProgressionLauncher(
                    driver,
                    new Yarn.Unity.DialogueRunner(),
                    new UnityEngine.TextAsset(),
                    new SaveCoordinator(store, ContentVersion),
                    new BacklogRecorder(),
                    replay);

                launcher.Resume();

                Check(driver.LastRestorePath != null && driver.LastRestorePath.Count == 0,
                    "empty replay path was collapsed into scene root resume");
                Check(replay.StagedTarget?.Occurrence == 2, "line target was not staged");
            }));

            await Test("Duplicate transitions are ignored and resume starts locally", async () =>
            {
                var driver = new ProgressionDriver { IsRunning = true };
                var stopped = new TaskCompletionSource<bool>(); driver.OnStop = () => stopped.Task;
                var launcher = new ProgressionLauncher(driver, new Yarn.Unity.DialogueRunner(),
                    new UnityEngine.TextAsset(), new SaveCoordinator(new LocalFileSaveStore(Dir()), ContentVersion),
                    new BacklogRecorder(), new ProgressionReplayState());
                int prepares = 0; Task first = launcher.TransitionAndResumeAsync(() => prepares++);
                await launcher.TransitionAndResumeAsync(() => prepares++);
                stopped.SetResult(true); await first;
                driver.IsRunning = false; launcher.Resume();
                Check(prepares == 1 && driver.Starts == 2, "transition guard failed");
            });

            Console.WriteLine($"{_passed} tests passed.");
        }
        finally
        {
            if (Directory.Exists(Root)) Directory.Delete(Root, true);
        }
    }
}
