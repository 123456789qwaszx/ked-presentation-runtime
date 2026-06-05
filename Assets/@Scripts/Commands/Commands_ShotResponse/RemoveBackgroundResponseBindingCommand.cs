using System;
using System.Collections;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Background Rig", "Remove Background Response Binding", Order = -996,
    Sets = new[]
    {
        CommandMenuSets.SetupBackground,
    },
    SetOrder = -978)]
public sealed class RemoveBackgroundResponseBindingCommandSpec : CommandSpecBase
{
    [Header("Rig")]
    [Tooltip("BackgroundRigRegistry에 등록된 rigKey.")]
    public string rigKey;
}

public sealed class RemoveBackgroundResponseBindingCommand : CommandBase
{
    private readonly RemoveBackgroundResponseBindingCommandSpec _spec;
    private readonly PresentationResponseRig _responseRig;

    public override bool WaitForCompletion => true;

    public RemoveBackgroundResponseBindingCommand(
        RemoveBackgroundResponseBindingCommandSpec spec,
        PresentationResponseRig responseRig)
    {
        _spec = spec;
        _responseRig = responseRig;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        Apply(scope);
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope) => Apply(scope);
    protected override void OnRollbackSeek(CommandRunScope scope) => Apply(scope);

    private void Apply(CommandRunScope scope)
    {
        _responseRig.RemoveBinding(ResponseBindingKeys.BackgroundRigFromRigKey(_spec.rigKey));
    }
}