using System;
using UnityEngine;

public partial class UIManager
{
    public T ShowTop<T>(Action<T> afterPatched = null)
        where T : UIBase, IUITop
    {
        if (!TryResolve("Top", out T top))
            return null;

        Mount(top, _layerTop);

        // top은 보통 입력을 막는 로딩/블랙/모달
        ApplyState(top, active: false, interactable: false, blocksRaycasts: false, alpha: 0f);

        ApplyState(top, active: true, interactable: true, blocksRaycasts: true, alpha: 1f);
        // InvokeAfterPatch(top, () =>
        // {
        //     afterPatched?.Invoke(top);
        // });

        return top;
    }

    public void HideTop<T>() where T : UIBase
    {
        T top = GetUI<T>();
        ApplyState(top, active: false, interactable: false, blocksRaycasts: false, alpha: 0f);
    }

    public void ClearTop()
    {
        if (_layerTop == null) return;

        for (int i = _layerTop.childCount - 1; i >= 0; i--)
        {
            GameObject go = _layerTop.GetChild(i).gameObject;
            UIBase uiBase = go.GetComponent<UIBase>();

            if (uiBase != null)
                ApplyState(uiBase, active: false, interactable: false, blocksRaycasts: false, alpha: 0f);
            else
                go.SetActive(false);
        }
    }
}