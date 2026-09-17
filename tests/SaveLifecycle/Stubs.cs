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
    public static Ked.Progression.ScenarioDefinition LoadSingleChapter(UnityEngine.TextAsset asset)
    {
        var chapter = new Ked.Progression.ChapterDefinition("chapter", "", "scene1", null,
            new[] { new Ked.Progression.EpisodeNode("scene1", "", "node") });
        return new Ked.Progression.ScenarioDefinition("scenario", "", "chapter", new[] { chapter });
    }
}
public static class ProgressionContentPreflight
{
    public static bool CheckAndLog(Ked.Progression.ScenarioDefinition scenario, object yarn) => true;
}
public sealed class ProgressionDriver
{
    public bool IsRunning;
    public IReadOnlyList<Ked.Progression.CommittedChoice> PendingPath = Array.Empty<Ked.Progression.CommittedChoice>();
    public Func<Task> OnStop;
    public int Starts;
    public Task RequestReplayAsync() => Task.CompletedTask;
    public async Task StopAsync() { if (OnStop != null) await OnStop(); IsRunning = false; }
    public void Start(Ked.Progression.ChapterDefinition chapter, Ked.Progression.ChapterState state,
        IReadOnlyList<Ked.Progression.ScenePathStep> restorePath)
    { Starts++; IsRunning = true; LastRestorePath = restorePath; }

    // 로드 계획이 진행 좌표로 잘려 들어왔는지 본다. null과 빈 목록은 다른 뜻이다.
    public IReadOnlyList<Ked.Progression.ScenePathStep> LastRestorePath;
}

// 라인 시크 복원은 이 harness의 관심사가 아니다.
// Launcher가 실행 전에 staging한다는 것만 지킨다.
public sealed class ProgressionReplayState
{
    public int Stages;
    public SaveLineTarget StagedTarget;
    public void Stage(IReadOnlyList<VNChoiceRecord> yarnChoices, SaveLineTarget target)
    { Stages++; StagedTarget = target; }
}
