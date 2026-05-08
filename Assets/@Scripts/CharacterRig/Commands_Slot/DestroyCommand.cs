using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
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

    [Header("Destroy")]
    [Tooltip("Destroy 전에 대상 Rig 하위 Tween을 정리합니다.")]
    public bool killTween = true;

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
        string resolvedRoleKey =
            CharacterRigTargetResolver.ResolveRoleKeyFromTargetKey(scope, _spec.targetKey);

        string targetName = _spec.GetResolvedTargetName(resolvedRoleKey);

        GameObject go = GameObject.Find(targetName);
        if (go == null)
            return;

        //Debug.Log($"[DestroyCommand] Destroy '{targetName}'");

        if (_spec.killTween)
            KillTweenBeforeDestroy(go.transform, resolvedRoleKey);

        Object.Destroy(go);

        if (!string.IsNullOrEmpty(resolvedRoleKey))
            scope.Refs[resolvedRoleKey] = null;
    }

    private static void KillTweenBeforeDestroy(Transform root, string resolvedRoleKey)
    {
        if (root == null)
            return;
        
        DOTween.Kill($"CharPortraitWipe:{resolvedRoleKey}", false);
        KillTweenOnHierarchy(root);
    }

    private static void KillTweenOnHierarchy(Transform root)
    {
        if (root == null)
            return;

        RectTransform[] rects = root.GetComponentsInChildren<RectTransform>(true);
        for (int i = 0; i < rects.Length; i++)
        {
            if (rects[i] != null)
                rects[i].DOKill(false);
        }

        CanvasGroup[] canvasGroups = root.GetComponentsInChildren<CanvasGroup>(true);
        for (int i = 0; i < canvasGroups.Length; i++)
        {
            if (canvasGroups[i] != null)
                canvasGroups[i].DOKill(false);
        }

        Graphic[] graphics = root.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
                graphics[i].DOKill(false);
        }

        DOTween.Kill(root, false);
        DOTween.Kill(root.gameObject, false);
    }
}