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
{ }

public sealed class SubPresentationAdvanceCommand : CommandBase
{
    private readonly VNSideRunnerSyncHub _syncHub;

    public SubPresentationAdvanceCommand(VNSideRunnerSyncHub syncHub)
    {
        _syncHub = syncHub;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        Apply();
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope) => Apply();

    private void Apply()
    {
        // This signal must be emitted only after the main line has been committed.
        //
        // Yarn command callbacks may run before VNLineEntryCommitter has updated the current YarnLineMeta.
        // Therefore, those callbacks only collect this command spec;
        // they must not advance the sub lane directly.
        //
        // The valid flow is:
        //   Yarn command callback
        //     -> collect SubPresentationAdvanceCommandSpec
        //   Main line entered
        //     -> CommitLineEntered updates current meta / backlog / rollback state
        //     -> PlayCollected runs this command
        //     -> DispatchPresentationAdvance
        _syncHub.DispatchPresentationAdvance();
    }
}