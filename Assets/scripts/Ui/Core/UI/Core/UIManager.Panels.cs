using System;

public partial class UIManager
{
    // 기본 API를 Patched로 승격 (누락 방지)
    public T PushPanel<T>(Action<T> callback = null)
        where T : UIBase, IUIPanel
    {
        return PushPanelPatched(callback);
    }

    public T PushPanelPatched<T>(Action<T> afterPatched = null)
        where T : UIBase, IUIPanel
    {
        BumpShowVersion();

        if (!TryResolve("Panel", out T panel))
            return null;

        Mount(panel, _layerPanels);

        // 패치 전 깜빡임 방지: 비활성/투명
        ApplyState(panel, active: false, interactable: false, blocksRaycasts: false, alpha: 0f);

        if (_panelStack.Contains(panel))
            PopUntil(panel);
        else
            _panelStack.Push(panel);

        // InvokeAfterPatch(panel, () =>
        // {
        //     ApplyPanelStackState(); // Top/coveredAlpha 정책 포함
        //     afterPatched?.Invoke(panel);
        // });

        return panel;
    }

    public void PopPanel()
    {
        BumpShowVersion();

        if (_panelStack.Count == 0)
            return;

        UIBase top = _panelStack.Pop();
        ApplyState(top, active: false, interactable: false, blocksRaycasts: false, alpha: 0f);

        ApplyPanelStackState();
    }

    public void PopAllPanels()
    {
        BumpShowVersion();

        while (_panelStack.Count > 0)
            PopPanel();
    }

    public UIBase PeekPanel()
    {
        if (_panelStack.Count == 0)
        {
            UnityEngine.Debug.Log("[UIManager] Panel stack is empty.", this);
            return null;
        }

        return _panelStack.Peek();
    }

    // - Bring an existing panel to top by popping (and hiding) panels above it.
    private void PopUntil(UIBase target)
    {
        BumpShowVersion();

        while (_panelStack.Count > 0 && _panelStack.Peek() != target)
        {
            UIBase popped = _panelStack.Pop();
            ApplyState(popped, active: false, interactable: false, blocksRaycasts: false, alpha: 0f);
        }

        if (_panelStack.Count > 0)
        {
            UIBase top = _panelStack.Peek();
            ApplyState(top, active: true, interactable: true, blocksRaycasts: true, alpha: 1f);
            top.transform.SetAsLastSibling();
        }
    }

    // - The topmost panel (index 0) is interactive and fully opaque.
    // - Panels below the top (within keepAliveDepth) stay visible but are not interactive (optionally dimmed).
    // - Panels beyond keepAliveDepth are fully deactivated (hidden).
    private void ApplyPanelStackState()
    {
        if (_panelStack.Count == 0)
            return;

        int keep = UnityEngine.Mathf.Max(1, _keepAliveDepth);

        int index = 0;
        foreach (UIBase panel in _panelStack) // Stack 열거 = top부터
        {
            bool keepAlive = index < keep;

            if (!keepAlive)
            {
                if (panel.gameObject.activeSelf)
                    ApplyState(panel, active: false, interactable: false, blocksRaycasts: false, alpha: 0f);

                index++;
                continue;
            }

            if (!panel.gameObject.activeSelf)
                panel.gameObject.SetActive(true);

            if (index == 0)
            {
                panel.transform.SetAsLastSibling();
                ApplyState(panel, active: true, interactable: true, blocksRaycasts: true, alpha: 1f);
            }
            else
            {
                ApplyState(panel, active: true, interactable: false, blocksRaycasts: false, alpha: _coveredAlpha);
            }

            index++;
        }
    }
}
