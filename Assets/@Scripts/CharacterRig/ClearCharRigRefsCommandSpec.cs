using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[Serializable]
[CommandMenuHint(
    "Char Rig", "Clear Character Rig Refs", Order = -998,
    Sets = new[]
    {
        CommandMenuSets.BuildChar,
    },
    SetOrder = -970)]
public sealed class ClearCharRigRefsCommandSpec : CommandSpecBase
{
    [Header("Clear")]
    [Tooltip("켜면 scope.Refs에 등록된 CharacterRigRefs 항목을 제거합니다. 끄면 null로만 세팅합니다.")]
    public bool removeKeys = true;

    [Tooltip("켜면 Refs에 연결된 Rig GameObject도 Destroy합니다.")]
    public bool destroyRigObjects = false;

    [Tooltip("Destroy 전에 대상 Rig 하위 Tween을 정리합니다.")]
    public bool killTweensBeforeDestroy = true;

    [Header("Filter")]
    [Tooltip("비워두면 모든 CharacterRigRefs를 정리합니다. 값이 있으면 해당 roleKey만 정리합니다.")]
    public string[] onlyRoleKeys;
}

public sealed class ClearCharRigRefsCommand : CommandBase
{
    private readonly ClearCharRigRefsCommandSpec _spec;

    public override bool WaitForCompletion => true;

    public ClearCharRigRefsCommand(ClearCharRigRefsCommandSpec spec)
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
        if (scope == null || scope.Refs == null)
            return;

        List<string> keysToClear = CollectTargetRoleKeys(scope);
        if (keysToClear.Count == 0)
            return;

        for (int i = 0; i < keysToClear.Count; i++)
        {
            string roleKey = keysToClear[i];

            if (!scope.Refs.TryGetValue(roleKey, out object value))
                continue;

            CharacterRigRefs rigRefs = value as CharacterRigRefs;

            if (_spec.destroyRigObjects && rigRefs != null)
                DestroyRigRefs(roleKey, rigRefs);

            if (_spec.removeKeys)
                scope.Refs.Remove(roleKey);
            else
                scope.Refs[roleKey] = null;
        }
    }

    private List<string> CollectTargetRoleKeys(CommandRunScope scope)
    {
        var result = new List<string>();

        HashSet<string> filter = BuildRoleKeyFilter();

        foreach (KeyValuePair<string, object> pair in scope.Refs)
        {
            string roleKey = pair.Key;
            if (string.IsNullOrEmpty(roleKey))
                continue;

            if (filter != null && !filter.Contains(roleKey))
                continue;

            if (pair.Value is CharacterRigRefs)
                result.Add(roleKey);
        }

        return result;
    }

    private HashSet<string> BuildRoleKeyFilter()
    {
        if (_spec.onlyRoleKeys == null || _spec.onlyRoleKeys.Length == 0)
            return null;

        var set = new HashSet<string>();

        for (int i = 0; i < _spec.onlyRoleKeys.Length; i++)
        {
            string key = SafeTrim(_spec.onlyRoleKeys[i]);
            if (!string.IsNullOrEmpty(key))
                set.Add(key);
        }

        return set.Count > 0 ? set : null;
    }

    private void DestroyRigRefs(string roleKey, CharacterRigRefs rigRefs)
    {
        GameObject root = ResolveRigRootObject(rigRefs);
        if (root == null)
            return;

        if (_spec.killTweensBeforeDestroy)
            KillTweenBeforeDestroy(root.transform, roleKey);

        Object.Destroy(root);
    }

    private static GameObject ResolveRigRootObject(CharacterRigRefs rigRefs)
    {
        if (rigRefs == null)
            return null;

        if (rigRefs.RigRoot != null)
            return rigRefs.RigRoot.gameObject;

        if (rigRefs.CharacterPortrait_Root != null)
            return rigRefs.CharacterPortrait_Root.root.gameObject;

        if (rigRefs.Character_Track != null)
            return rigRefs.Character_Track.root.gameObject;

        return null;
    }

    private static void KillTweenBeforeDestroy(Transform root, string roleKey)
    {
        if (root == null)
            return;

        roleKey = SafeTrim(roleKey);
        if (!string.IsNullOrEmpty(roleKey))
            DOTween.Kill($"CharPortraitWipe:{roleKey}", false);

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

    private static string SafeTrim(string s)
    {
        return string.IsNullOrEmpty(s) ? "" : s.Trim();
    }
}