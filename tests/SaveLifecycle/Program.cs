using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

internal static class Program
{
    private static readonly List<string> Passed = new();
    private static readonly string Root = Path.Combine(Path.GetTempPath(), "save-tests-" + Guid.NewGuid().ToString("N"));
    private static string Dir() => Path.Combine(Root, Guid.NewGuid().ToString("N"));
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Throws(Action action) { try { action(); } catch (IOException) { return; } throw new Exception("Expected I/O failure"); }
    private static LocalSaveFile Save(string id, int scene = 1) => new LocalSaveFile
    {
        PlaythroughId = id, ChapterId = "chapter", CurrentEpisodeId = "scene" + scene,
        Stats = new Dictionary<string,int> { ["score"] = scene }, SavedAtUtc = "2026-09-07T00:00:00Z",
    };
    private static void Commit(PlaythroughSession session, int scene) => session.Commit(Save(session.Id,scene),
        new[] { new PendingChoice { EpisodeId = "scene" + scene, OptionIndex = scene, ChosenAt = "now" } },
        new[] { new PendingEvent { EpisodeId = "scene" + scene, OccurredAt = "now" } });
    private static ApiResult<SaveUploadResponseDto> Ack(long revision = 1) => ApiResult<SaveUploadResponseDto>.Success(200,
        new SaveUploadResponseDto { Revision = revision }, "");
    private static Task<ApiResult<T>> Ok<T>(T body) => Task.FromResult(ApiResult<T>.Success(200,body,""));
    private static async Task Test(string name, Func<Task> test) { await test(); Passed.Add(name); Console.WriteLine("PASS " + name); }
    private static Task Run(Action action) { action(); return Task.CompletedTask; }

    private sealed class Transport : ISaveSyncTransport
    {
        public Func<SyncWork,Task<ApiResult<SaveUploadResponseDto>>> Send;
        public readonly List<SyncWork> Sent = new();
        public Task<long?> CreatePlaythroughAsync(LocalSaveFile save) => Task.FromResult<long?>(10);
        public Task<int?> ResolveChapterVersionAsync(string id) => Task.FromResult<int?>(1);
        public Task<ApiResult<SaveUploadResponseDto>> UploadAsync(long id, SyncWork work)
        { Sent.Add(PlaythroughSession.Copy(work)); return Send(work); }
    }

