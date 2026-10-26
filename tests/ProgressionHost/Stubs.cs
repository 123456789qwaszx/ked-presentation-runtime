// 이 하네스는 Unity/Yarn에 매인 것만 대역으로 두고, SceneRunner와 그 협력자는 실제 소스를 컴파일한다.
using System.Collections.Generic;

namespace UnityEngine
{
    public static class Debug
    {
        public static readonly List<string> Logs = new();
        public static readonly List<string> Errors = new();

        public static void Log(object value) => Logs.Add(value?.ToString());
        public static void LogWarning(object value) => Logs.Add(value?.ToString());
        public static void LogError(object value) => Errors.Add(value?.ToString());
    }
}

// 진짜는 VNYarnLineBoundary.cs에 있고 Yarn.Unity를 참조한다. 모양만 같게 둔다.
public readonly struct YarnLineMeta
{
    public readonly string nodeName;
    public readonly string lineId;
    public readonly string charName;
    public readonly string rawText;

    public YarnLineMeta(string nodeName, string lineId, string charName, string rawText)
    {
        this.nodeName = nodeName;
        this.lineId = lineId;
        this.charName = charName;
        this.rawText = rawText;
    }
}

// 라인 시크 자체는 이 하네스의 관심사가 아니다.
// SceneRunner가 보는 면(IsSeekingActive · BeginLoadReplay · ClearSeek)만 흉내낸다.
//
// 진짜는 표적 라인에 닿으면 시크가 끝난다. 여기서는 "표적이 든 노드를 재생하면 끝난다"로 줄인다 —
// FakePresentation이 노드를 재생할 때 ReachNode를 부른다.
public sealed class ProgressionReplayState
{
    public bool IsSeekingActive { get; private set; }
    public string SeekUntilNode;

    public int BeginLoadReplayCount;
    public int ClearSeekCount;

    // 로드 시크. 표적 노드는 테스트가 SeekUntilNode로 미리 정한다.
    public void BeginLoadReplay()
    {
        BeginLoadReplayCount++;
        IsSeekingActive = true;
    }

    // 롤백 시크. 진짜로는 VNFeatureController가 VNLinePresentationState에 건다.
    public void BeginRollbackSeek(string untilNode)
    {
        IsSeekingActive = true;
        SeekUntilNode = untilNode;
    }

    public void ClearSeek()
    {
        ClearSeekCount++;
        IsSeekingActive = false;
        SeekUntilNode = null;
    }

    public void ReachNode(string nodeName)
    {
        if (IsSeekingActive && nodeName == SeekUntilNode)
        {
            IsSeekingActive = false;
            SeekUntilNode = null;
        }
    }
}
