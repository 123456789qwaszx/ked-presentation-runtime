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
    private readonly DialogueAdvanceDispatcher _dispatcher;

    public override bool WaitForCompletion => false;

    public SubPresentationAdvanceCommand(DialogueAdvanceDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
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
        _dispatcher.DispatchSubAdvance();
    }
}