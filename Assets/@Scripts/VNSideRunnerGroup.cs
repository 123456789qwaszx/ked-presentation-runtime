using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

public sealed class VNSideRunnerGroup
{
    private readonly List<string> _laneKeys = new List<string>();
    private readonly VNSideRunnerSyncHub _syncHub;
    private readonly VNTraceStream _trace;

    public VNSideRunnerGroup(VNSideRunnerSyncHub syncHub, VNTraceStream trace = null)
    {
        _syncHub = syncHub;
        _trace = trace;
    }

    public void RegisterLane(string laneKey, DialogueRunner runner)
    {
        if (_syncHub == null)
            return;

        _syncHub.RegisterLane(laneKey, runner);

        if (!_laneKeys.Contains(laneKey))
            _laneKeys.Add(laneKey);
    }

    public IEnumerator RestartLaneCoroutine(string laneKey, string nodeName)
    {
        if (string.IsNullOrEmpty(nodeName))
        {
            Debug.LogWarning($"[VNSideRunnerGroup] Cannot restart lane. nodeName is null or empty. lane='{laneKey}'");
            yield break;
        }

        if (_syncHub == null)
            yield break;

        DialogueRunner runner;
        if (!_syncHub.TryGetRunner(laneKey, out runner))
            yield break;

        _syncHub.ResetLaneForRestart(laneKey);

        if (runner.IsDialogueRunning)
        {
            Trace("StopLaneBeforeRestart", $"lane={laneKey}, runner={runner.name}, node={nodeName}");

            YarnTask stopTask = runner.Stop();

            while (!stopTask.IsCompletedSuccessfully())
                yield return null;
        }

        Trace("StartLane", $"lane={laneKey}, runner={runner.name}, node={nodeName}");

        YarnTask startTask = runner.StartDialogue(nodeName);

        while (!startTask.IsCompletedSuccessfully())
            yield return null;
    }

    public IEnumerator StopAllCoroutine()
    {
        if (_syncHub == null)
            yield break;

        _syncHub.ClearAllForSeekOrLoad();

        List<YarnTask> tasks = new List<YarnTask>();

        for (int i = 0; i < _laneKeys.Count; i++)
        {
            DialogueRunner runner;
            if (!_syncHub.TryGetRunner(_laneKeys[i], out runner))
                continue;

            if (runner != null && runner.IsDialogueRunning)
                tasks.Add(runner.Stop());
        }

        YarnTask all = YarnTask.WhenAll(tasks);

        while (!all.IsCompletedSuccessfully())
            yield return null;
    }

    private void Trace(string evt, string note = null)
    {
        if (_trace == null)
            return;

        _trace.Trace(nameof(VNSideRunnerGroup), evt, "sideRunnerGroup", note);
    }
}