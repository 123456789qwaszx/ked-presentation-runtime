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
    private static void Commit(PlaythroughSession session, int scene) => session.Commit(Save(session.Id,scene));
    private static async Task Test(string name, Func<Task> test) { await test(); Passed.Add(name); Console.WriteLine("PASS " + name); }
    private static Task Run(Action action) { action(); return Task.CompletedTask; }
    private static Ked.Progression.ProgressionState State(string episode = "scene1") =>
        Ked.Progression.ProgressionState.CreateInitial(Array.Empty<Ked.Progression.StatDefinition>(), episode);
    private static SceneEntryReport Entry() => new SceneEntryReport("chapter", State(), null, 0);
    private static SceneCommitReport Completion() => new SceneCommitReport("chapter", Array.Empty<CommittedChoice>(),
        Array.Empty<VNChoiceRecord>(), Array.Empty<string>(), State("scene2"), null, Array.Empty<DialogueLogEntry>(), 0, false);
    private static Bookmark Slot(string id,string source="A",int version=1) => new Bookmark
    {
        Id=id,PlaythroughId=source,LocalVersion=version,ChapterId="chapter",Preview="line",Label="slot",
        Checkpoint=new SceneCheckpoint { ChapterId="chapter",EpisodeId="scene1" },
        Load=new SavedLoadPlan { Target=new SaveLineTarget { LineId="L" } },
    };
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
                Check(writes == 1 && a.Read().Snapshot.CurrentEpisodeId == "scene2", "not one commit");
                fail = true; Throws(() => Commit(a,3));
                Check(a.Read().Snapshot.CurrentEpisodeId == "scene2", "memory advanced");
                var disk = new LocalFileSaveStore(dir).Open("A").Read();
                Check(disk.LocalCommitVersion == 2 && disk.Snapshot.CurrentEpisodeId == "scene2", "disk advanced");
            }));
            await Test("Bookmark fork creates one self-contained file without source", async () =>
            {
                var store=new LocalFileSaveStore(Dir()); var coordinator=new SaveCoordinator(store);
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
            await Test("Completed resume starts a distinct playthrough", async () =>
            {
                var store=new LocalFileSaveStore(Dir()); var done=Save("A"); done.ChapterCompleted=true;
                store.Create(done); store.SetActive("A"); var coordinator=new SaveCoordinator(store);
                var launcher=new ProgressionLauncher(new ProgressionDriver(),new Yarn.Unity.DialogueRunner(),
                    new UnityEngine.TextAsset(),coordinator.LoadActiveResumePoint,coordinator.PrepareNewPlaythroughAsync);
                await launcher.LaunchAsync();
                Check(coordinator.PlaythroughId!="A" && store.LoadPlaythrough("A").ChapterCompleted,"completed timeline reused");
            });
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
            new SaveCoordinator(store).DeleteBookmark("slot");
            Check(Directory.GetFiles(Path.Combine(dir,"bookmark-snapshots")).Length==0 && store.LoadBookmarks().Bookmarks.Count == 0,"delete not durable");
        }));
            await Test("Loading a manual slot forks without changing its saved content", async () =>
        {
            var store=new LocalFileSaveStore(Dir()); var coordinator=new SaveCoordinator(store);
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
            await TestLocal();
            await TestAnalytics();
            Console.WriteLine($"{Passed.Count} tests passed.");
        }
        finally { if (Directory.Exists(Root)) Directory.Delete(Root,true); }
    }
    private static async Task TestLocal()
    {
        await Test("Offline new game preserves active until first successful write", async () =>
        {
            string dir=Dir(); bool fail=false;
            var store=new LocalFileSaveStore(dir,(p,c)=>{if(fail) throw new IOException(); AtomicFile.WriteAllText(p,c);});
            store.Create(Save("A")); store.SetActive("A"); var save=new SaveCoordinator(store);
            save.LoadActiveResumePoint(); await save.PrepareNewPlaythroughAsync(); string id=save.PlaythroughId;
            await save.PrepareNewPlaythroughAsync(); Check(save.PlaythroughId==id && store.ActiveId=="A","prepare lost active");
            fail=true; Throws(()=>save.ReportSceneEntered(Entry())); Check(store.ActiveId=="A","failed create lost active");
            fail=false; save.ReportSceneEntered(Entry()); Check(store.ActiveId==id,"new save not activated");
            var restarted=new SaveCoordinator(new LocalFileSaveStore(dir));
            Check(restarted.LoadActiveResumePoint().EpisodeId=="scene1","offline restart failed");
        });
        await Test("Failed active pointer write preserves old resume and permits retry", async () =>
        {
            string dir=Dir(); bool fail=false;
            var store=new LocalFileSaveStore(dir,(p,c)=>
            { if(fail && Path.GetFileName(p)=="active.json") throw new IOException(); AtomicFile.WriteAllText(p,c); });
            store.Create(Save("A")); store.SetActive("A"); var save=new SaveCoordinator(store);
            save.LoadActiveResumePoint(); await save.PrepareNewPlaythroughAsync(); string id=save.PlaythroughId;
            fail=true; Throws(()=>save.ReportSceneEntered(Entry()));
            Check(store.ActiveId=="A" && new LocalFileSaveStore(dir).LoadActive().PlaythroughId=="A","failed selection lost old resume");
            fail=false; save.ReportSceneEntered(Entry()); save.ReportSceneCommitted(Completion());
            Check(store.ActiveId==id && new LocalFileSaveStore(dir).LoadActive().CurrentEpisodeId=="scene2","selection retry failed");
        });
        await Test("Legacy v3 revision is ignored; snapshot survives next write", () => Run(() =>
        {
            string dir=Dir(); var legacy=JObject.FromObject(new {formatVersion=3,snapshot=JObject.Parse(SaveJson.Serialize(Save("A"))),
                localCommitVersion=7,syncedCommitVersion=6,sync=new {baseRevision=42},inFlight=new {id="old"}});
            string path=Path.Combine(dir,"playthroughs-v3","A.json"); AtomicFile.WriteAllText(path,legacy.ToString());
            var store=new LocalFileSaveStore(dir); Check(store.LoadPlaythrough("A").Stats["score"]==1,"legacy unreadable");
            Commit(store.Open("A"),2); var written=JObject.Parse(File.ReadAllText(path));
            Check(written["sync"]==null && written["inFlight"]==null && written["localCommitVersion"].Value<int>()==8,"legacy network state retained");
        }));
        await Test("Legacy interrupted transfer preserves destination before selecting it", () => Run(() =>
        {
            string dir=Dir(); var store=new LocalFileSaveStore(dir);store.Create(Save("A"));store.SetActive("A");
            var active=JObject.Parse(File.ReadAllText(Path.Combine(dir,"active.json")));
            AtomicFile.WriteAllText(Path.Combine(dir,"conflict-transfer.json"),SaveJson.Serialize(new {
                sourceId="A",destinationId="B",destination=new PlaythroughFile {Snapshot=Save("B"),LocalCommitVersion=1},
                wasActive=true,selectionVersion=active["selectionVersion"].Value<long>()}));
            var restart=new LocalFileSaveStore(dir);restart.Initialize();
            Check(restart.ActiveId=="B" && restart.LoadPlaythrough("A")!=null && restart.LoadPlaythrough("B")!=null,"legacy transfer lost data");
        }));
        await Test("Completed scene fork preserves replay target, path and inherited time", async () =>
        {
            var store=new LocalFileSaveStore(Dir());var file=Save("A");
            file.Scenes.Add(new SceneRecord { Checkpoint=new SceneCheckpoint {ChapterId="chapter",EpisodeId="scene1",PlaySecondsAtEntry=12},
                Path=new List<SavedChoice> {new SavedChoice {FromEpisodeId="scene1",OptionIndex=1}} });
            store.Create(file);store.SetActive("A");var save=new SaveCoordinator(store);save.LoadActiveResumePoint();
            await save.ForkFromScene(new SaveForkTarget(0,new SaveLineTarget {NodeName="node",LineId="L",Occurrence=1}));
            var fork=store.LoadActive();Check(fork.PlaythroughId!="A" && fork.PendingLoad.Target.LineId=="L"
                && fork.PendingLoad.Path.Single().OptionIndex==1 && fork.InheritedPlaySeconds==12,"fork state lost");
            Check(store.LoadPlaythrough("A").Scenes.Count==1,"source mutated");
        });
        await Test("Local retention protects active and manual slots without server ACK", () => Run(() =>
        {
            var store=new LocalFileSaveStore(Dir());store.Create(Save("active"));store.Create(Save("slotSource"));store.Create(Save("old"));
            store.SetActive("active");store.SaveBookmarks(new BookmarkFile {Bookmarks=new List<Bookmark>{Slot("slot","slotSource")}});
            Check(store.CollectUnusedPlaythroughs()==1 && store.LoadPlaythrough("old")==null,"unreferenced not collected");
            Check(store.LoadActive()!=null && store.LoadBookmark("slot")!=null,"protected data collected");
        }));
        await Test("Observer failure cannot cancel successful local save", () => Run(() =>
        {
            var store=new LocalFileSaveStore(Dir());var save=new SaveCoordinator(store);
            var reporter=new AnalyticsProgressionReporter(save,store,s=>throw new Exception("offline"));
            reporter.ReportSceneEntered(Entry());reporter.ReportSceneCommitted(Completion());
            Check(store.LoadActive().CurrentEpisodeId=="scene2","observer stopped local save");
        }));
        await Test("Failed local write is never reported to analytics", () => Run(() =>
        {
            bool fail=false;int observed=0;var store=new LocalFileSaveStore(Dir(),(p,c)=>{if(fail)throw new IOException();AtomicFile.WriteAllText(p,c);});
            var reporter=new AnalyticsProgressionReporter(new SaveCoordinator(store),store,s=>observed++);
            reporter.ReportSceneEntered(Entry());fail=true;Throws(()=>reporter.ReportSceneCommitted(Completion()));
            Check(observed==1 && store.LoadActive().Scenes.Count==0,"failed snapshot observed");
        }));
    }

    private sealed class Transport : IAnalyticsTransport
    {
        public Func<string,Task<AnalyticsApiResult<AnalyticsPlaythroughDto>>> Register;
        public Func<long,IReadOnlyList<AnalyticsChoiceItemDto>,Task<AnalyticsApiResult<bool>>> Replace;
        public int Calls;
        public readonly List<(long id,int option)> Sent=new();
        public Task<AnalyticsApiResult<AnalyticsPlaythroughDto>> CreateOrGetPlaythroughAsync(string chapter,string id)
        { Calls++;return Register?.Invoke(id) ?? Task.FromResult(AnalyticsApiResult<AnalyticsPlaythroughDto>.Success(201,new AnalyticsPlaythroughDto {ClientPlaythroughId=id,PlaythroughId=id=="A"?1:2},"")); }
        public Task<AnalyticsApiResult<bool>> ReplaceChoicesAsync(long id,IReadOnlyList<AnalyticsChoiceItemDto> choices)
        { Sent.Add((id,choices.Count==0?-1:choices[0].OptionIndex));return Replace?.Invoke(id,choices) ?? Task.FromResult(AnalyticsApiResult<bool>.Success(204,true,"")); }
    }
    private static LocalSaveFile Choice(string id,int option)
    {
        var save=Save(id);save.Scenes.Add(new SceneRecord {Path=new List<SavedChoice>{new SavedChoice {FromEpisodeId="EP01",OptionIndex=option}}});return save;
    }
    private static async Task TestAnalytics()
    {
        await Test("Failed A cannot overwrite a newer A or block B during backoff", async () =>
        {
            DateTime now=DateTime.UtcNow;var gate=new TaskCompletionSource<AnalyticsApiResult<bool>>();
            var transport=new Transport {Replace=(id,c)=>gate.Task};var sync=new AnalyticsSync(transport,clock:()=>now);
            sync.Observe(Choice("A",0));sync.Tick();sync.Observe(Choice("A",1));sync.Observe(Choice("B",2));
            gate.SetResult(AnalyticsApiResult<bool>.Network("offline"));await Task.Yield();
            transport.Replace=null;sync.Tick();Check(transport.Sent.Last().id==2,"B blocked by A backoff");
            sync.Tick();Check(transport.Sent.Count==2,"immediate retry loop");
            now=now.AddSeconds(5);sync.Tick();Check(transport.Sent.Last()==(1L,1),"new snapshot lost");
        });
        await Test("A registration completing after B observation retains both identities", async () =>
        {
            var gate=new TaskCompletionSource<AnalyticsApiResult<AnalyticsPlaythroughDto>>();var transport=new Transport {Register=id=>gate.Task};
            var sync=new AnalyticsSync(transport);sync.Observe(Choice("A",0));sync.Tick();sync.Observe(Choice("B",1));
            gate.SetResult(AnalyticsApiResult<AnalyticsPlaythroughDto>.Success(201,new AnalyticsPlaythroughDto {ClientPlaythroughId="A",PlaythroughId=1},""));await Task.Yield();
            transport.Register=null;sync.Tick();Check(transport.Sent.SequenceEqual(new[]{(1L,0),(2L,1)}),"identity crossed");
        });
        await Test("Immediate completion and newer snapshots do not strand the worker", () => Run(() =>
        {
            var transport=new Transport();var sync=new AnalyticsSync(transport);
            sync.Observe(Choice("A",0));sync.Tick();sync.Observe(Choice("A",1));sync.Tick();
            Check(transport.Sent.Count==2,"synchronous completion stranded work");
        }));
        await Test("Disposed analytics ignores an in-flight registration response", async () =>
        {
            var gate=new TaskCompletionSource<AnalyticsApiResult<AnalyticsPlaythroughDto>>();var transport=new Transport {Register=id=>gate.Task};
            var sync=new AnalyticsSync(transport);sync.Observe(Choice("A",0));sync.Tick();sync.Dispose();
            gate.SetResult(AnalyticsApiResult<AnalyticsPlaythroughDto>.Success(201,new AnalyticsPlaythroughDto {ClientPlaythroughId="A",PlaythroughId=1},""));await Task.Yield();
            sync.Tick();Check(transport.Sent.Count==0,"disposed owner sent choices");
        });
    }
}
