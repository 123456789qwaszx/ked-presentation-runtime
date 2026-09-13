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

    private static Ked.Progression.ProgressionState State(string episode = "scene1") =>
        Ked.Progression.ProgressionState.CreateInitial(Array.Empty<Ked.Progression.StatDefinition>(), episode);
    private static SceneEntryReport Entry() => new("chapter", State(), null, 0);
    private static SceneCommitReport Completion() => new("chapter", Array.Empty<CommittedChoice>(),
        Array.Empty<VNChoiceRecord>(), Array.Empty<string>(), State("scene2"), null,
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
                await save.ForkFromSaveSlot(store.LoadSaveSlotIndex().Slots.Single());
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
                save.LoadActiveResumePoint(); await save.PrepareNewPlaythroughAsync(); string id = save.PlaythroughId;
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
                save.LoadActiveResumePoint(); await save.PrepareNewPlaythroughAsync(); string id = save.PlaythroughId;
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
                await save.ForkFromScene(new SaveForkTarget(0,
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

            await Test("Duplicate transitions are ignored and resume starts locally", async () =>
            {
                var driver = new ProgressionDriver { IsRunning = true };
                var stopped = new TaskCompletionSource<bool>(); driver.OnStop = () => stopped.Task;
                var launcher = new ProgressionLauncher(driver, new Yarn.Unity.DialogueRunner(),
                    new UnityEngine.TextAsset(), () => null, () => Task.CompletedTask);
                int prepares = 0; Task first = launcher.TransitionAsync(() => { prepares++; return Task.CompletedTask; });
                await launcher.TransitionAsync(() => { prepares++; return Task.CompletedTask; });
                stopped.SetResult(true); await first;
                driver.IsRunning = false; await launcher.ResumeAsync();
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
