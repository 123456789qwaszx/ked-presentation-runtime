using UnityEngine;

public sealed class VNDefaultOptionsPanel
    : PresentationOptionsBoxViewBase<VNDefaultOptionsPanel.Refs>
{
    public enum Refs
    {
        OptionBox_Root,
        ItemContainer,
    }
    
    public override RectTransform Root => View.Rect(Refs.OptionBox_Root);
    public override CanvasGroup CanvasGroup => View.CanvasGroup(Refs.OptionBox_Root);
    public override RectTransform ItemContainer => View.Rect(Refs.ItemContainer);
}