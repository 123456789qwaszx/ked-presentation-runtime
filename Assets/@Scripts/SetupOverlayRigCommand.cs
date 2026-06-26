using System;
using System.Collections;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Overlay Rig", "@Set Overlay Rig", Order = -970,
    Sets = new[]
    {
        CommandMenuSets.BuildChar,
    },
    SetOrder = -970)]
public sealed class SetupOverlayRigCommandSpec : CommandSpecBase
{
    [Header("Key")]
    [Tooltip("Overlay registration key. Also used as prefix: 'intro_nameplate' -> 'intro_nameplate_'.")]
    public string overlayKey;

    [Header("Parent")]
    [Tooltip("Only chooses which StageOverlay child root owns the rig. The generated rig graph is the same.")]
    public StageOverlayRigRootKind rootKind = StageOverlayRigRootKind.Sprite;

    [Header("Sprite Payload")]
    public Sprite sprite;
    public bool setNativeSize = true;

    [Header("Text Payload")]
    [TextArea]
    public string text;

    [Header("Prefab")]
    [Tooltip("Optional OverlayRig prefab. Empty fields bake a complete graph from OverlayRigSchema at runtime.")]
    public RectTransform prefab;

    [Header("Root")]
    public string rootName = "OverlayRig";

    public string ResolvedRolePrefix
    {
        get
        {
            if (string.IsNullOrEmpty(overlayKey))
                return "";

            return overlayKey.EndsWith("_", StringComparison.Ordinal)
                ? overlayKey
                : $"{overlayKey}_";
        }
    }
}

public sealed class SetupOverlayRigCommand : CommandBase
{
    private readonly StageOverlayRigSlotResolver _slotResolver;
    private readonly OverlayRigBuilder _builder;
    private readonly SetupOverlayRigCommandSpec _spec;

    protected override SkipPolicy SkipPolicy => SkipPolicy.ExecuteEvenIfSkipping;

    public SetupOverlayRigCommand(
        StageOverlayRigSlotResolver slotResolver,
        OverlayRigBuilder builder,
        SetupOverlayRigCommandSpec spec)
    {
        _slotResolver = slotResolver;
        _builder = builder;
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        SetupOverlayRigCommandSpec spec = _spec;

        string key = (spec.overlayKey ?? string.Empty).Trim();
        string rolePrefix = spec.ResolvedRolePrefix;

        RectTransform root = _builder.BuildOverlayRoot(
            spec.prefab,
            rolePrefix,
            spec.rootName);

        if (_slotResolver.TryResolve(spec.rootKind, out RectTransform parent))
            root.SetParent(parent, false);

        _builder.BindRefsFromRoot(root, rolePrefix, out OverlayRigRefs refs);

        refs.ResetToBaselineImmediate();
        refs.SetSprite(spec.sprite, spec.setNativeSize);
        refs.SetText(spec.text);

        scope.OverlayRigs.Register(key, refs);

        yield break;
    }
}