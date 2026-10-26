using System;
using System.Collections.Generic;
using UnityEngine;

public partial class UIManager
{
    private sealed class PageState
    {
        public UIBase Page;
    }

    private readonly Dictionary<UIBase, PageState> _pagesByOwner = new();

    public T SwitchPage<T>(
        UIBase owner,
        Action<T> afterPatched = null,
        Action<UIBase> afterClosed = null)
        where T : UIBase, IUIPage
    {
        if (owner == null)
            return null;

        if (owner is not IUIPageOwner pageOwner)
        {
            Debug.LogError(
                $"[UIManager] View '{owner.GetType().Name}' does not support Pages.",
                this);

            return null;
        }

        if (!IsLivePageOwner(owner))
        {
            Debug.LogError(
                $"[UIManager] View '{owner.GetType().Name}' is not a live Page Owner.",
                this);

            return null;
        }

        if (!TryResolve("Page", out T page))
            return null;

        if (!ValidatePageOwner(page, pageOwner))
            return null;

        BumpShowVersion();

        if (_pagesByOwner.TryGetValue(owner, out PageState current) &&
            current.Page != page)
        {
            HideManagedUI(current.Page);
            afterClosed?.Invoke(current.Page);
        }

        _pagesByOwner[owner] = new PageState
        {
            Page = page,
        };

        ApplyState(
            page,
            active: false,
            interactable: false,
            blocksRaycasts: false,
            alpha: 0f);

        InvokeAfterPatch(page, () =>
        {
            ApplyState(
                page,
                active: true,
                interactable: true,
                blocksRaycasts: true,
                alpha: 1f);

            afterPatched?.Invoke(page);
        });

        return page;
    }

    public UIBase ClosePage(
        UIBase owner,
        Action<UIBase> afterClosed = null)
    {
        if (owner == null)
            return null;

        if (!_pagesByOwner.TryGetValue(owner, out PageState state))
            return null;

        _pagesByOwner.Remove(owner);

        BumpShowVersion();

        HideManagedUI(state.Page);
        afterClosed?.Invoke(state.Page);

        return state.Page;
    }

    private bool IsLivePageOwner(UIBase owner)
    {
        return CurSceneRoot == owner ||
               _panelStack.Contains(owner);
    }

    private bool ValidatePageOwner(
        UIBase page,
        IUIPageOwner owner)
    {
        if (page.transform.parent == owner.PageRoot)
            return true;

        Debug.LogError(
            $"[UIManager] Page '{page.GetType().Name}' must be a child of " +
            $"'{owner.PageRoot.name}'.",
            page);

        return false;
    }
}
