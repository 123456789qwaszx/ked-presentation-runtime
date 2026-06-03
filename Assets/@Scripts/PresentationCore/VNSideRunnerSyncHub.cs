using System;
using System.Collections;
using System.Collections.Generic;
using Yarn.Unity;

public static class VNSideRunnerLaneKeys
{
    public const string Presentation = "presentation";
    public const string Camera = "camera";
    public const string Option = "option";
    public const string Choice = "choice";
    public const string Data = "data";
}

public sealed class VNSideRunnerSyncHub
{
    private sealed class LaneState
    {
        public readonly DialogueRunner Runner;
        public int PendingAdvanceCount;
        public bool IsReadyForAdvance;

        // --- 자동 진행 제어 상태 ---
        public int HoldRemaining;                      // 남은 hold 라인 수
        public int ExtraAdvance;                        // 이번 라인에 추가 진행할 수
        public bool SuppressFirstAutoAdvance = true;    // 정책: 시작 첫 라인 suppress (기본 on)
        public bool SuppressNextAutoAdvance;            // 라이브 1회성 플래그

        public LaneState(DialogueRunner runner)
        {
            Runner = runner;
        }

        public void Reset()
        {
            PendingAdvanceCount = 0;
            IsReadyForAdvance = false;
            HoldRemaining = 0;
            ExtraAdvance = 0;
            SuppressNextAutoAdvance = SuppressFirstAutoAdvance;
        }
    }

    private readonly Dictionary<string, LaneState> _lanes = new(StringComparer.Ordinal);

    public event Action<string> LaneReady;

    public bool RegisterPresentationLane(DialogueRunner runner) => RegisterLane(VNSideRunnerLaneKeys.Presentation, runner);
    public IEnumerator StartPresentationLaneCoroutine(string nodeName) => StartLaneCoroutine(VNSideRunnerLaneKeys.Presentation, nodeName);
    public IEnumerator StopPresentationLaneCoroutine() => StopLaneCoroutine(VNSideRunnerLaneKeys.Presentation);
    public YarnTask WaitUntilPresentationLaneReadyAsync() => WaitUntilLaneReadyAsync(VNSideRunnerLaneKeys.Presentation);
    public void DispatchPresentationAdvance() => DispatchLaneAdvance(VNSideRunnerLaneKeys.Presentation);
    public void NotifyPresentationLaneReady() => NotifyLaneReady(VNSideRunnerLaneKeys.Presentation);
    public void NotifyPresentationLaneNotReady() => NotifyLaneNotReady(VNSideRunnerLaneKeys.Presentation);

    // 정방향: suppress/hold/extra 적용한 advance 횟수
    public int ConsumePresentationAutoAdvanceCount() => ConsumeAutoAdvanceCount(VNSideRunnerLaneKeys.Presentation);
    // 시크(롤백/로드): base 재동기화 횟수. suppress만 존중, hold/extra는 의도적으로 무시.
    public int ConsumePresentationSeekResyncCount() => ConsumeSeekResyncCount(VNSideRunnerLaneKeys.Presentation);

    // 제어 커맨드 (override = 마지막 값으로 덮어씀)
    public void HoldPresentation(int lines)         => SetHold(VNSideRunnerLaneKeys.Presentation, lines);
    public void AdvancePresentationExtra(int steps) => SetExtraAdvance(VNSideRunnerLaneKeys.Presentation, steps);
    public void SetPresentationSuppressFirstAutoAdvance(bool suppress)
        => SetSuppressFirst(VNSideRunnerLaneKeys.Presentation, suppress);

    public void ClearAllForSeekOrLoad()
    {
        foreach (LaneState lane in _lanes.Values)
            lane.Reset();
    }

    private bool RegisterLane(string laneKey, DialogueRunner runner)
    {
        if (string.IsNullOrEmpty(laneKey) || runner == null)
            return false;
        if (_lanes.ContainsKey(laneKey))
            return true;
        _lanes.Add(laneKey, new LaneState(runner));
        return true;
    }

    private IEnumerator StartLaneCoroutine(string laneKey, string nodeName)
    {
        LaneState lane = GetLane(laneKey);
        if (lane == null)
            yield break;

        lane.Reset();   // suppress 정책 적용 + hold/extra 클리어

        YarnTask startTask = lane.Runner.StartDialogue(nodeName);
        while (!startTask.IsCompletedSuccessfully())
            yield return null;
    }

