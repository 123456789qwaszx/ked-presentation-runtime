using System;
using System.Collections;

[Serializable]
[CommandMenuHint(
    "Sub Presentation", "Advance Sub Presentation", Order = -909,
    Sets = new[]
    {
        CommandMenuSets.Presentation,
    },
    SetOrder = -909)]
public sealed class SubPresentationAdvanceCommandSpec : CommandSpecBase
{
    public string laneKey = VNSideRunnerLaneKeys.Presentation;
}

public sealed class SubPresentationAdvanceCommand : CommandBase
{
    private readonly SubPresentationAdvanceCommandSpec _spec;
    private readonly VNSideRunnerSyncHub _syncHub;

    public override bool WaitForCompletion => false;

    public SubPresentationAdvanceCommand(
        SubPresentationAdvanceCommandSpec spec,
        VNSideRunnerSyncHub syncHub)
    {
        _spec = spec;
        _syncHub = syncHub;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        Apply();
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope) => Apply();
    protected override void OnRollbackSeek(CommandRunScope scope) => Apply();

    private void Apply()
    {
        if (_syncHub == null)
            return;

        _syncHub.DispatchLaneAdvance(_spec.laneKey);
    }
}