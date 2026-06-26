using System;
using System.Collections;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Stage Overlay",
    "Overlay Sprite",
    Order = -935)]
public sealed class OverlaySpriteCommandSpec : CommandSpecBase
{
    [Header("Overlay")]
    public string rigKey;

    [Header("Sprite")]
    public Sprite sprite;
    public string resourcesPath;
    public bool setNativeSize = true;
}

public sealed class OverlaySpriteCommand : CommandBase
{
    private readonly OverlaySpriteCommandSpec _spec;

    public OverlaySpriteCommand(OverlaySpriteCommandSpec spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!scope.OverlayRigs.TryGet(_spec.rigKey, out OverlayRigRefs refs))
            yield break;

        Sprite sprite = _spec.sprite;

        if (sprite == null && !string.IsNullOrWhiteSpace(_spec.resourcesPath))
            sprite = Resources.Load<Sprite>(_spec.resourcesPath.Trim());

        refs.SetSprite(sprite, _spec.setNativeSize);

        yield break;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!scope.OverlayRigs.TryGet(_spec.rigKey, out OverlayRigRefs refs))
            return;

        Sprite sprite = _spec.sprite;

        if (sprite == null && !string.IsNullOrWhiteSpace(_spec.resourcesPath))
            sprite = Resources.Load<Sprite>(_spec.resourcesPath.Trim());

        refs.SetSprite(sprite, _spec.setNativeSize);
    }
}