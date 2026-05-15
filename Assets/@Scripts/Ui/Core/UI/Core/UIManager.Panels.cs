using System;

public partial class UIManager
{
    public T PushPanel<T>(Action<T> afterPatched = null)
        where T : UIBase, IUIPanel
    {
        if (!TryResolve("Panel", out T panel))
            return null;

        BumpShowVersion();

        Mount(panel, _layerPanels);

        ApplyState(
            panel,
            active: false,
            interactable: false,
            blocksRaycasts: false,
            alpha: 0f);

        if (_panelStack.Contains(panel))
            PopUntil(panel);
        else
            _panelStack.Push(panel);

        InvokeAfterPatch(panel, () =>
        {
            ApplyPanelStackState();
            afterPatched?.Invoke(panel);
        });

        return panel;
    }

    public void PopPanel()
    {
        if (_panelStack.Count == 0)
            return;

        UIBase top = _panelStack.Pop();

        HideManagedUI(top);

        BumpShowVersion();
        ApplyPanelStackState();
    }

    public void PopAllPanels()
    {
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

    private void PopUntil(UIBase target)
    {
        while (_panelStack.Count > 0 && _panelStack.Peek() != target)
        {
            UIBase popped = _panelStack.Pop();
            HideManagedUI(popped);
        }
    }

    private void ApplyPanelStackState()
    {
        if (_panelStack.Count == 0)
            return;

        int keep = UnityEngine.Mathf.Max(1, _keepAliveDepth);

        int index = 0;

        foreach (UIBase panel in _panelStack)
        {
            bool keepAlive = index < keep;

            if (!keepAlive)
            {
                if (panel.gameObject.activeSelf)
                {
                    ApplyState(
                        panel,
                        active: false,
                        interactable: false,
                        blocksRaycasts: false,
                        alpha: 0f);
                }

                index++;
                continue;
            }

            if (index == 0)
            {
                panel.transform.SetAsLastSibling();

                ApplyState(
                    panel,
                    active: true,
                    interactable: true,
                    blocksRaycasts: true,
                    alpha: 1f);
            }
            else
            {
                ApplyState(
                    panel,
                    active: true,
                    interactable: false,
                    blocksRaycasts: false,
                    alpha: _coveredAlpha);
            }

            index++;
        }
    }
}