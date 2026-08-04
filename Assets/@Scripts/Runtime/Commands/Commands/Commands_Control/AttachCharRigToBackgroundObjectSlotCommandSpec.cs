using System;
using System.Collections;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Char Rig Parenting",
    "Attach Character Rig To Background Object Slot",
    Order = -760,
    Sets = new[]
    {
        CommandMenuSets.BuildChar,
        CommandMenuSets.SetupBackground,
    },
    SetOrder = -740)]
public sealed class AttachCharRigToBackgroundObjectSlotCommandSpec : CommandSpecBase
{
    [Header("Character")]
    [Tooltip("Registered CharacterRig key.")]
    public string charRigKey;

    [Header("Background")]
    [Tooltip("Registered BackgroundRig key.")]
    public string backgroundRigKey;

    [Tooltip("Background target used as the new parent.")]
    public BackgroundRigTarget parentTarget = BackgroundRigTarget.Background_ObjectSlotRoot;

    [Header("Parenting")]
    [Tooltip("False means the character adopts the target slot's local coordinate space.")]
    public bool worldPositionStays = false;

    [Tooltip("Move the character to the last sibling after parenting.")]
    public bool setAsLastSibling = true;
}

public sealed class AttachCharRigToBackgroundObjectSlotCommand : CommandBase
{
    private readonly AttachCharRigToBackgroundObjectSlotCommandSpec _spec;

    public AttachCharRigToBackgroundObjectSlotCommand(AttachCharRigToBackgroundObjectSlotCommandSpec spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        Apply(scope);
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope) => Apply(scope);

    private void Apply(CommandRunScope scope)
    {
        CharacterRigRefs charRefs = 
            CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.charRigKey);

        if (!scope.BackgroundRigs.TryGetRig(_spec.backgroundRigKey, out BackgroundRigRefs backgroundRefs))
            return;

        RectTransform rigRoot = charRefs.RigRoot;
        RectTransform parent = backgroundRefs.GetRect(_spec.parentTarget);
        RectTransform restoreParent = rigRoot.parent as RectTransform;

        scope.BackgroundRigs.RegisterExternalChild(
            _spec.backgroundRigKey,
            rigRoot,
            restoreParent);

        rigRoot.SetParent(parent, _spec.worldPositionStays);

        if (_spec.setAsLastSibling)
            rigRoot.SetAsLastSibling();
    }
}