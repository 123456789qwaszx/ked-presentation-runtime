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
    
    public void PopPanel(Action<UIBase> afterPopped = null)
    {
        if (_panelStack.Count == 0)
            return;

        UIBase popped = _panelStack.Pop();

        // 숨기기 전에 티켓을 무효화한다. 로더가 비동기가 되면 이미 숨긴 UI에 패치가 적용될 수 있다.
        BumpShowVersion();

        HideManagedUI(popped);

        afterPopped?.Invoke(popped);

        ApplyPanelStackState();
    }
    
    public void PopAllPanels(Action<UIBase> afterEachPatched = null)
    {
        while (_panelStack.Count > 0)
            PopPanel(afterEachPatched);
    }

    
    private void PopUntil(UIBase target, Action<UIBase> afterPopped = null)
    {
        while (_panelStack.Count > 0 && _panelStack.Peek() != target)
        {
            UIBase popped = _panelStack.Pop();

            HideManagedUI(popped);
            afterPopped?.Invoke(popped);
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