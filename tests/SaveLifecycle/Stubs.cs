// 이 harness는 Unity/Yarn 재생만 대역으로 두고 저장 코드는 실제 소스를 컴파일한다.
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
    public void Start(object yarn, Ked.Progression.ChapterProgression chapter, Ked.Progression.ProgressionState state,
        YarnVariableSnapshot variables, IReadOnlyList<DialogueLogEntry> backlog, SavedLoadPlan plan)
    { Starts++; IsRunning = true; }
}
