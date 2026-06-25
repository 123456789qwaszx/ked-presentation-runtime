public enum PresentationStageKey
{
    Stage00 = 0,
    Stage01 = 1,
    Stage02 = 2,
    
    Count = 3
}

public enum PresentationDepthLayerKey
{
    Far = 0,
    Back = 1,
    Mid = 2,
    Front = 3,
    Close = 4,
    
    Count = 5
}


public sealed partial class PresentationUIRoot
{
    private const int PresentationStageCount = (int)PresentationStageKey.Count;
    private const int PresentationDepthLayerCount = (int)PresentationDepthLayerKey.Count;
}