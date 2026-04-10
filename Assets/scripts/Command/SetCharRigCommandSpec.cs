using System;
using System.Collections;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Char Rig", "@Set Character Rig", Order = -999,
    Sets = new[]
    {
        CommandMenuSets.BuildChar,
    }, SetOrder = -980)]
public sealed class SetCharRigCommandSpec : CommandSpecBase
{
    [Tooltip("있으면 이 프리팹을 인스턴스해서 Rig를 구성합니다. 없으면 자동 생성합니다.")]
    public GameObject rigPrefab;

    [Tooltip("Rig를 붙일 Slot")]
    public CharRigSlot parentSlot = CharRigSlot.CharacterStageSlot00;

    [Tooltip("자동 생성 시 루트 오브젝트 이름.")]
    public string rigRootName = "CharacterRig";

    [Tooltip("켜면 roleKey로부터 자동으로 prefix를 생성합니다. 예: roleKey='seina' -> 'seina_'")]
    public bool autoRolePrefixFromRoleKey = true;
    public bool addRolePrefix = true;
    
    // // 역할 기반 자동 prefix (기본 ON)
    // [Header("Role Prefix")]
    // // 수동 입력은 'Override' 체크 후에만 쓰기
    // [Tooltip("체크하면 아래 rolePrefixOverride를 사용합니다.")]
    // public bool overrideRolePrefix = false;
    //
    // [Tooltip("수동 prefix. 예: 'seina_' (Override 체크된 경우에만 적용)")]
    // public string rolePrefixOverride = "";
    //
    [Tooltip("Parent Slot에 동일한 이름의 Rig가 이미 있으면 파괴 후 새로 생성합니다.")]
    public bool destroyExistingRigWithSameName = true;
    
    [Tooltip("필수 노드가 없으면 예외를 던질지.")]
    public bool strict = true;


    // 최종 prefix (auto/override 반영)
    public string ResolvedRolePrefix
    {
        get
        {
            // if (overrideRolePrefix)
            //     return rolePrefixOverride ?? "";

            if (!autoRolePrefixFromRoleKey)
                return ""; // auto도 끄고 override도 아니면 prefix 없음

            if (string.IsNullOrEmpty(roleKey))
                return "";

            // 관례: roleKey + "_" (이미 _로 끝나면 중복 방지)
            return roleKey.EndsWith("_", StringComparison.Ordinal)
                ? roleKey
                : $"{roleKey}_";
        }
    }

// #if UNITY_EDITOR
//     // 인스펙터에서 값 바꿀 때 자동 정리
//     private void OnValidate()
//     {
//         // override prefix를 쓰면 끝에 '_'를 자동 보정하고 싶다면:
//         if (overrideRolePrefix && !string.IsNullOrEmpty(rolePrefixOverride))
//         {
//             if (!rolePrefixOverride.EndsWith("_", StringComparison.Ordinal))
//                 rolePrefixOverride += "_";
//         }
//     }
// #endif
}

public sealed class SetCharRigCommand : CommandBase
{
    private readonly CharacterRigAccess _rig;
    private readonly SetCharRigCommandSpec _spec;

    public override bool WaitForCompletion => true;

    public SetCharRigCommand(CharacterRigAccess rig, SetCharRigCommandSpec spec)
    {
        _rig = rig;
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (_spec.destroyExistingRigWithSameName)
        {
            TryDestroyExistingRig();
        }

        CharacterRigRefs refs = _rig.BindAndBuildRefs(_spec);
        scope.Refs[_spec.roleKey] = refs;
        yield break;
    }

    private void TryDestroyExistingRig()
    {
        RectTransform parent =
            _rig.ResolveParentSlot(_spec.parentSlot, _spec.strict);

        if (parent == null)
            return;

        string targetName = _spec.addRolePrefix
            ? _spec.ResolvedRolePrefix + _spec.rigRootName
            : _spec.rigRootName;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child.name != targetName)
                continue;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            else
#endif
                UnityEngine.Object.Destroy(child.gameObject);
        }
    }

}
