public static class ResponseBindingKeys
{
    private const string CharacterSlotPrefix = "slot:";
    private const string BackgroundPrefix = "bg:";
    private const string StageDepthPrefix = "depth:";
    
    public static string CharacterRigFromSlotKey(string slotKey) => CharacterSlotPrefix + slotKey;
    public static string BackgroundRigFromRigKey(string bgKey) => BackgroundPrefix + bgKey;
    public static string StageDepthLayer(StageDepthLayer layer) => StageDepthPrefix + layer;
}