    private IEnumerator StopLaneCoroutine(string laneKey)
    {
        LaneState lane = GetLane(laneKey);
        if (lane == null)
            yield break;

        if (lane.Runner != null && lane.Runner.IsDialogueRunning)
            lane.Runner.Stop();   // ※ Yarn 버전별 API 확인

        lane.Reset();
    }

    private async YarnTask WaitUntilLaneReadyAsync(string laneKey)
    {
        LaneState lane = GetLane(laneKey);
        if (lane == null)
            return;

        // 레인이 실행 중이 아니면 기다릴 대상이 없음 → 즉시 반환 (롤백/로드 hang 방지)
        if (lane.Runner == null || !lane.Runner.IsDialogueRunning)
            return;

        if (lane.IsReadyForAdvance)
            return;

        bool ready = false;
        void OnLaneReady(string readyLaneKey)
        {
            if (string.Equals(readyLaneKey, laneKey, StringComparison.Ordinal))
                ready = true;
        }

        LaneReady += OnLaneReady;
        while (!ready)
            await YarnTask.Yield();
        LaneReady -= OnLaneReady;
    }

    private void DispatchLaneAdvance(string laneKey)
    {
        LaneState lane = GetLane(laneKey);
        if (lane == null)
            return;
        lane.PendingAdvanceCount++;
        TryFlush(lane);
    }

    private void NotifyLaneReady(string laneKey)
    {
        LaneState lane = GetLane(laneKey);
        if (lane == null)
            return;
        lane.IsReadyForAdvance = true;
        LaneReady?.Invoke(laneKey);
        TryFlush(lane);
    }

    private void NotifyLaneNotReady(string laneKey)
    {
        LaneState lane = GetLane(laneKey);
        if (lane == null)
            return;
        lane.IsReadyForAdvance = false;
    }

    private bool TryFlush(LaneState lane)
    {
        if (lane.PendingAdvanceCount <= 0)
            return false;
        if (!lane.IsReadyForAdvance)
            return false;
        if (lane.Runner == null || !lane.Runner.IsDialogueRunning)
            return false;

        lane.PendingAdvanceCount--;
        lane.IsReadyForAdvance = false;
        lane.Runner.RequestNextLine();
        return true;
    }

    private int ConsumeAutoAdvanceCount(string laneKey)
    {
        LaneState lane = GetLane(laneKey);
        if (lane == null || lane.Runner == null || !lane.Runner.IsDialogueRunning)
            return 0;

        int baseStep;
        if (lane.SuppressNextAutoAdvance)
        {
            lane.SuppressNextAutoAdvance = false;
            baseStep = 0;
        }
        else if (lane.HoldRemaining > 0)
        {
            lane.HoldRemaining--;
            baseStep = 0;
        }
        else
        {
            baseStep = 1;
        }

        int extra = lane.ExtraAdvance;
        lane.ExtraAdvance = 0;
        return baseStep + extra;
    }

    private int ConsumeSeekResyncCount(string laneKey)
    {
        LaneState lane = GetLane(laneKey);
        if (lane == null || lane.Runner == null || !lane.Runner.IsDialogueRunning)
            return 0;

        // 노드 시작 정렬을 정방향과 맞추기 위해 suppress만 존중. hold/extra는 무시(아래 한계 메모 참고).
        if (lane.SuppressNextAutoAdvance)
        {
            lane.SuppressNextAutoAdvance = false;
            return 0;
        }
        return 1;
    }

    private void SetHold(string laneKey, int lines)
    {
        LaneState lane = GetLane(laneKey);
        if (lane != null) lane.HoldRemaining = Math.Max(0, lines);
    }

    private void SetExtraAdvance(string laneKey, int steps)
    {
        LaneState lane = GetLane(laneKey);
        if (lane != null) lane.ExtraAdvance = Math.Max(0, steps);
    }

    private void SetSuppressFirst(string laneKey, bool suppress)
    {
        LaneState lane = GetLane(laneKey);
        if (lane != null) lane.SuppressFirstAutoAdvance = suppress;   // 다음 sub_table(Reset) 때 적용
    }

    private LaneState GetLane(string laneKey)
    {
        _lanes.TryGetValue(laneKey, out LaneState lane);
        return lane;
    }
}