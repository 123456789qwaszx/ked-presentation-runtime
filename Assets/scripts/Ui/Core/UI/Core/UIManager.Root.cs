using System;

public partial class UIManager
{
    public void SwitchRoot<T>(Action<T> afterPatched = null)
        where T : UIBase, IUIRoot
    {
        if (!TryResolve("Root", out T root))
            return;

        if (CurSceneRoot != null)
            CurSceneRoot.gameObject.SetActive(false);

        CurSceneRoot = root;

        Mount(root, _layerUIRoot);

        ApplyState(root, active: true, interactable: true, blocksRaycasts: true, alpha: 1f);
        afterPatched?.Invoke(root);
        
        // 패치 전 깜빡임 방지: 비활성/투명
        //ApplyState(root, active: false, interactable: false, blocksRaycasts: false, alpha: 0f);

        // InvokeAfterPatch(root, () =>
        // {
        //     ApplyState(root, active: true, interactable: true, blocksRaycasts: true, alpha: 1f);
        //     afterPatched?.Invoke(root);
        // });
    }
}