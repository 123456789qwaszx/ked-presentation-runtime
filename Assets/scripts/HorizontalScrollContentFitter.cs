using UnityEngine;
using UnityEngine.UI;

// Manually rebuilds a horizontal ScrollRect content width based on its children bounds.
// Optionally normalizes left-anchored content, shifts children to start at padding, and resets scroll to left
public sealed class HorizontalScrollContentFitter : MonoBehaviour
{
    [Header("Refs (optional, auto-resolve if empty)")]
    [SerializeField] private RectTransform content;
    [SerializeField] private RectTransform viewport;
    [SerializeField] private ScrollRect scrollRect;

    [Header("Padding")]
    [SerializeField] private float paddingLeft = 0f;
    [SerializeField] private float paddingRight = 200f;

    [Header("Options")]
    [SerializeField] private bool includeInactive = false;
    [SerializeField] private bool forceLeftAnchoredContent = true;
    [SerializeField] private bool shiftChildrenToMakeMinXAtPaddingLeft = true;
    [SerializeField] private bool alignLeftAfterRebuild = true;

    private static readonly Vector3[] _corners = new Vector3[4];

    private void Reset()
    {
        content = transform as RectTransform;

        scrollRect = GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
        {
            if (scrollRect.content != null) content = scrollRect.content;
            viewport = scrollRect.viewport;
        }
    }

    [ContextMenu("Rebuild Size")]
    public void RebuildSize()
    {
        ResolveRefs();
        if (content == null || viewport == null) return;

        Canvas.ForceUpdateCanvases();

        if (forceLeftAnchoredContent)
            EnsureLeftAnchoredContent(content);

        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        int count = 0;

        for (int i = 0; i < content.childCount; i++)
        {
            var child = content.GetChild(i) as RectTransform;
            if (child == null) continue;
            if (!includeInactive && !child.gameObject.activeInHierarchy) continue;

            child.GetWorldCorners(_corners);

            for (int c = 0; c < 4; c++)
            {
                Vector3 local = content.InverseTransformPoint(_corners[c]);
                if (local.x < minX) minX = local.x;
                if (local.x > maxX) maxX = local.x;
            }

            count++;
        }

        float viewportW = viewport.rect.width;

        if (count == 0)
        {
            SetContentWidth(viewportW);

            if (alignLeftAfterRebuild && scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.horizontalNormalizedPosition = 0f;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            return;
        }

        if (shiftChildrenToMakeMinXAtPaddingLeft)
        {
            float delta = (paddingLeft - minX);
            if (!Mathf.Approximately(delta, 0f))
            {
                for (int i = 0; i < content.childCount; i++)
                {
                    var child = content.GetChild(i) as RectTransform;
                    if (child == null) continue;
                    if (!includeInactive && !child.gameObject.activeInHierarchy) continue;

                    var ap = child.anchoredPosition;
                    ap.x += delta;
                    child.anchoredPosition = ap;
                }

                minX += delta;
                maxX += delta;
            }
        }

        float childrenW = (maxX - minX) + paddingLeft + paddingRight;
        float targetW = Mathf.Max(viewportW, childrenW);

        SetContentWidth(targetW);

        if (alignLeftAfterRebuild && scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.horizontalNormalizedPosition = 0f;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }

    private void EnsureLeftAnchoredContent(RectTransform rt)
    {
        var aMin = rt.anchorMin;
        var aMax = rt.anchorMax;
        var piv = rt.pivot;

        bool changed = false;

        if (!Mathf.Approximately(aMin.x, 0f)) { aMin.x = 0f; changed = true; }
        if (!Mathf.Approximately(aMax.x, 0f)) { aMax.x = 0f; changed = true; }
        if (!Mathf.Approximately(piv.x, 0f))  { piv.x  = 0f; changed = true; }

        if (changed)
        {
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = piv;

            float curW = rt.rect.width;
            rt.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0f, curW);
        }
    }

    private void SetContentWidth(float width)
    {
        var sd = content.sizeDelta;
        sd.x = width;
        content.sizeDelta = sd;
    }

    private void ResolveRefs()
    {
        if (content == null) content = transform as RectTransform;

        if (scrollRect == null) scrollRect = GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
        {
            if (viewport == null) viewport = scrollRect.viewport;
            if (content == null && scrollRect.content != null) content = scrollRect.content;
        }

        if (viewport == null && content != null && content.parent is RectTransform)
            viewport = (RectTransform)content.parent;
    }
}