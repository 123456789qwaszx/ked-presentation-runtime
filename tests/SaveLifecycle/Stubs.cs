// 이 harness의 대역 범위는 Unity/Yarn 재생과 HTTP 경계뿐이다.
// Save, Coordinator, Analytics, Launcher는 실제 소스를 컴파일한다.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnityEngine
{
    public static class Time { public static float realtimeSinceStartup; }
    public static class Debug
    {
        public static readonly List<string> Errors = new();
        public static void Log(object value) { }
        public static void LogWarning(object value) { }
        public static void LogError(object value) { Errors.Add(value.ToString()); }
    }
    public sealed class TextAsset { public byte[] bytes = Array.Empty<byte>(); }
}
namespace Yarn.Unity { public sealed class DialogueRunner { public object YarnProject; } }
public struct YarnLineMeta { public string lineId, nodeName, rawText; }
public sealed class RollbackPoint { public int historyIndex; }
public static class ProgressionContentLoader
{
    public static Ked.Progression.ScenarioProgression LoadSingleChapter(UnityEngine.TextAsset asset)
    {
        var chapter = new Ked.Progression.ChapterProgression("chapter", "", "scene1", null,
            new[] { new Ked.Progression.EpisodeNode("scene1", "", "node") });
        return new Ked.Progression.ScenarioProgression("scenario", "", "chapter", new[] { chapter });
    }
}
public static class ProgressionContentPreflight
{
    public static bool CheckAndLog(Ked.Progression.ScenarioProgression scenario, object yarn) => true;
}
public sealed class ProgressionDriver
{
    public bool IsRunning;
    public IReadOnlyList<CommittedChoice> PendingPath = Array.Empty<CommittedChoice>();
    public Func<Task> OnStop;
    public int Starts;
    public Task RequestReplayAsync() => Task.CompletedTask;
    public async Task StopAsync() { if (OnStop != null) await OnStop(); IsRunning = false; }
    public Task RunAsync(object yarn, Ked.Progression.ChapterProgression chapter, Ked.Progression.ProgressionState state,
        YarnVariableSnapshot variables, IReadOnlyList<DialogueLogEntry> backlog, SavedLoadPlan plan)
    { Starts++; IsRunning = true; return Task.CompletedTask; }
}
namespace UnityEngine.Networking
{
    public sealed class UnityWebRequestAsyncOperation
    {
        public bool isDone = true;
        public event Action<UnityWebRequestAsyncOperation> completed { add { } remove { } }
    }
    public class UploadHandlerRaw { public UploadHandlerRaw(byte[] bytes) {} }
    public class DownloadHandlerBuffer { public string text = ""; }
    public sealed class UnityWebRequest : IDisposable
    {
        public enum Result { ConnectionError, DataProcessingError, Success, ProtocolError }
        public const string kHttpVerbPOST="POST", kHttpVerbPUT="PUT";
        public int timeout;
        public UploadHandlerRaw uploadHandler;
        public DownloadHandlerBuffer downloadHandler = new();
        public Result result=Result.ConnectionError;
        public long responseCode;
        public string error="offline";
        public UnityWebRequest(string url,string method) {}
        public static UnityWebRequest Get(string url) => new(url,"GET");
        public void SetRequestHeader(string key,string value) {}
        public UnityWebRequestAsyncOperation SendWebRequest() => new();
        public void Dispose() {}
    }
}
