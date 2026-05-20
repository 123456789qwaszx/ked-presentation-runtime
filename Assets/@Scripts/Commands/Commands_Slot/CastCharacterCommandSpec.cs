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
    [Header("Slot / Role")]
    [Tooltip("캐릭터를 바인딩할 slotKey / roleKey.")]
    public string slotKey;

    [Header("Character Identity")]
    [Tooltip("이 slotKey에 바인딩할 캐릭터 키.")]
    public string characterKey;

    [Tooltip("의상/변형 키. 예: a, b. 비우면 emotion command에서 기본 a로 처리합니다.")]
    public string variantKey = "";
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
        string slotKey = _spec.slotKey;
        string characterKey = _spec.characterKey;
        string variantKey = _spec.variantKey;

        if (!scope.Refs.TryGetCharRigRefs(slotKey, out CharacterRigRefs rig) || rig == null)
        {
            throw new InvalidOperationException(
                $"[CastCharacterCommand] Rig is not bound for slotKey='{slotKey}'. Call <<slot>> before <<cast>>.");
        }

        scope.CastRegistry.CastCharRig(slotKey, characterKey, variantKey);
    }
}