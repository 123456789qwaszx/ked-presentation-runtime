using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    private void BindOverlayRig(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string>(
            "overlay_rig",
            EnqueueSetupOverlayRigSpec);

        runner.AddCommandHandler<string>(
            "sprite_rig",
            EnqueueSetupSpriteOverlayRigSpec);

        runner.AddCommandHandler<string>(
            "text_rig",
            EnqueueSetupTextOverlayRigSpec);
    }

    private void EnqueueSetupOverlayRigSpec(
        string overlayKey,
        string rootKindToken = "sprite")
    {
        var spec = new SetupOverlayRigCommandSpec
        {
            overlayKey = overlayKey,
            rootKind = StageOverlayRigRootKindParser.Parse(
                rootKindToken,
                StageOverlayRigRootKind.Sprite),
            prefab = _overlayRigPrefab,
            rootName = "OverlayRig"
        };

        Collect(spec);
    }

    private void EnqueueSetupSpriteOverlayRigSpec(string overlayKey)
    {
        var spec = new SetupOverlayRigCommandSpec
        {
            overlayKey = overlayKey,
            rootKind = StageOverlayRigRootKind.Sprite,
            prefab = _overlayRigPrefab,
            rootName = "SpriteOverlayRig"
        };

        Collect(spec);
    }

    private void EnqueueSetupTextOverlayRigSpec(string overlayKey)
    {
        var spec = new SetupOverlayRigCommandSpec
        {
            overlayKey = overlayKey,
            rootKind = StageOverlayRigRootKind.Text,
            prefab = _overlayRigPrefab,
            rootName = "TextOverlayRig"
        };

        Collect(spec);
    }
}
