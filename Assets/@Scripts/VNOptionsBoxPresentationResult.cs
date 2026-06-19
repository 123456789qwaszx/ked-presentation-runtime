using UnityEngine;

public sealed class VNOptionsBoxPresentationResult
{
    public IPresentationOptionsBoxView View { get; }
    public RectTransform ItemContainer { get; }

    public bool IsValid
    {
        get
        {
            return View != null &&
                   ItemContainer != null;
        }
    }

    public VNOptionsBoxPresentationResult(
        IPresentationOptionsBoxView view)
    {
        View = view;
        ItemContainer = view != null
            ? view.ItemContainer
            : null;
    }

    public static VNOptionsBoxPresentationResult Invalid()
    {
        return new VNOptionsBoxPresentationResult(null);
    }
}