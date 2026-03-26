using System;
using UnityEngine;

public partial class UIManager
{
    public T ShowOverlay<T>(Action<T> afterPatched = null)
        where T : UIBase, IUIOverlay
    {
        if (!TryResolve("Overlay", out T overlay))
            return null;

        Mount(overlay, _layerOverlay);

        // overlay는 보통 입력 막지 않으니 기본은 interactable=false
        ApplyState(overlay, active: false, interactable: false, blocksRaycasts: false, alpha: 0f);

        // InvokeAfterPatch(overlay, () =>
        // {
        //     ApplyState(overlay, active: true, interactable: false, blocksRaycasts: false, alpha: 1f);
        //     afterPatched?.Invoke(overlay);
        // });

        return overlay;
    }

    public void HideOverlay<T>() where T : UIBase
    {
        T overlay = GetUI<T>();
        ApplyState(overlay, active: false, interactable: false, blocksRaycasts: false, alpha: 0f);
    }

    public void ClearOverlay()
    {
        if (_layerOverlay == null) return;

        for (int i = _layerOverlay.childCount - 1; i >= 0; i--)
        {
            GameObject go = _layerOverlay.GetChild(i).gameObject;
            UIBase uiBase = go.GetComponent<UIBase>();

            if (uiBase != null)
                ApplyState(uiBase, active: false, interactable: false, blocksRaycasts: false, alpha: 0f);
            else
                go.SetActive(false);
        }
    }
}