using System;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

[Serializable]
[CommandMenuHint(
    "Other", "Destroy Rig", Order = 900)]
public sealed class DestroyCommandSpec : CharacterRigCommandSpecBase
{
    [Tooltip("자동 생성 시 루트 오브젝트 이름.")]
    public string rigRootName = "CharacterRig";

    [Header("Role Prefix")]
    [Tooltip("켜면 targetKey에서 해석된 roleKey로부터 자동으로 prefix를 생성합니다. 예: roleKey='seina' -> 'seina_'")]
    public bool autoRolePrefixFromRoleKey = true;

    [Tooltip("켜면 최종 prefix를 실제 타겟 이름에 적용합니다.")]
    public bool addRolePrefix = true;

    [Tooltip("수동 prefix. 비워두면 자동/없음 정책을 따릅니다. 예: 'seina_'")]
    public string rolePrefixOverride = "";

    public string GetResolvedRolePrefix(string resolvedRoleKey)
    {
        if (!addRolePrefix)
            return "";

        if (!string.IsNullOrEmpty(rolePrefixOverride))
            return rolePrefixOverride;

        if (!autoRolePrefixFromRoleKey)
            return "";

        if (string.IsNullOrEmpty(resolvedRoleKey))
            return "";

        return resolvedRoleKey.EndsWith("_", StringComparison.Ordinal)
            ? resolvedRoleKey
            : $"{resolvedRoleKey}_";
    }

    public string GetResolvedTargetName(string resolvedRoleKey)
    {
        return $"{GetResolvedRolePrefix(resolvedRoleKey)}{rigRootName}";
    }
}

public sealed class DestroyCommand : CommandBase
{
    private readonly DestroyCommandSpec _spec;

    public override bool WaitForCompletion => _spec.wait;

    public DestroyCommand(DestroyCommandSpec spec)
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
        string resolvedRoleKey = CharacterRigTargetResolver.ResolveRoleKeyFromTargetKey(scope, _spec.targetKey);
        string targetName = _spec.GetResolvedTargetName(resolvedRoleKey);

        GameObject go = GameObject.Find(targetName);
        if (go == null)
            return;

        Debug.Log($"[DestroyCommand] Destroy '{targetName}'");

        Object.Destroy(go);

        scope.Refs[resolvedRoleKey] = null;
    }

}