using System;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

[Serializable]
[CommandMenuHint(
    "Other", "Destroy Rig", Order = 900)]
public sealed class DestroyCommandSpec : CharRigCommandSpecBase
{
    [Tooltip("자동 생성 시 루트 오브젝트 이름.")]
    public string rigRootName = "CharacterRig";

    [Header("Role Prefix")]
    [Tooltip("켜면 roleKey로부터 자동으로 prefix를 생성합니다. 예: roleKey='seina' -> 'seina_'")]
    public bool autoRolePrefixFromRoleKey = true;

    [Tooltip("켜면 최종 prefix를 실제 타겟 이름에 적용합니다.")]
    public bool addRolePrefix = true;

    [Tooltip("수동 prefix. 비워두면 자동/없음 정책을 따릅니다. 예: 'seina_'")]
    public string rolePrefixOverride = "";

    /// <summary>
    /// 최종 prefix (override > auto > "")
    /// </summary>
    public string ResolvedRolePrefix
    {
        get
        {
            if (!addRolePrefix)
                return "";

            if (!string.IsNullOrEmpty(rolePrefixOverride))
                return rolePrefixOverride;

            if (!autoRolePrefixFromRoleKey)
                return "";

            if (string.IsNullOrEmpty(roleKey))
                return "";

            return roleKey.EndsWith("_", StringComparison.Ordinal)
                ? roleKey
                : $"{roleKey}_";
        }
    }

    public string ResolvedTargetName => $"{ResolvedRolePrefix}{rigRootName}";
}

public sealed class DestroyCommand : CommandBase
{
    private readonly DestroyCommandSpec _spec;

    public override bool WaitForCompletion => true;

    public DestroyCommand(DestroyCommandSpec spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        GameObject go = FindTarget();
        if (go == null)
        {
            yield break;
        }

        Debug.Log($"[DestroyCommand] Destroy '{_spec.ResolvedTargetName}'");
        Object.Destroy(go);

        // roleKey에 매핑된 refs를 비움(네 설계대로)
        scope.Refs[_spec.roleKey] = null;
        yield break;
    }

    private GameObject FindTarget()
    {
        string targetName = _spec.ResolvedTargetName;
        var go = GameObject.Find(targetName);
        return go != null ? go : null;
    }
}