using System;
using System.Collections;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Background Rig", "@Set Background Rig", Order = -998,
    Sets = new[]
    {
        CommandMenuSets.SetupBackground,
    },
    SetOrder = -980)]
public sealed class SetupBackgroundRigCommandSpec : CommandSpecBase
{
    [Header("Rig / Slot")]
    [Tooltip("Rig registration key. Also used as prefix: 'city' -> 'city_'.")]
    public string rigKey;

    [Tooltip("BackgroundRig prefab used for command presentation. " +
             "Empty fields bake a complete rig from BackgroundRigSchema at runtime. " +
             "Prefab the baked result when you need performance setup, external systems, response targets, or shot helpers.")]
    public RectTransform rigPrefab;

    public PresentationStageKey stage = PresentationStageKey.Stage00;
    public PresentationDepthLayerKey layer = PresentationDepthLayerKey.Far;

    [Tooltip("Base root name. Final name is '{rolePrefix}{rigRootName}'.")]
    public string rigRootName = "BackgroundRig";

    public string ResolvedRolePrefix
    {
        get
        {
            if (string.IsNullOrEmpty(rigKey))
                return "";

            return rigKey.EndsWith("_", StringComparison.Ordinal)
                ? rigKey
                : $"{rigKey}_";
        }
    }
}

public sealed class SetupBackgroundRigCommand : CommandBase
{
    private readonly BackgroundRigBuilder _rigBuilder;
    private readonly SetupBackgroundRigCommandSpec _spec;

    public SetupBackgroundRigCommand(
    SetupBackgroundRigCommandSpec spec,
        BackgroundRigBuilder rigBuilder)
    {
        _spec = spec;
        _rigBuilder = rigBuilder;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        Apply(scope);
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope) => Apply(scope);
    
    private void Apply(CommandRunScope scope)
    {
        SetupBackgroundRigCommandSpec spec = _spec;

        string rigKey = spec.rigKey;
        string rolePrefix = spec.ResolvedRolePrefix;

        RectTransform rigRoot = _rigBuilder.BuildBackgroundRigRoot(
            spec.rigPrefab,
            rolePrefix,
            spec.rigRootName);
        
        if (BackgroundRigSlotResolver.TryResolve(_spec.stage, _spec.layer, out RectTransform parent))
            rigRoot.SetParent(parent, false);

        _rigBuilder.BindRefsFromRoot(rigRoot, rolePrefix, out BackgroundRigRefs refs);

        scope.BackgroundRigs.Register(rigKey, refs);

        // Optional bake helper:
        // Enable after refs registration when saving the generated rig as a reusable prefab.
        //StripRolePrefixForBake(rigRoot, rolePrefix, spec.rigRootName);
    }

    #region Helpers
    private static void StripRolePrefixForBake(RectTransform rigRoot, string rolePrefix, string rigRootName)
    {
        if (rigRoot == null)
            return;

        if (string.IsNullOrEmpty(rolePrefix))
            return;

        StripPrefixRecursive(rigRoot, rolePrefix);

        rigRoot.name = rigRootName;
    }

    private static void StripPrefixRecursive(Transform root, string rolePrefix)
    {
        if (root.name.StartsWith(rolePrefix, StringComparison.Ordinal))
            root.name = root.name.Substring(rolePrefix.Length);

        for (int i = 0; i < root.childCount; i++)
            StripPrefixRecursive(root.GetChild(i), rolePrefix);
    }
    #endregion
}