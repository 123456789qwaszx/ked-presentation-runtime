using System;
using System.Collections;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Char Rig", "Remove Character Response Binding", Order = -996,
    Sets = new[]
    {
        CommandMenuSets.BuildChar,
    },
    SetOrder = -978)]
public sealed class RemoveCharacterResponseBindingCommandSpec : CommandSpecBase
{
    [Header("Rig")]
    [Tooltip("CharacterRigRegistry에 등록된 slotKey / characterKey.")]
    public string targetKey;
}

public sealed class RemoveCharacterResponseBindingCommand : CommandBase
{
    private readonly RemoveCharacterResponseBindingCommandSpec _spec;
    private readonly PresentationResponseRig _responseRig;

    public RemoveCharacterResponseBindingCommand(
        RemoveCharacterResponseBindingCommandSpec spec,
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
    protected override void OnRollbackSeek(CommandRunScope scope) => OnSkip(scope);

    private void Apply(CommandRunScope scope)
    {
        string resolvedSlotKey = CharacterRigTargetResolver.ResolveRigKeyByPolicy(scope, _spec.targetKey);

        _responseRig.RemoveBinding(ResponseBindingKeys.CharacterRigFromSlotKey(resolvedSlotKey));
    }
}