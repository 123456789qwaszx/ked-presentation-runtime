using UnityEngine;

public sealed class SystemMenuPanel
    : UIPanel<SystemMenuPanel.Refs>, IUIPageOwner
{
    public RectTransform PageRoot => _pageRoot;

    public enum Refs
    {
        PageRoot,
    }

    private RectTransform _pageRoot;

    protected override void OnInitialize()
    {
        _pageRoot = View.Rect(Refs.PageRoot);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (_pageRoot == null)
        {
            Debug.LogWarning(
                $"[SystemMenuPanel] Missing ref: {Refs.PageRoot}",
                this);
        }
#endif
    }
}
