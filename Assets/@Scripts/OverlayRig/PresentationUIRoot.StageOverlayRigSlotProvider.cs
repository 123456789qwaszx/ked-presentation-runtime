using UnityEngine;

public interface IStageOverlayRigSlotProvider
{
    RectTransform GetStageOverlayRigRoot(StageOverlayRigRootKind kind);
}

public sealed partial class PresentationUIRoot : IStageOverlayRigSlotProvider
{
    public RectTransform GetStageOverlayRigRoot(StageOverlayRigRootKind kind)
    {
        return kind switch
        {
            StageOverlayRigRootKind.Sprite => View.Rect(Refs.SpriteRig_Root),
            StageOverlayRigRootKind.Text => View.Rect(Refs.TextRig_Root),
            _ => View.Rect(Refs.SpriteRig_Root),
        };
    }
}
