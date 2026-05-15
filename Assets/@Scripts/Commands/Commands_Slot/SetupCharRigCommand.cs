using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[Serializable]
[CommandMenuHint(
    "Char Rig", "@Set Character Rig", Order = -999,
    Sets = new[]
    {
        CommandMenuSets.BuildChar,
    },
    SetOrder = -980)]
public sealed class SetupCharRigCommandSpec : CommandSpecBase
{
    [Header("Role / Slot")]
    [Tooltip("Rig registration key. Also used as prefix: 'hill' -> 'hill_'.")]
    public string roleKey;

    [Header("Rig")]
    [Tooltip("Optional rig prefab. If empty, the schema generates the rig graph.")]
    public RectTransform rigPrefab;

    [Tooltip("Slot to attach this rig to.")]
    public CharRigSlot parentSlot = CharRigSlot.Stage00CharacterSlot;

    [Tooltip("Base root name. Final name is '{rolePrefix}_{rigRootName}'.")]
    public string rigRootName = "CharacterRig";

    public string ResolvedRolePrefix
    {
        get
        {
            if (string.IsNullOrEmpty(roleKey))
                return "";

            return roleKey.EndsWith("_", StringComparison.Ordinal)
                ? roleKey
                : $"{roleKey}_";
        }
    }
}

public sealed class SetupCharRigCommand : CommandBase
{
    private readonly ICharRigSlotResolver _slotResolver;
    private readonly CharacterRigBuilder _rigBuilder;
    private readonly SetupCharRigCommandSpec _spec;

    public override bool WaitForCompletion => true;

    public SetupCharRigCommand(ICharRigSlotResolver slotResolver, CharacterRigBuilder rigBuilder, SetupCharRigCommandSpec spec)
    {
        _slotResolver = slotResolver;
        _rigBuilder = rigBuilder;
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        Apply(scope);
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope) => Apply(scope);
    protected override void OnRollbackSeek(CommandRunScope scope) => OnSkip(scope);
    
    private void Apply(CommandRunScope scope)
    {
        SetupCharRigCommandSpec spec = _spec;

        string roleKey = spec.roleKey;
        string rolePrefix = spec.ResolvedRolePrefix;
        RectTransform rigRoot = _rigBuilder.BuildCharacterRigRoot(spec.rigPrefab, rolePrefix, spec.rigRootName);
        
        if (_slotResolver.TryResolve(spec.parentSlot, out RectTransform parent))
            rigRoot.SetParent(parent, false);
        
        _rigBuilder.BindRefsFromRoot(rigRoot, rolePrefix, out CharacterRigRefs refs);
        
        if (!scope.Refs.TryAdd(roleKey, refs))
        {
            RemoveRegisteredRig(scope, roleKey);
            
            scope.Refs[roleKey] = refs;
            Debug.LogWarning($"[SetupCharRigCommand] Rebound rig refs. roleKey='{roleKey}'.");
        }
    }
    
    
    #region Helpers
    private static void RemoveRegisteredRig(CommandRunScope scope, string roleKey)
    {
        if (!scope.Refs.TryGetCharRigRefs(roleKey, out CharacterRigRefs existingRig) || existingRig?.RigRoot == null)
            return;

        KillTweenOnHierarchy(existingRig.RigRoot);
        Object.Destroy(existingRig.RigRoot.gameObject);

        Debug.LogWarning($"[SetupCharRigCommand] Removing leftover rig. roleKey='{roleKey}'.");
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
    #endregion
}