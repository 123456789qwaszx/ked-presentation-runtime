using UnityEngine;
using UnityEngine.UI;

public sealed partial class EpisodeSelectionPanel : IEpisodeGraphScrollRootProvider
{
    public ScrollRect GraphScrollRect => View?.Rect(Refs.ButtonViewport)?.GetComponent<ScrollRect>();
    public RectTransform GraphContent => View?.Rect(Refs.EpisodeButtons);
    public RectTransform GraphViewport => View?.Rect(Refs.ButtonViewport);
}