using System;
using System.Collections;
using UnityEngine;

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

    [Tooltip("CharacterRig prefab used for command presentation. " +
             "Empty fields bake a complete rig from CharacterRigSchema at runtime. " +
             "Prefab the baked result when you need performance setup, external systems, response targets, or shot helpers.")]
    public RectTransform rigPrefab;

    [Tooltip("Slot to attach this rig to.")]
    public CharRigSlot parentSlot = CharRigSlot.Stage00CharacterSlot;

    [Tooltip("Base root name. Final name is '{rolePrefix}{rigRootName}'.")]
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
    private readonly CharRigSlotResolver _slotResolver;
    private readonly CharacterRigBuilder _rigBuilder;
    private readonly SetupCharRigCommandSpec _spec;

    public override bool WaitForCompletion => true;

    public SetupCharRigCommand(
        CharRigSlotResolver slotResolver,
        CharacterRigBuilder rigBuilder,
        SetupCharRigCommandSpec spec)
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

        string rigKey = spec.roleKey;
        string rolePrefix = spec.ResolvedRolePrefix;

        RectTransform rigRoot = _rigBuilder.BuildCharacterRigRoot(
            spec.rigPrefab,
            rolePrefix,
            spec.rigRootName);
        
        if (_slotResolver.TryResolve(spec.parentSlot, out RectTransform parent))
            rigRoot.SetParent(parent, false);
        
        _rigBuilder.BindRefsFromRoot(rigRoot, rolePrefix, out CharacterRigRefs refs);

        scope.characterRigs.Register(rigKey, refs);
        
        // Optional bake helper:
        // Enable after refs registration when saving the generated rig as a reusable prefab.
        //StripRolePrefixForBake(rigRoot, rolePrefix, spec.rigRootName);
    }
    
    #region Helpers
    // For prefab baking: turns 'Caramel_CharSlot_Anchor' back into 'CharSlot_Anchor'.
    // Safe only after refs are already bound.
    // Do not call before BuildRefMap/BindRefs.
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