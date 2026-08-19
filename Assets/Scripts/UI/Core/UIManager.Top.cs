using System;
using UnityEngine;

public partial class UIManager
{
    public T ShowTop<T>(Action<T> afterPatched = null)
        where T : UIBase, IUITop
    {
        if (!TryResolve("Top", out T top))
            return null;

        BumpShowVersion();

        Mount(top, _layerTop);

        ApplyState(
            top,
            active: false,
            interactable: false,
            blocksRaycasts: false,
            alpha: 0f);

        InvokeAfterPatch(top, () =>
        {
            ApplyState(
                top,
                active: true,
                interactable: true,
                blocksRaycasts: true,
                alpha: 1f);

            afterPatched?.Invoke(top);
        });

        return top;
    }

    public void HideTop<T>() where T : UIBase
    {
        T top = GetUI<T>();

        // 숨기기 전에 티켓을 무효화한다. 로더가 비동기가 되면 이미 숨긴 UI에 패치가 적용될 수 있다.
        BumpShowVersion();

        HideManagedUI(top);
    }

    public void ClearTop()
    {
        if (_layerTop == null)
            return;

        BumpShowVersion();

        for (int i = _layerTop.childCount - 1; i >= 0; i--)
        {
            GameObject go = _layerTop.GetChild(i).gameObject;
            UIBase uiBase = go.GetComponent<UIBase>();

            if (uiBase != null)
                HideManagedUI(uiBase);
            else
                go.SetActive(false);
        }
    }
}