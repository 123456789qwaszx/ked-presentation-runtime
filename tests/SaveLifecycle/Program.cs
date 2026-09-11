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
        public Func<string,long,Task<bool>> Publish;
        public Task<bool> SetResumeAsync(string id,long selection,string scope) => Publish?.Invoke(id,selection) ?? Task.FromResult(true);
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
                await new ServerSyncSaveStore(reopened,retry).RetryAsync("A");
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
                int stops=0; driver.OnStop=()=>{stops++;return Task.CompletedTask;};
                await launcher.TransitionAfterAsync(()=>Task.FromResult(false),()=>throw new Exception("should not prepare"));
                Check(stops==0 && driver.IsRunning,"failed hydration stopped gameplay");
                var downloaded=new TaskCompletionSource<bool>();
                Task load=launcher.TransitionAfterAsync(()=>downloaded.Task,()=>throw new Exception("obsolete bookmark"));
                await launcher.TransitionAsync(()=>Task.CompletedTask); downloaded.SetResult(true); await load;
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
            await TestManualSlots();
            await TestRetryAndRetention();
            await TestLearningReporter();
            Console.WriteLine($"{Passed.Count} tests passed.");
        }
        finally { if(Directory.Exists(Root)) Directory.Delete(Root,true); }
    }

    private static async Task TestLearningReporter()
    {
        await Test("Learning reporter observes persisted entry and committed path without acknowledging sync", () => Run(() =>
        {
            string dir = Dir();
            var store = new LocalFileSaveStore(dir);
            var coordinator = new SaveCoordinator(store, null);
            var logs = new List<string>();
            var reporter = new LearningProgressionReporter(coordinator, store, logs.Add);

            reporter.ReportSceneEntered(new SceneEntryReport("qwer_scene",
                Ked.Progression.ProgressionState.CreateInitial(Array.Empty<Ked.Progression.StatDefinition>(), "EP01"), null, 0));

            string id = coordinator.PlaythroughId;
            Check(store.ActiveId == id && logs.Single().Contains("SceneEntered"), "entry was not persisted before observation");
            Check(new LocalFileSaveStore(dir).LoadActive().CurrentEpisodeId == "EP01", "initial snapshot missing on disk");

            reporter.ReportSceneCommitted(new SceneCommitReport("qwer_scene",
                new[] { new CommittedChoice("EP01", 0), new CommittedChoice("EP02_01", 0) },
                Array.Empty<VNChoiceRecord>(), Array.Empty<string>(),
                Ked.Progression.ProgressionState.CreateInitial(Array.Empty<Ked.Progression.StatDefinition>(), "EP03"),
                null, Array.Empty<DialogueLogEntry>(), 0, false));

            PlaythroughFile file = new LocalFileSaveStore(dir).Open(id).Read();
            Check(logs.Count == 2 && logs[1].Contains("SceneCommitted") && logs[1].Contains("EP03"), "commit observation missing");
            Check(file.Snapshot.Scenes.Single().Path.Select(c => c.FromEpisodeId).SequenceEqual(new[] { "EP01", "EP02_01" }), "wrong committed path");
            Check(file.Snapshot.CurrentEpisodeId == "EP03" && !file.Snapshot.ChapterCompleted, "wrong resume point");
            Check(file.Sync.PendingChoices.Count == 2 && file.Sync.PlaythroughId == null && file.Sync.BaseRevision == null
                && file.SyncedCommitVersion == 0 && file.InFlight == null, "learning observation altered sync state");
        }));

        await Test("Learning reporter never reports a failed local commit as persisted", () => Run(() =>
        {
            string dir = Dir();
            bool fail = false;
            var store = new LocalFileSaveStore(dir, (path, content) =>
            {
                if (fail) throw new IOException("injected write failure");
                AtomicFile.WriteAllText(path, content);
            });
            var logs = new List<string>();
            var reporter = new LearningProgressionReporter(new SaveCoordinator(store, null), store, logs.Add);
            reporter.ReportSceneEntered(new SceneEntryReport("chapter",
                Ked.Progression.ProgressionState.CreateInitial(Array.Empty<Ked.Progression.StatDefinition>(), "scene1"), null, 0));
            fail = true;

            Throws(() => reporter.ReportSceneCommitted(new SceneCommitReport("chapter",
                Array.Empty<CommittedChoice>(), Array.Empty<VNChoiceRecord>(), Array.Empty<string>(),
                Ked.Progression.ProgressionState.CreateInitial(Array.Empty<Ked.Progression.StatDefinition>(), "scene2"),
                null, Array.Empty<DialogueLogEntry>(), 0, false)));

            Check(logs.Count == 1, "failed write was reported as a commit");
            Check(new LocalFileSaveStore(dir).LoadActive().CurrentEpisodeId == "scene1", "failed write replaced resume point");
        }));

        await Test("Learning log failure does not cancel a successful completed save", () => Run(() =>
        {
            var store = new LocalFileSaveStore(Dir());
            var reporter = new LearningProgressionReporter(new SaveCoordinator(store, null), store,
                message => throw new InvalidOperationException("unavailable observer"));
            reporter.ReportSceneEntered(new SceneEntryReport("chapter",
                Ked.Progression.ProgressionState.CreateInitial(Array.Empty<Ked.Progression.StatDefinition>(), "scene1"), null, 0));
            reporter.ReportSceneCommitted(new SceneCommitReport("chapter",
                Array.Empty<CommittedChoice>(), Array.Empty<VNChoiceRecord>(), Array.Empty<string>(),
                Ked.Progression.ProgressionState.CreateInitial(Array.Empty<Ked.Progression.StatDefinition>(), "scene1"),
                null, Array.Empty<DialogueLogEntry>(), 0, true));

            Check(store.LoadActive().ChapterCompleted && store.LoadActive().Scenes.Count == 1,
                "observer failure cancelled the successful save");
        }));
    }

    private static GuestSession Account(ServerApi api,string dir)
    {
        string path=Path.Combine(dir,"account.json");
        AtomicFile.WriteAllText(path,SaveJson.Serialize(new AccountFile { UserId=1,Token="test",ExpiresAtUtc=DateTime.UtcNow.AddDays(1).ToString("o") }));
        return new GuestSession(api,path);
    }

    private static ResumeSaveDto Resume(string id) => new ResumeSaveDto
    {
        Playthrough=new PlaythroughSummaryDto { Id=1,ClientPlaythroughId=id,ChapterId="chapter" },
        Save=new SaveSlotDetailDto { Revision=5,Snapshot=JToken.Parse(SaveJson.Serialize(Save(id))) }, NextChoiceSeq=1,
    };
    private static Bookmark Slot(string id,string source="A",int version=1) => new Bookmark
    {
        Id=id,PlaythroughId=source,LocalVersion=version,ChapterId="chapter",Preview="line",Label="slot",
        Checkpoint=new SceneCheckpoint { ChapterId="chapter",EpisodeId="scene1" },
        Load=new SavedLoadPlan { Target=new SaveLineTarget { LineId="L" } },
    };
    private static async Task TestRestore()
    {
        await Test("Resume plus paged summaries recover partially without fetching full history", async () =>
        {
            string dir=Dir(); var store=new LocalFileSaveStore(dir); bool failPage=true; int detailCalls=0,resumes=0;
            var api=new ServerApi
            {
                OnResume=()=>{resumes++;return Ok(Resume("A"));},
                OnPage=cursor=>cursor=="next" && failPage ? Task.FromResult(ApiResult<BookmarkPageDto>.Network("offline")) : Ok(
                    new BookmarkPageDto { Items=new List<BookmarkDetailDto> { new BookmarkDetailDto {
                        ClientBookmarkId=cursor==null ? "slot1":"slot2",ClientVersion=1,PlaythroughClientId="remote" } },
                        NextCursor=cursor==null ? "next":null }),
                OnBookmark=id=>{detailCalls++;return Ok(new BookmarkDetailDto { ClientBookmarkId=id,ClientVersion=1,
                    Snapshot=JToken.Parse(SaveJson.Serialize(Slot(id,"remote"))) });},
            };
            await new ServerRestore(api,Account(api,dir),store).RestoreAsync();
            Check(store.LoadRestoreProgress().BookmarkCursor=="next" && !store.LoadRestoreProgress().Completed,"partial not checkpointed");
            Check(store.ListPlaythroughIds().Count==1 && detailCalls==0,"full history fetched");
            Commit(store.Open("A"),7); failPage=false;
            var restart=new LocalFileSaveStore(dir); var restore=new ServerRestore(api,Account(api,dir),restart);
            await restore.RestoreAsync();
            Check(restart.LoadRestoreProgress().Completed && resumes==1 && restart.LoadBookmarks().Bookmarks.Count==2,"restore not resumed");
            Check(restart.LoadPlaythrough("A").Stats["score"]==7,"local progress overwritten");
            Check(restart.LoadBookmark("slot1")==null,"summary materialized body");
            await restore.HydrateBookmarkAsync("slot1"); await restore.HydrateBookmarkAsync("slot1");
            Check(detailCalls==1 && restart.LoadBookmark("slot1")!=null,"not one selected fetch");
        });
        await Test("Delayed resume cannot override explicit new game", async () =>
        {
            string dir=Dir(); var store=new LocalFileSaveStore(dir);
            var gate=new TaskCompletionSource<ApiResult<ResumeSaveDto>>();
            var api=new ServerApi { OnResume=()=>gate.Task,OnPage=c=>Ok(new BookmarkPageDto()) };
            var coordinator=new SaveCoordinator(store,null,restore:new ServerRestore(api,Account(api,dir),store));
            Task startup=coordinator.SyncPendingAsync(); await coordinator.PrepareNewPlaythroughAsync();
            coordinator.ReportSceneEntered(new SceneEntryReport("chapter",Ked.Progression.ProgressionState.CreateInitial(Array.Empty<Ked.Progression.StatDefinition>(),"scene1"),null,0));
            string active=store.ActiveId;
            gate.SetResult(ApiResult<ResumeSaveDto>.Success(200,Resume("remote"),""));
            await startup; Check(store.ActiveId==active,"restore stole active");
        });
        await Test("Continue is not blocked by slow bookmark list; deletion beats late hydration", async () =>
        {
            string dir=Dir(); var store=new LocalFileSaveStore(dir);
            var page=new TaskCompletionSource<ApiResult<BookmarkPageDto>>();
            var body=new TaskCompletionSource<ApiResult<BookmarkDetailDto>>();
            var api=new ServerApi { OnResume=()=>Ok(Resume("A")),OnPage=c=>page.Task,OnBookmark=id=>body.Task };
            var restore=new ServerRestore(api,Account(api,dir),store); var coordinator=new SaveCoordinator(store,null,restore:restore);
            Task startup=coordinator.SyncPendingAsync();
            Check(startup.IsCompleted && store.ActiveId=="A","manual list blocks continue");
            page.SetResult(ApiResult<BookmarkPageDto>.Success(200,new BookmarkPageDto { Items=new List<BookmarkDetailDto> {
                new BookmarkDetailDto { ClientBookmarkId="slot",ClientVersion=1,PlaythroughClientId="A" } } },""));
            await restore.RestoreBookmarkIndexAsync();
            Task<Bookmark> hydrate=restore.HydrateBookmarkAsync("slot"); coordinator.DeleteBookmark("slot");
            body.SetResult(ApiResult<BookmarkDetailDto>.Success(200,new BookmarkDetailDto { ClientVersion=1,
                Snapshot=JToken.Parse(SaveJson.Serialize(Slot("slot"))) },""));
            Check(await hydrate==null && store.LoadBookmarks().Bookmarks.Count==0,"deleted slot resurrected");
        });
    }

    private static async Task TestManualSlots()
    {
        await Test("Slot overwrite is atomic; summary reads write nothing", () => Run(() =>
        {
            string dir=Dir(); bool failIndex=false; int writes=0;
            var store=new LocalFileSaveStore(dir,(p,c)=>{ if(failIndex && Path.GetFileName(p)=="bookmarks.json") throw new IOException(); writes++; AtomicFile.WriteAllText(p,c); });
            store.SaveBookmarks(new BookmarkFile { Bookmarks=new List<Bookmark> { Slot("slot") } });
            writes=0; var summaries=store.LoadBookmarks(); store.ListPlaythroughSummaries(); store.LoadBookmark("slot");
            Check(writes==0 && summaries.Bookmarks[0].Checkpoint==null,"query wrote or loaded body");
            failIndex=true; Throws(()=>store.SaveBookmarks(new BookmarkFile { Bookmarks=new List<Bookmark> { Slot("slot","B",2) } }));
            Check(new LocalFileSaveStore(dir).LoadBookmark("slot").PlaythroughId=="A","failed overwrite lost old slot");
            failIndex=false; store.SaveBookmarks(new BookmarkFile { Bookmarks=new List<Bookmark> { Slot("slot","B",2) } });
            Check(store.LoadBookmark("slot").LocalVersion==2 && Directory.GetFiles(Path.Combine(dir,"bookmark-snapshots")).Length==1,"orphan payload retained");
            new SaveCoordinator(store,null).DeleteBookmark("slot");
            Check(Directory.GetFiles(Path.Combine(dir,"bookmark-snapshots")).Length==0 && store.LoadBookmarks().DeletedIds.Contains("slot"),"delete not durable");
        }));
        await Test("Loading a manual slot forks without changing its saved content", async () =>
        {
            var store=new LocalFileSaveStore(Dir()); var coordinator=new SaveCoordinator(store,null);
            await coordinator.PrepareNewPlaythroughAsync();
            coordinator.ReportSceneEntered(new SceneEntryReport("chapter",Ked.Progression.ProgressionState.CreateInitial(Array.Empty<Ked.Progression.StatDefinition>(),"scene1"),null,0));
            var bookmark=coordinator.CreateBookmark(Array.Empty<CommittedChoice>(),Array.Empty<VNChoiceRecord>(),new SaveLineTarget { LineId="L" },"first");
            string before=SaveJson.Serialize(store.LoadBookmark(bookmark.Id)); string source=store.ActiveId;
            await coordinator.ForkFromBookmark(coordinator.Bookmarks.Single());
            Commit(store.Open(store.ActiveId),8);
            Check(store.ActiveId!=source && SaveJson.Serialize(store.LoadBookmark(bookmark.Id))==before,"manual slot followed autosave");
            coordinator.ReportSceneEntered(new SceneEntryReport("chapter",Ked.Progression.ProgressionState.CreateInitial(Array.Empty<Ked.Progression.StatDefinition>(),"scene8"),null,0));
            coordinator.OverwriteBookmark(bookmark.Id,Array.Empty<CommittedChoice>(),Array.Empty<VNChoiceRecord>(),new SaveLineTarget { LineId="later" },"second");
            Check(coordinator.Bookmarks.Count==1 && store.LoadBookmark(bookmark.Id).LocalVersion==2 && store.LoadBookmark(bookmark.Id).Load.Target.LineId=="later","overwrite added another slot");
        });
        await Test("Late PUT never acknowledges overwritten slot; DELETE follows in-flight PUT", async () =>
        {
            string dir=Dir(); var store=new LocalFileSaveStore(dir); store.SaveBookmarks(new BookmarkFile { Bookmarks=new List<Bookmark> { Slot("slot") } });
            var gate=new TaskCompletionSource<ApiResult<BookmarkUpsertResponseDto>>(); var calls=new List<string>();
            var api=new ServerApi {
                OnVersions=id=>Ok(new List<ChapterVersionInfoDto> { new ChapterVersionInfoDto { Version=1,Checksum="e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855" } }),
                OnPutBookmark=r=>{calls.Add("put"+r.ClientVersion); if(r.ClientVersion==2) Check(r.BaseVersion==1,"wrong overwrite CAS"); return gate.Task;},
                OnDeleteBookmark=id=>{calls.Add("delete");return Ok<object>(null);} };
            var sync=new ServerBookmarkSync(api,Account(api,dir),new ChapterVersionResolver(api,new UnityEngine.TextAsset()),store);
            Task first=sync.PushAsync("slot");
            store.SaveBookmarks(new BookmarkFile { Bookmarks=new List<Bookmark> { Slot("slot","A",2) } });
            gate.SetResult(ApiResult<BookmarkUpsertResponseDto>.Success(200,new BookmarkUpsertResponseDto { UpdatedAt="now" },"")); await first;
            Check(store.LoadBookmarks().Bookmarks.Single().SyncedAtUtc==null,"old ACK marked new save synced");
            Check(store.LoadBookmark("slot").SyncedVersion==1,"old ACK did not advance CAS baseline");
            gate=new TaskCompletionSource<ApiResult<BookmarkUpsertResponseDto>>(); Task second=sync.PushAsync("slot");
            var coordinator=new SaveCoordinator(store,null,sync); coordinator.DeleteBookmark("slot");
            Check(!calls.Contains("delete"),"DELETE overtook PUT");
            gate.SetResult(ApiResult<BookmarkUpsertResponseDto>.Success(200,new BookmarkUpsertResponseDto { UpdatedAt="now" },""));
            await second; await sync.DeleteAsync("slot");
            Check(calls.SequenceEqual(new[]{"put1","put2","delete"}) && store.LoadBookmarks().Bookmarks.Count==0,"delete ordering wrong");
        });
    }
    private static async Task TestRetryAndRetention()
    {
        await Test("Transient backoff survives restart; permanent failure needs explicit retry", async () =>
        {
            string dir=Dir(); var store=new LocalFileSaveStore(dir); store.Create(Save("A")); DateTime now=DateTime.UtcNow;
            var transport=new Transport { Send=w=>Task.FromResult(ApiResult<SaveUploadResponseDto>.Network("offline")) };
            var worker=new ServerSyncSaveStore(store,transport,()=>now); await worker.TrySyncAsync(); await worker.TrySyncAsync();
            Check(transport.Sent.Count==1,"immediate retry loop");
            store=new LocalFileSaveStore(dir); worker=new ServerSyncSaveStore(store,transport,()=>now);
            await worker.TrySyncAsync(); Check(transport.Sent.Count==1,"restart erased backoff");
            now=now.AddSeconds(6); transport.Send=w=>Task.FromResult(ApiResult<SaveUploadResponseDto>.Failure(413,"TOO_LARGE",""));
            await worker.TrySyncAsync(); now=now.AddHours(1); await worker.TrySyncAsync();
            Check(transport.Sent.Count==2 && store.Open("A").Read().Sync.BlockedReason=="TOO_LARGE","permanent error retried");
            transport.Send=w=>Task.FromResult(Ack()); await worker.RetryAsync("A");
            Check(!store.Open("A").NeedsSync && store.Open("A").Read().Sync.BlockedReason==null,"explicit retry failed");
        });
        await Test("Retention keeps active/manual/pending/conflict and collects only unreferenced completed files", () => Run(() =>
        {
            var store=new LocalFileSaveStore(Dir());
            foreach(string id in new[]{"active","manual","old"}) store.ImportRestored(Save(id),1,1,1);
            store.SetActive("active"); store.Create(Save("pending")); var conflict=store.Create(Save("conflict")); conflict.MarkConflicted("now");
            store.SaveBookmarks(new BookmarkFile { Bookmarks=new List<Bookmark> { Slot("slot","manual") } });
            Check(store.CollectUnusedPlaythroughs(true)==1 && store.Open("old")==null,"wrong initial collection");
            Check(store.Open("manual")!=null && store.Open("pending")!=null && store.Open("conflict")!=null,"protected file collected");
            var coordinator=new SaveCoordinator(store,null); coordinator.DeleteBookmark("slot");
            Check(store.CollectUnusedPlaythroughs(true)==1 && store.Open("manual")==null,"deleted last slot kept source");
            Check(store.CollectUnusedPlaythroughs(false)==1 && store.Open("pending")==null && store.Open("active")!=null,"offline cleanup wrong");
        }));
        await Test("Explicit conflict fork releases source for collection; slot copy preserves original", async () =>
        {
            var store=new LocalFileSaveStore(Dir()); store.Create(Save("A")).MarkConflicted("now");
            var worker=new ServerSyncSaveStore(store,new Transport { Send=w=>Task.FromResult(Ack()) });
            await worker.ResolveConflictAsForkAsync("A");
            Check(store.Open("A").Read().ReleasedTo!=null && store.CollectUnusedPlaythroughs(true)==2,"resolved conflict leaked files");
            var original=Slot("slot"); original.SyncError="CONFLICT";
            store.SaveBookmarks(new BookmarkFile { Bookmarks=new List<Bookmark> { original } });
            var copy=await new SaveCoordinator(store,null).DuplicateBookmarkAsync("slot");
            Check(copy.Id!="slot" && copy.SyncedVersion==0 && copy.SyncError==null && store.LoadBookmark("slot").SyncError=="CONFLICT","copy changed original");
        });
        await Test("Work queued during resume publication drains and publishes the new active", async () =>
        {
            var store=new LocalFileSaveStore(Dir()); store.Create(Save("A")); store.SetActive("A");
            var gate=new TaskCompletionSource<bool>(); var published=new List<string>();
            var transport=new Transport { Send=w=>Task.FromResult(Ack()),Publish=(id,v)=>{published.Add(id);return id=="A"?gate.Task:Task.FromResult(true);} };
            var worker=new ServerSyncSaveStore(store,transport); Task drain=worker.TrySyncAsync();
            store.Create(Save("B")); store.SetActive("B"); _=worker.RequestSyncAsync("B"); gate.SetResult(true); await drain;
            Check(!store.Open("B").NeedsSync && published.SequenceEqual(new[]{"A","B"}),"queued work stranded or wrong resume");
        });
    }
}