    public static async Task Main()
    {
        try
        {
            await Test("Read/list do not create files; one session per ID", () => Run(() =>
            {
                string dir = Dir(); var store = new LocalFileSaveStore(dir); store.Initialize();
                Check(store.Open("A") == null && store.LoadActive() == null, "empty reads");
                Check(store.ListPlaythroughIds().Count == 0 && store.LoadBookmarks().Bookmarks.Count == 0, "empty lists");
                Check(!Directory.Exists(dir), "read wrote files");
                var a = store.Create(Save("A")); Check(ReferenceEquals(a,store.Open("A")), "duplicate owner");
            }));
            await Test("Commit is one write; failed write changes neither disk nor memory", () => Run(() =>
            {
                string dir = Dir(); bool fail = false; int writes = 0;
                var store = new LocalFileSaveStore(dir,(p,c) => { if(fail) throw new IOException(); writes++; AtomicFile.WriteAllText(p,c); });
                var a = store.Create(Save("A")); writes = 0; Commit(a,2);
                Check(writes == 1 && a.Read().Sync.PendingChoices.Count == 1, "not one commit");
                fail = true; Throws(() => Commit(a,3));
                Check(a.Read().Snapshot.CurrentEpisodeId == "scene2", "memory advanced");
                var disk = new LocalFileSaveStore(dir).Open("A").Read();
                Check(disk.LocalCommitVersion == 2 && disk.Sync.PendingChoices.Count == 1, "disk advanced");
            }));
            await Test("Capture is a deep snapshot; late ACK preserves newer commit", () => Run(() =>
            {
                var store = new LocalFileSaveStore(Dir()); var a = store.Create(Save("A")); Commit(a,2);
                var work = a.CaptureSyncWork(); Commit(a,3);
                work.Snapshot.Stats["score"] = 999; work.Choices.Clear();
                Check(a.CaptureSyncWork().Snapshot.Stats["score"] == 2, "mutable capture leaked");
                a.Acknowledge(work.Id,7);
                var file = a.Read(); Check(file.Snapshot.Stats["score"] == 3, "late ACK overwrote snapshot");
                Check(file.LocalCommitVersion == 3 && file.SyncedCommitVersion == 2 && a.NeedsSync, "dirty lost");
                Check(file.Sync.PendingChoices.Single().Seq == 2 && file.Sync.PendingEvents.Count == 1, "wrong prefix ACK");
            }));
            await Test("Snapshot-only commits are dirty", () => Run(() =>
            {
                var a = new LocalFileSaveStore(Dir()).Create(Save("A"));
                var work = a.CaptureSyncWork(); a.Acknowledge(work.Id,1);
                a.Commit(Save("A",2),Array.Empty<PendingChoice>(),Array.Empty<PendingEvent>());
                Check(a.NeedsSync && a.Read().Sync.PendingChoices.Count == 0, "snapshot dirty missing");
            }));
            await Test("Response loss and restart retry identical work before newer commit", async () =>
            {
                string dir = Dir(); var store = new LocalFileSaveStore(dir); var a = store.Create(Save("A")); Commit(a,2);
                var transport = new Transport { Send = w => Task.FromResult(ApiResult<SaveUploadResponseDto>.Network("lost")) };
                await new ServerSyncSaveStore(store,transport).TrySyncAsync(); Commit(a,3);
                var reopened = new LocalFileSaveStore(dir);
                var retry = new Transport { Send = w => Task.FromResult(Ack(w.CommitVersion)) };
                await new ServerSyncSaveStore(reopened,retry).TrySyncAsync();
                Check(SaveJson.Serialize(transport.Sent.Single()) == SaveJson.Serialize(retry.Sent.First()), "retry changed body");
                Check(retry.Sent.Count == 2 && retry.Sent[1].Choices.Single().Seq == 2, "new work missing");
                Check(!reopened.Open("A").NeedsSync, "not drained");
            });
            await Test("Delayed A response never acknowledges B", async () =>
            {
                var store = new LocalFileSaveStore(Dir()); var a = store.Create(Save("A")); store.SetActive("A");
                var delayed = new TaskCompletionSource<ApiResult<SaveUploadResponseDto>>();
                var transport = new Transport { Send = w => w.PlaythroughId == "A" ? delayed.Task : Task.FromResult(Ack(90)) };
                var worker = new ServerSyncSaveStore(store,transport); Task drain = worker.RequestSyncAsync("A");
                var b = store.Create(Save("B")); store.SetActive("B"); Commit(b,2); _ = worker.RequestSyncAsync("B");
                Check(b.Read().Sync.BaseRevision == null, "B touched before response");
                delayed.SetResult(Ack(12)); await drain;
                Check(store.ActiveId == "B" && a.Read().Sync.BaseRevision == 12 && b.Read().Sync.BaseRevision == 90, "identity crossed");
            });
            await Test("Failed A does not block B or spin", async () =>
            {
                var store = new LocalFileSaveStore(Dir()); store.Create(Save("A")); store.Create(Save("B"));
                var transport = new Transport { Send = w => Task.FromResult(w.PlaythroughId == "A" ? ApiResult<SaveUploadResponseDto>.Network("offline") : Ack()) };
                await new ServerSyncSaveStore(store,transport).TrySyncAsync();
                Check(transport.Sent.Count == 2 && store.Open("A").NeedsSync && !store.Open("B").NeedsSync,"worker stalled or spun");
            });
            await Test("Conflict transfer resumes after every write boundary", () => Run(() =>
            {
                for(int failAt=1;failAt<=4;failAt++)
                {
                    string dir = Dir(); int writes=0; bool inject=false;
                    var store = new LocalFileSaveStore(dir,(p,c) => { if(inject && ++writes==failAt) throw new IOException(); AtomicFile.WriteAllText(p,c); });
                    var a=store.Create(Save("A")); store.SetActive("A"); Commit(a,2);
                    inject=true; Throws(() => store.ForkConflict("A"));
                    var restart=new LocalFileSaveStore(dir); restart.Initialize();
                    var fork=restart.ForkConflict("A");
                    Check(restart.ListPlaythroughIds().Count==2,"duplicate fork on recovery");
                    Check(restart.Open("A").Read().ReleasedTo==fork.Id,"source not released");
                    Check(fork.Read().Sync.PendingChoices.Single().Seq==1,"pending lost");
                    Check(restart.ActiveId==fork.Id,"active not recovered");
                }
            }));
            await Test("Stale conflict keeps current active; transfer is idempotent", () => Run(() =>
            {
                var store=new LocalFileSaveStore(Dir()); var a=store.Create(Save("A")); Commit(a,2);
                store.Create(Save("B")); store.SetActive("B");
                var fork=store.ForkConflict("A"); Commit(fork,3);
                Check(store.ForkConflict("A").Read().Snapshot.Stats["score"]==3,"retry reset destination");
                Check(store.ActiveId=="B","late conflict changed active");
            }));
            await Test("New game completes while A upload waits; first entry persists initial save", async () =>
            {
                var store=new LocalFileSaveStore(Dir()); store.Create(Save("A")); store.SetActive("A");
                var delayed=new TaskCompletionSource<ApiResult<SaveUploadResponseDto>>();
                var transport=new Transport { Send=w=>w.PlaythroughId=="A" ? delayed.Task : Task.FromResult(Ack()) };
                var worker=new ServerSyncSaveStore(store,transport); var coordinator=new SaveCoordinator(store,worker);
                coordinator.LoadActiveResumePoint(); Task drain=worker.TrySyncAsync();
                Task prepare=coordinator.PrepareNewPlaythroughAsync(); Check(prepare.IsCompleted,"new game waited on network");
                string newId=coordinator.PlaythroughId; await coordinator.PrepareNewPlaythroughAsync();
                Check(coordinator.PlaythroughId==newId && coordinator.LoadActiveResumePoint()==null,"duplicate prepare or old resume");
                coordinator.ReportSceneEntered(new SceneEntryReport("chapter",Ked.Progression.ProgressionState.CreateInitial(Array.Empty<Ked.Progression.StatDefinition>(),"scene1"),null,0));
                Check(store.ActiveId==newId && store.LoadActive()!=null,"initial save missing");
                delayed.SetResult(Ack()); await drain; Check(store.ActiveId==newId,"late response changed active");
            });
            await Test("Bookmark fork creates one self-contained file without source", async () =>
            {
                var store=new LocalFileSaveStore(Dir()); var coordinator=new SaveCoordinator(store,null);
                await coordinator.ForkFromBookmark(new Bookmark { PlaythroughId="missing", ChapterId="chapter",
                    Checkpoint=new SceneCheckpoint { ChapterId="chapter",EpisodeId="scene1" },
                    Load=new SavedLoadPlan { Target=new SaveLineTarget { LineId="L" } } });
                Check(store.ListPlaythroughIds().Count==1 && store.LoadActive().PendingLoad.Target.LineId=="L","bookmark fork failed");
            });
            await Test("Launcher ignores duplicate transitions and obsolete resume", async () =>
            {
                var driver=new ProgressionDriver { IsRunning=true }; var stopped=new TaskCompletionSource<bool>(); driver.OnStop=()=>stopped.Task;
                var launcher=new ProgressionLauncher(driver,new Yarn.Unity.DialogueRunner(),new UnityEngine.TextAsset(),()=>null,()=>Task.CompletedTask);
                int prepares=0; Task first=launcher.TransitionAsync(()=>{prepares++;return Task.CompletedTask;});
                await launcher.TransitionAsync(()=>{prepares++;return Task.CompletedTask;});
                stopped.SetResult(true); await first; Check(prepares==1 && driver.Starts==1,"duplicate transition");
                driver.IsRunning=false; var ready=new TaskCompletionSource<bool>(); Task resume=launcher.ResumeAfterAsync(ready.Task);
                await launcher.TransitionAsync(()=>Task.CompletedTask); ready.SetResult(true); await resume;
                Check(driver.Starts==2,"stale resume launched again");
            });
            await Test("Active 409 preserves current scene and requeues latest pending", async () =>
            {
                var store=new LocalFileSaveStore(Dir()); store.ImportRestored(Save("A"),10,4,1); store.SetActive("A");
                var a=store.Open("A"); Commit(a,2);
                var delayed=new TaskCompletionSource<ApiResult<SaveUploadResponseDto>>();
                var transport=new Transport { Send=w=>w.PlaythroughId=="A" ? delayed.Task : Task.FromResult(Ack(1)) };
                var worker=new ServerSyncSaveStore(store,transport); var coordinator=new SaveCoordinator(store,worker);
                coordinator.LoadActiveResumePoint();
                coordinator.ReportSceneEntered(new SceneEntryReport("chapter",Ked.Progression.ProgressionState.CreateInitial(
                    Array.Empty<Ked.Progression.StatDefinition>(),"scene3"),null,0));
                Task drain=worker.RequestSyncAsync("A");
                delayed.SetResult(ApiResult<SaveUploadResponseDto>.Failure(409,"CONFLICT","")); await drain;
                string forkId=coordinator.PlaythroughId;
                Check(forkId!="A" && store.ActiveId==forkId && a.Read().ReleasedTo==forkId,"runtime ownership not switched");
                coordinator.ReportSceneCommitted(new SceneCommitReport("chapter",Array.Empty<CommittedChoice>(),
                    Array.Empty<VNChoiceRecord>(),Array.Empty<string>(),Ked.Progression.ProgressionState.CreateInitial(
                        Array.Empty<Ked.Progression.StatDefinition>(),"scene4"),null,Array.Empty<DialogueLogEntry>(),0,false));
                Check(store.LoadActive().CurrentEpisodeId=="scene4","current scene lost after conflict");
                Check(transport.Sent.Any(w=>w.PlaythroughId==forkId && w.Choices.Count==1),"pending not transferred");
            });
            await Test("Unexpected conflict on a new server slot stops instead of creating endless forks", async () =>
            {
                var store=new LocalFileSaveStore(Dir()); store.Create(Save("A"));
                var transport=new Transport { Send=w=>Task.FromResult(ApiResult<SaveUploadResponseDto>.Failure(409,"CONFLICT","")) };
                await new ServerSyncSaveStore(store,transport).TrySyncAsync();
                Check(transport.Sent.Count==1 && store.ListPlaythroughIds().Count==1 && store.Open("A").Read().Sync.ConflictedAtUtc!=null,"fork loop");
            });
            await Test("Completed resume starts a distinct playthrough", async () =>
            {
                var store=new LocalFileSaveStore(Dir()); var done=Save("A"); done.ChapterCompleted=true;
                store.Create(done); store.SetActive("A"); var coordinator=new SaveCoordinator(store,null);
                var launcher=new ProgressionLauncher(new ProgressionDriver(),new Yarn.Unity.DialogueRunner(),
                    new UnityEngine.TextAsset(),coordinator.LoadActiveResumePoint,coordinator.PrepareNewPlaythroughAsync);
                await launcher.LaunchAsync();
                Check(coordinator.PlaythroughId!="A" && store.LoadPlaythrough("A").ChapterCompleted,"completed timeline reused");
            });
            await TestRestore();
            Console.WriteLine($"{Passed.Count} tests passed.");
        }
        finally { if(Directory.Exists(Root)) Directory.Delete(Root,true); }
    }

