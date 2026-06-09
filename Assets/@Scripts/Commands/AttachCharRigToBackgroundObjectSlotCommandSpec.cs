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

    public override bool WaitForCompletion => true;

    public AttachCharRigToBackgroundObjectSlotCommand(
        AttachCharRigToBackgroundObjectSlotCommandSpec spec)
    {
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
        AttachCharRigToBackgroundObjectSlotCommandSpec spec = _spec;

        if (scope == null)
            return;

        if (string.IsNullOrEmpty(spec.charRigKey))
        {
            Debug.LogWarning("[AttachCharRigToBackgroundObjectSlotCommand] charRigKey is empty.");
            return;
        }

        if (string.IsNullOrEmpty(spec.backgroundRigKey))
        {
            Debug.LogWarning("[AttachCharRigToBackgroundObjectSlotCommand] backgroundRigKey is empty.");
            return;
        }

        if (!scope.characterRigs.TryGetRig(spec.charRigKey, out CharacterRigRefs charRefs))
            return;

        if (!scope.backgroundRigs.TryGetRig(spec.backgroundRigKey, out BackgroundRigRefs backgroundRefs))
            return;

        RectTransform childRoot = charRefs.RigRoot;
        if (childRoot == null)
        {
            Debug.LogWarning($"[AttachCharRigToBackgroundObjectSlotCommand] Character rig root is null. charRigKey='{spec.charRigKey}'.");
            return;
        }

        RectTransform parent = backgroundRefs.GetRect(spec.parentTarget);
        if (parent == null)
        {
            Debug.LogWarning(
                $"[AttachCharRigToBackgroundObjectSlotCommand] Background parent target is null. " +
                $"backgroundRigKey='{spec.backgroundRigKey}', parentTarget='{spec.parentTarget}'.");
            return;
        }

        RectTransform restoreParent = childRoot.parent as RectTransform;

        scope.backgroundRigs.RegisterExternalChild(
            spec.backgroundRigKey,
            childRoot,
            restoreParent);

        childRoot.SetParent(parent, spec.worldPositionStays);

        if (spec.setAsLastSibling)
            childRoot.SetAsLastSibling();
    }
}