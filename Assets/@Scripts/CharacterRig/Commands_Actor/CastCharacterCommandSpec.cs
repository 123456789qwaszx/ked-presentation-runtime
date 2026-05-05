using System;
using System.Collections;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Char Rig", "@Cast Character", Order = -998,
    Sets = new[]
    {
        CommandMenuSets.BuildChar,
    },
    SetOrder = -979)]
public sealed class CastCharacterCommandSpec : CommandSpecBase
{
    [Header("Character Identity")]
    [Tooltip("이 roleKey 슬롯에 바인딩할 캐릭터 키.")]
    public string characterKey;

    [Tooltip("의상/변형 키. 예: a, b. 비우면 emotion command에서 기본 a로 처리합니다.")]
    public string variantKey = "";

    [Header("Validation")]
    [Tooltip("켜면 이 roleKey에 해당하는 Rig가 이미 존재해야 합니다. 보통 <<slot>> 이후에 <<cast>>를 호출할 때 사용합니다.")]
    public bool requireExistingRig = true;

    [Tooltip("켜면 잘못된 입력/상태에서 로그를 남깁니다.")]
    public bool strict = true;
}

public sealed class CastCharacterCommand : CommandBase
{
    private readonly CastCharacterCommandSpec _spec;

    protected override SkipPolicy SkipPolicy => SkipPolicy.ExecuteEvenIfSkipping;

    public CastCharacterCommand(CastCharacterCommandSpec spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        ApplyBinding(scope);
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        ApplyBinding(scope);
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        OnSkip(scope);
    }

    private void ApplyBinding(CommandRunScope scope)
    {
        if (scope == null)
            return;

        string roleKey = SafeTrim(_spec.roleKey);
        string characterKey = SafeTrim(_spec.characterKey);
        string variantKey = SafeTrim(_spec.variantKey);

        if (string.IsNullOrEmpty(roleKey))
        {
            if (_spec.strict)
                Debug.LogError("[CastCharacterCommand] roleKey is null or empty.");
            return;
        }

        if (string.IsNullOrEmpty(characterKey))
        {
            if (_spec.strict)
                Debug.LogError($"[CastCharacterCommand] characterKey is null or empty. roleKey={roleKey}");
            return;
        }

        if (_spec.requireExistingRig &&
            !scope.Refs.TryGetCharRigRefs(roleKey, out CharacterRigRefs rig))
        {
            if (_spec.strict)
                Debug.LogError($"[CastCharacterCommand] Rig is not bound for roleKey='{roleKey}'. Call <<slot>> before <<cast>>.");
            return;
        }

        scope.CastRegistry.Cast(roleKey, characterKey, variantKey);
    }

    private static string SafeTrim(string s)
    {
        return string.IsNullOrEmpty(s) ? "" : s.Trim();
    }
}