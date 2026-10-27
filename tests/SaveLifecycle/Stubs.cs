// 이 하네스는 Unity/Yarn 재생만 대역으로 두고 저장·호스트 코드는 실제 소스를 컴파일한다.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace UnityEngine
{
    public static class Debug
    {
        public static readonly List<string> Warnings = new();
        public static readonly List<string> Errors = new();
        public static void Log(object value) { }
        public static void LogWarning(object value) { Warnings.Add(value.ToString()); }
        public static void LogError(object value) { Errors.Add(value.ToString()); }
        public static void LogException(Exception error) { Errors.Add(error.ToString()); }
    }
}

public struct YarnLineMeta { public string lineId, nodeName, rawText; }

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

// 무대는 이 하네스의 관심사가 아니다. 재개 시크가 걸렸는지와 재생이 언제 끝나는지만 본다.
// 진짜처럼 정지가 재생을 끝낸다(ScenePresentation.StopAsync).
internal sealed class FakeStage : IPlaybackStage
{
    private TaskCompletionSource<bool> _playing;

    public PresentationResumePoint Resumed;
    public int Resumes;
    public int Plays;

    // true면 재생이 정지될 때까지 끝나지 않는다. false면 노드가 곧바로 끝까지 간다.
    public bool BlockPlays;

    public bool IsSeeking { get; set; }

    public void BeginScene() => IsSeeking = false;

    public void BeginResume(PresentationResumePoint resume)
    {
        Resumes++;
        Resumed = resume;
        IsSeeking = true;
    }

    public void AbandonSeek() => IsSeeking = false;

    public Task PlayEpisodeAsync(string nodeName)
    {
        Interlocked.Increment(ref Plays);

        if (!BlockPlays)
            return Task.CompletedTask;

        var playing = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _playing = playing;
        return playing.Task;
    }

    public void PrepareReplay() { }

    public Task StopAsync()
    {
        TaskCompletionSource<bool> playing = _playing;
        _playing = null;
        playing?.TrySetResult(true);
        return Task.CompletedTask;
    }
}

// 연출 단독 회차의 끝 화면. 부른 횟수만 센다.
internal sealed class FakeEndView : IEpisodeEndView
{
    public int Shown;

    public void Show() => Shown++;
}

// 연출 기억과 그 뒤의 진짜 부품들. 단위를 열어 보면 재개 예약이 무엇이었는지 알 수 있다.
internal sealed class PresentationRig
{
    public readonly FakeStage Stage = new();
    public readonly BacklogRecorder Backlog = new();
    public readonly ChoiceHistory Choices = new();
    public readonly RollbackHistory Rollback = new();
    public readonly PlaybackUnit Unit;
    public readonly PlaybackMemory Memory;

    public PresentationRig()
    {
        Unit = new PlaybackUnit(Stage, Backlog, Rollback);
        Memory = new PlaybackMemory(Unit, Backlog, Choices, Rollback);
    }

    // 줄 하나가 보였다 — VNYarnLineBoundary가 하는 두 기록.
    public void Show(string lineId, string node = "node")
    {
        var meta = new YarnLineMeta { lineId = lineId, nodeName = node, rawText = lineId + " 대사" };
        Backlog.Record(meta);
        Rollback.AddRollbackPoint(meta);
    }

    // 단위를 열고, 그때 걸린 재개 시크를 돌려준다(없으면 null).
    public PresentationResumePoint OpenAndTakeResume()
    {
        Stage.Resumed = null;
        Unit.OpenAsync().GetAwaiter().GetResult();
        return Stage.Resumed;
    }
}
