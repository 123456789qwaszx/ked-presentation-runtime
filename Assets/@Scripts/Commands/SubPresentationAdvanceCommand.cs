using System;
using System.Collections;
using UnityEngine;

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
    private readonly SubPresentationAdvanceCommandSpec _spec;

    public override bool WaitForCompletion => false;

    public SubPresentationAdvanceCommand(
        DialogueAdvanceDispatcher dispatcher,
        SubPresentationAdvanceCommandSpec spec)
    {
        _dispatcher = dispatcher;
        _spec = spec;
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
        if (_dispatcher == null)
        {
            Debug.LogWarning("[SubPresentationAdvanceCommand] dispatcher is null.");
            return;
        }

        _dispatcher.DispatchSubPresentationAdvance();
    }
}