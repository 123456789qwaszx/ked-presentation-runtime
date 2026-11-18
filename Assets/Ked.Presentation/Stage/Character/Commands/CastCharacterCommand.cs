using System;
using System.Collections;
using UnityEngine;

[Serializable]
public sealed class CastCharacterCommandSpec : CommandSpecBase
{
    [Header("Slot / Role")]
    [Tooltip("캐릭터를 바인딩할 역할(슬롯) 키.")]
    public string roleKey;

    [Header("Character Identity")]
    [Tooltip("이 역할에 바인딩할 캐릭터 키.")]
    public string characterKey;

    [Tooltip("슬롯만 묶는다 — 이 캐릭터 키로 부르는 명령은 이 슬롯으로 오지 않는다. " +
             "대본 어휘가 아니다 — 파서가 portrait를 풀 때만 켠다(같은 캐릭터가 무대에도 설 수 있다).")]
    public bool slotOnly;
}
public sealed class CastCharacterCommand : CommandBase
{
    private readonly CastCharacterCommandSpec _spec;
    protected override SkipPolicy SkipPolicy => SkipPolicy.ExecuteEvenIfSkipping;

    private readonly PresentationStage _stage;

    public CastCharacterCommand(PresentationStage stage, CastCharacterCommandSpec spec)
    {
        _stage = stage;
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        _stage.castRegistry.CastCharRig(_spec.roleKey, _spec.characterKey, claimCharacterKey: !_spec.slotOnly);
        yield break;
    }
}