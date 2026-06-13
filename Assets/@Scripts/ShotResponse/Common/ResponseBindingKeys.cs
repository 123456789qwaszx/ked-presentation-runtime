public static class ResponseBindingKeys
{
    private const string StageDepthPrefix = "depth:";
    
    public static string StageDepthLayer(StageDepthLayer layer) => StageDepthPrefix + layer;
}