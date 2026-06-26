using System;
using System.Collections;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Sprite Image", "@Set Sprite Image", Order = -970,
    Sets = new[]
    {
        CommandMenuSets.BuildChar,
    },
    SetOrder = -970)]
public sealed class SetupSpriteImageRigCommandSpec : CommandSpecBase
{
    [Header("Key")]
    [Tooltip("Sprite image registration key. Also used as prefix: 'intro_nameplate' -> 'intro_nameplate_'.")]
    public string imageKey;

    [Header("Sprite")]
    public Sprite sprite;
    public bool setNativeSize = true;

    [Header("Prefab")]
    [Tooltip("Optional SpriteImage prefab. Empty fields bake a complete sprite image graph from SpriteImageSchema at runtime.")]
    public RectTransform prefab;

    [Header("Stage Depth Slot")]
    public PresentationStageKey stage = PresentationStageKey.Stage00;
    public PresentationDepthLayerKey layer = PresentationDepthLayerKey.Mid;

    [Header("Special Parent")]
    public bool useProtagonistSlot = false;

    [Header("Root")]
    public string rootName = "SpriteImage";

    public string ResolvedRolePrefix
    {
        get
        {
            if (string.IsNullOrEmpty(imageKey))
                return "";

            return imageKey.EndsWith("_", StringComparison.Ordinal)
                ? imageKey
                : $"{imageKey}_";
        }
    }
}

public sealed class SetupSpriteImageRigCommand : CommandBase
{
    private readonly CharRigSlotResolver _slotResolver;
    private readonly SpriteImageRigBuilder _builder;
    private readonly SetupSpriteImageRigCommandSpec _spec;

    protected override SkipPolicy SkipPolicy => SkipPolicy.ExecuteEvenIfSkipping;

    public SetupSpriteImageRigCommand(
        CharRigSlotResolver slotResolver,
        SpriteImageRigBuilder builder,
        SetupSpriteImageRigCommandSpec spec)
    {
        _slotResolver = slotResolver;
        _builder = builder;
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        SetupSpriteImageRigCommandSpec spec = _spec;

        string key = (spec.imageKey ?? string.Empty).Trim();
        string rolePrefix = spec.ResolvedRolePrefix;

        RectTransform root = _builder.BuildSpriteImageRoot(
            spec.prefab,
            rolePrefix,
            spec.rootName);

        bool resolved = spec.useProtagonistSlot
            ? _slotResolver.TryResolveProtagonist(out RectTransform parent)
            : _slotResolver.TryResolve(spec.stage, spec.layer, out parent);

        if (resolved)
            root.SetParent(parent, false);

        _builder.BindRefsFromRoot(root, rolePrefix, out SpriteImageRigRefs refs);

        refs.ResetToBaselineImmediate();
        refs.SetSprite(spec.sprite, spec.setNativeSize);

        scope.SpriteImages.Register(key, refs);

        yield break;
    }
}