using System;
using System.Collections;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Char Rig", "Register Character Response Binding", Order = -997,
    Sets = new[]
    {
        CommandMenuSets.BuildChar,
    },
    SetOrder = -979)]
public sealed class RegisterCharacterResponseBindingCommandSpec : CommandSpecBase
{
    [Header("Rig")]
    [Tooltip("CharacterRigRegistry에 등록된 slotKey / characterKey.")]
    public string targetKey;

    [Tooltip("CharacterRig가 shot intent에 반응하는 방식.")]
    public PresentationResponseProfile responseProfile = PresentationResponseProfile.CharacterSlot;
}

public sealed class RegisterCharacterResponseBindingCommand : CommandBase
{
    private readonly RegisterCharacterResponseBindingCommandSpec _spec;
    private readonly PresentationResponseRig _responseRig;

    public RegisterCharacterResponseBindingCommand(
        RegisterCharacterResponseBindingCommandSpec spec,
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

    private void Apply(CommandRunScope scope)
    {
        if (!scope.CharacterRigs.TryGetRig(_spec.targetKey, out CharacterRigRefs rigRefs))
            return;

        var target = new CharacterRigResponseTarget(rigRefs);

        _responseRig.RegisterRuntimeBinding(
            ResponseBindingKeys.CharacterRigFromSlotKey(_spec.targetKey),
            target,
            _spec.responseProfile);
    }
}