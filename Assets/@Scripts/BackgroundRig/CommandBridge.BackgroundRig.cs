using Yarn.Unity;
using UnityEngine;

public sealed partial class YarnCommandBridge
{
    public void RegisterBackgroundRigCommands()
    {
        _dialogueRunner.AddCommandHandler<string, string>(
            "spawn_bg",
            EnqueueSetupBackgroundRigSpec);

        _dialogueRunner.AddCommandHandler<string, float, float, float, float, float>(
            "bg_place",
            EnqueueSetBackgroundAnchorSpec);
    }

    private void EnqueueSetupBackgroundRigSpec(string rigKey, string parentSlotKey)
    {
        var spec = new SetupBackgroundRigCommandSpec
        {
            rigKey = rigKey,
            parentSlot = BackgroundRigSlotParser.Parse(parentSlotKey, BackgroundRigSlot.Stage00BackgroundSlot),
            rigRootName = "BackgroundRig",
            rigPrefab = null
        };

        Collect(spec);
    }

    private void EnqueueSetBackgroundAnchorSpec(
        string rigKey,
        float x = 0f,
        float y = 0f,
        float scaleX = 1f,
        float scaleY = 1f,
        float rotationZ = 0f)
    {
        var spec = new SetAnchorCommandSpecBgR
        {
            rigKey = rigKey,
            target = BackgroundRigTarget.Background_CastTransform,
            anchoredPosition = new Vector2(x, y),
            scale = new Vector2(scaleX, scaleY),
            rotationZ = rotationZ,
            resetFraming = false,
            resetActing = true
        };

        Collect(spec);
    }
}