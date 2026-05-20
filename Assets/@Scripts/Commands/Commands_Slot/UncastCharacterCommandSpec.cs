using System;
using System.Collections;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Char Rig", "@Uncast Character", Order = -997,
    Sets = new[]
    {
        CommandMenuSets.BuildChar,
    },
    SetOrder = -978)]
public sealed class UncastCharacterCommandSpec : CharacterRigCommandSpecBase
{ }

public sealed class UncastCharacterCommand : CommandBase
{
    private readonly UncastCharacterCommandSpec _spec;

    protected override SkipPolicy SkipPolicy => SkipPolicy.ExecuteEvenIfSkipping;

    public UncastCharacterCommand(UncastCharacterCommandSpec spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        Apply(scope);
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        Apply(scope);
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        OnSkip(scope);
    }

    private void Apply(CommandRunScope scope)
    {
        string roleKey = 
            CharacterRigTargetResolver.ResolveSlotKeyFromTargetKey(scope, _spec.targetKey);

        scope.CastRegistry.UncastCharRig(roleKey);
    }
}