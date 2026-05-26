using UnityEngine;
using UnityEngine.UI;

public sealed class EpisodeGraphScrollController
{
    private IEpisodeGraphScrollRootProvider _rootProvider;
    private IEpisodeGraphScrollRootProvider RootProvider => _rootProvider ??= ResolveRootProvider();
    private IEpisodeGraphScrollRootProvider ResolveRootProvider() => UIManager.Instance.GetUI<EpisodeSelectionPanel>();
    
    public void ScrollToLeft()
    {
        ScrollRect scrollRect = RootProvider.GraphScrollRect;
        RectTransform content = RootProvider.GraphContent;

        if (scrollRect == null || content == null)
            return;

        Canvas.ForceUpdateCanvases();

        scrollRect.StopMovement();
        scrollRect.horizontalNormalizedPosition = 0f;

        Vector2 pos = content.anchoredPosition;
        pos.x = 0f;
        content.anchoredPosition = pos;
    }

    public bool ScrollToEpisode(
        EpisodeGraphViewData viewData,
        string episodeId,
        float viewportAnchor01 = 0.5f)
    {
        if (viewData == null || string.IsNullOrEmpty(episodeId))
            return false;

        if (!viewData.TryGetNode(episodeId, out EpisodeNodeViewData node))
            return false;

        return ScrollToPositionX(node.AnchoredPosition.x, viewportAnchor01);
    }

    public bool ScrollToPositionX(float contentLocalX, float viewportAnchor01 = 0.5f)
    {
        RectTransform content = RootProvider.GraphContent;
        RectTransform viewport = RootProvider.GraphViewport;

        if (content == null || viewport == null)
            return false;

        Canvas.ForceUpdateCanvases();

        float contentWidth = content.rect.width;
        float viewportWidth = viewport.rect.width;

        if (contentWidth <= viewportWidth)
        {
            SetContentX(0f);
            return true;
        }

        float targetViewportX = viewportWidth * Mathf.Clamp01(viewportAnchor01);
        float targetContentX = targetViewportX - contentLocalX;

        float minX = viewportWidth - contentWidth;
        float maxX = 0f;

        targetContentX = Mathf.Clamp(targetContentX, minX, maxX);

        SetContentX(targetContentX);
        return true;
    }

    private void SetContentX(float x)
    {
        ScrollRect scrollRect = RootProvider.GraphScrollRect;
        RectTransform content = RootProvider.GraphContent;

        if (content == null)
            return;

        if (scrollRect != null)
            scrollRect.StopMovement();

        Vector2 pos = content.anchoredPosition;
        pos.x = x;
        content.anchoredPosition = pos;
    }
}