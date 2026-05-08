using UnityEngine;

public sealed class VNSaveRuntimeState
{
    public bool IsRollbackSeeking { get; private set; }
    public bool IsLoadSeeking { get; private set; }

    public bool IsSkipping { get; private set; }
    public bool IsNodeBusy { get; private set; }

    public bool IsDialogueTransitioning { get; private set; }
    public bool IsLinePreparing { get; private set; }

    public bool IsModalOpen { get; private set; }

    public string DebugState
    {
        get
        {
            return
                $"rollback={IsRollbackSeeking}, " +
                $"load={IsLoadSeeking}, " +
                $"skip={IsSkipping}, " +
                $"nodeBusy={IsNodeBusy}, " +
                $"dialogueTransition={IsDialogueTransitioning}, " +
                $"linePreparing={IsLinePreparing}, " +
                $"modal={IsModalOpen}";
        }
    }

    public void SetRollbackSeeking(bool value)
    {
        IsRollbackSeeking = value;
    }

    public void SetLoadSeeking(bool value)
    {
        IsLoadSeeking = value;
    }

    public void SetSkipping(bool value)
    {
        IsSkipping = value;
    }

    public void SetNodeBusy(bool value)
    {
        IsNodeBusy = value;
    }

    public void SetDialogueTransitioning(bool value)
    {
        IsDialogueTransitioning = value;
    }

    public void SetLinePreparing(bool value)
    {
        IsLinePreparing = value;
    }

    public void SetModalOpen(bool value)
    {
        IsModalOpen = value;
    }

    public void ClearTransientStates()
    {
        IsRollbackSeeking = false;
        IsLoadSeeking = false;
        IsSkipping = false;
        IsNodeBusy = false;
        IsDialogueTransitioning = false;
        IsLinePreparing = false;
        IsModalOpen = false;
    }

    public void LogCurrentState(string prefix)
    {
        Debug.Log($"{prefix} {DebugState}");
    }
}