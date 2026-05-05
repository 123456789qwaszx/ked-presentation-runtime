using System;
using UnityEngine;
using System.Collections;

[Serializable]
[CommandMenuHint(
    "Char Rig", "@Uncast Character", Order = -997,
    Sets = new[]
    {
        CommandMenuSets.BuildChar,
    },
    SetOrder = -978)]
public sealed class UncastCharacterCommandSpec : CommandSpecBase
{
    [Header("Validation")]
    [Tooltip("켜면 잘못된 입력/상태에서 로그를 남깁니다.")]
    public bool strict = true;
}


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

    protected override void OnRollbackSeek(CommandRunScope scope) => OnSkip(scope);
    
    private void Apply(CommandRunScope scope)
    {
        if (scope == null)
            return;

        string roleKey = SafeTrim(_spec.roleKey);

        if (string.IsNullOrEmpty(roleKey))
        {
            if (_spec.strict)
                Debug.LogError("[UncastCharacterCommand] roleKey is null or empty.");
            return;
        }

        scope.CastRegistry.UncastRole(roleKey);
    }

    private static string SafeTrim(string s)
    {
        return string.IsNullOrEmpty(s) ? string.Empty : s.Trim();
    }
}