using System;
using System.Collections;
using UnityEngine;

[Serializable]
public sealed class SetPresentationActorAliasCommandSpec : CommandSpecBase
{
    [Header("Alias")]
    public string aliasSymbol = "@";

    [Header("Target")]
    public string targetKey;
}

public sealed class SetPresentationActorAliasCommand : CommandBase
{
    private readonly SetPresentationActorAliasCommandSpec _spec;

    protected override SkipPolicy SkipPolicy => SkipPolicy.ExecuteEvenIfSkipping;

    public SetPresentationActorAliasCommand(
        SetPresentationActorAliasCommandSpec spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        scope.CharacterTargetAliases.Register(
            _spec.aliasSymbol,
            _spec.targetKey);
        
        yield break;
    }
}
