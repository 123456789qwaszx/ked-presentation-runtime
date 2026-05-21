using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    public void RegisterBackgroundRigCommands()
    {
        _dialogueRunner.AddCommandHandler<string, string>("setup_bg_rig", EnqueueSetupBackgroundRigSpec);
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
}