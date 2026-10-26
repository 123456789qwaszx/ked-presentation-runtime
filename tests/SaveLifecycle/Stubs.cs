// 이 하네스는 Unity/Yarn 재생만 대역으로 두고 저장·호스트 코드는 실제 소스를 컴파일한다.
using System;
using System.Collections.Generic;
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
    }
}

public struct YarnLineMeta { public string lineId, nodeName, rawText; }
public sealed class RollbackPoint { public int historyIndex; }

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

// 라인 시크 복원은 이 하네스의 관심사가 아니다.
// 런처가 실행 전에 되읽은 재생 계획을 올려 둔다는 것만 지킨다.
public sealed class ProgressionReplayState
{
    public int PreparedLoads;
    public PresentationLoadPlan StagedPlan;

    public void PrepareLoad(PresentationLoadPlan plan)
    {
        PreparedLoads++;
        StagedPlan = plan;
    }
}