    private static GuestSession Account(ServerApi api,string dir)
    {
        string path=Path.Combine(dir,"account.json");
        AtomicFile.WriteAllText(path,SaveJson.Serialize(new AccountFile { UserId=1,Token="test",ExpiresAtUtc=DateTime.UtcNow.AddDays(1).ToString("o") }));
        return new GuestSession(api,path);
    }

    private static async Task TestRestore()
    {
        await Test("Partial restore resumes without replacing local edits", async () =>
        {
            string dir=Dir(); var store=new LocalFileSaveStore(dir); bool failB=true;
            var api=new ServerApi
            {
                OnList=()=>Ok(new List<PlaythroughSummaryDto> {
                    new PlaythroughSummaryDto { Id=1,ClientPlaythroughId="A",ChapterId="chapter",LastSavedAt="2" },
                    new PlaythroughSummaryDto { Id=2,ClientPlaythroughId="B",ChapterId="chapter",LastSavedAt="1" }}),
                OnSave=id=>id==2&&failB ? Task.FromResult(ApiResult<SaveSlotDetailDto>.Network("offline"))
                    : Ok(new SaveSlotDetailDto { Revision=5,Snapshot=JToken.Parse(SaveJson.Serialize(Save(id==1?"A":"B"))) }),
                OnChoices=id=>Ok(new List<ChoiceHistoryItemDto>()),
                OnBookmarks=()=>Ok(new List<BookmarkDetailDto>()),
            };
            await new ServerRestore(api,Account(api,dir),store).RestoreAsync();
            Check(!store.LoadRestoreProgress().Completed && store.Open("A")!=null,"partial marked complete");
            Commit(store.Open("A"),7); failB=false;
            var restart=new LocalFileSaveStore(dir);
            await new ServerRestore(api,Account(api,dir),restart).RestoreAsync();
            Check(restart.LoadRestoreProgress().Completed && restart.Open("B")!=null,"restore not resumed");
            Check(restart.LoadPlaythrough("A").Stats["score"]==7,"local progress overwritten");
        });
        await Test("Delayed restore cannot override explicit new game", async () =>
        {
            string dir=Dir(); var store=new LocalFileSaveStore(dir);
            var gate=new TaskCompletionSource<ApiResult<List<PlaythroughSummaryDto>>>();
            var api=new ServerApi {
                OnList=()=>gate.Task,
                OnSave=id=>Ok(new SaveSlotDetailDto { Revision=5,Snapshot=JToken.Parse(SaveJson.Serialize(Save("remote"))) }),
                OnChoices=id=>Ok(new List<ChoiceHistoryItemDto>()),OnBookmarks=()=>Ok(new List<BookmarkDetailDto>()) };
            var restore=new ServerRestore(api,Account(api,dir),store);
            var coordinator=new SaveCoordinator(store,null,restore:restore);
            Task startup=coordinator.SyncPendingAsync(); await coordinator.PrepareNewPlaythroughAsync();
            coordinator.ReportSceneEntered(new SceneEntryReport("chapter",Ked.Progression.ProgressionState.CreateInitial(Array.Empty<Ked.Progression.StatDefinition>(),"scene1"),null,0));
            string active=store.ActiveId;
            gate.SetResult(ApiResult<List<PlaythroughSummaryDto>>.Success(200,new List<PlaythroughSummaryDto> {
                new PlaythroughSummaryDto { Id=1,ClientPlaythroughId="remote",ChapterId="chapter" } },""));
            await startup; Check(store.ActiveId==active && store.Open("remote")!=null,"restore stole active");
        });
    }
}
