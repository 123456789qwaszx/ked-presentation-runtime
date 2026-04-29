using System;
using System.Collections;
using UnityEngine;

public partial class UIManager
{
    private UIPatchService _uiPatchService;
    
    private UIContext _uiContext = new("default", "ko-KR");
    private int _showVersion;

    public void AttachUIPatchService(UIPatchService uiPatchService)
    {
        _uiPatchService = uiPatchService;
    }
    
    public void SetUIContext(UIContext context)
    {
        _uiContext = context;
    }

    public void SetUIContext(string themeId, string localeId)
    {
        _uiContext = new UIContext(themeId, localeId);
    }

    private bool HasUIPatch()
    {
        if (_uiPatchService == null)
            return false;

        if (string.IsNullOrEmpty(_uiContext.ThemeId))
            return false;

        if (string.IsNullOrEmpty(_uiContext.LocaleId))
            return false;

        return true;
    }

    private void BumpShowVersion()
    {
        _showVersion++;
    }

    private void InvokeAfterPatch(UIBase ui, Action callback)
    {
        if (ui == null)
            return;

        if (!HasUIPatch())
        {
            callback?.Invoke();
            return;
        }

        int ticket = _showVersion;
        StartCoroutine(CoPatchThen(ticket, ui, callback));
    }

    private IEnumerator CoPatchThen(int ticket, UIBase ui, Action callback)
    {
        yield return _uiPatchService.PatchUIInHierarchy(ui, _uiContext);

        if (ticket != _showVersion)
            yield break;

        callback?.Invoke();
    }

    public void RepatchVisible()
    {
        if (!HasUIPatch())
            return;

        BumpShowVersion();
        int ticket = _showVersion;
        StartCoroutine(CoRepatchVisible(ticket));
    }

    private IEnumerator CoRepatchVisible(int ticket)
    {
        if (CurSceneRoot != null && CurSceneRoot.gameObject.activeInHierarchy)
        {
            yield return _uiPatchService.PatchUIInHierarchy(CurSceneRoot, _uiContext);

            if (ticket != _showVersion)
                yield break;
        }

        if (_panelStack.Count > 0)
        {
            int keep = Mathf.Max(1, _keepAliveDepth);
            int index = 0;

            foreach (UIBase panel in _panelStack)
            {
                if (index >= keep)
                    break;

                if (panel != null && panel.gameObject.activeInHierarchy)
                {
                    yield return _uiPatchService.PatchUIInHierarchy(panel, _uiContext);

                    if (ticket != _showVersion)
                        yield break;
                }

                index++;
            }

            ApplyPanelStackState();
        }

        if (_layerOverlay != null)
        {
            for (int i = 0; i < _layerOverlay.childCount; i++)
            {
                UIBase ui = _layerOverlay.GetChild(i).GetComponent<UIBase>();
                if (ui != null && ui.gameObject.activeInHierarchy)
                {
                    yield return _uiPatchService.PatchUIInHierarchy(ui, _uiContext);

                    if (ticket != _showVersion)
                        yield break;
                }
            }
        }

        if (_layerTop != null)
        {
            for (int i = 0; i < _layerTop.childCount; i++)
            {
                UIBase ui = _layerTop.GetChild(i).GetComponent<UIBase>();
                if (ui != null && ui.gameObject.activeInHierarchy)
                {
                    yield return _uiPatchService.PatchUIInHierarchy(ui, _uiContext);

                    if (ticket != _showVersion)
                        yield break;
                }
            }
        }
    }

    public Coroutine PatchNow(UIBase ui, Action afterPatched = null)
    {
        if (ui == null)
            return null;

        if (!HasUIPatch())
        {
            afterPatched?.Invoke();
            return null;
        }

        return StartCoroutine(CoPatchNow(ui, afterPatched));
    }

    private IEnumerator CoPatchNow(UIBase ui, Action afterPatched)
    {
        yield return _uiPatchService.PatchUIInHierarchy(ui, _uiContext);
        afterPatched?.Invoke();
    }
}