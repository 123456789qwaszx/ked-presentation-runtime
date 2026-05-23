public static class ResponseBindingKeys
{
    private const string CharacterSlotPrefix = "slot:";
    private const string BackgroundPrefix = "bg:";

    public static string CharacterRig(CommandRunScope scope, string targetKey)
    {
        return CharacterRigTargetResolver.ResolveRigKeyByPolicy(scope, targetKey);
    }

    public static string CharacterRigFromSlotKey(string slotKey)
    {
        return CharacterSlotPrefix + slotKey;
    }

    public static string ResolveCharBindingKey(CommandRunScope scope, string targetKey)
    {
        string slotKey = CharacterRig(scope, targetKey);
        return CharacterRigFromSlotKey(slotKey);
    }

    public static string BackgroundRig(CommandRunScope scope, string targetKey)
    {
        return targetKey;
    }

    public static string BackgroundRigFromRigKey(string bgKey)
    {
        return BackgroundPrefix + bgKey;
    }

    public static string ResolveBackgroundBindingKey(CommandRunScope scope, string targetKey)
    {
        string bgKey = BackgroundRig(scope, targetKey);
        return BackgroundRigFromRigKey(bgKey);
    }
}