using System;

public partial class UIManager
{
    public void SwitchRoot<T>(
        Action<T> afterPatched = null,
        bool forceRefresh = false)
        where T : UIBase, IUIRoot
    {
        if (!TryResolve("Root", out T root))
            return;

        bool sameRoot = CurSceneRoot == root;

        if (sameRoot && !forceRefresh)
        {
            afterPatched?.Invoke(root);
            return;
        }

        if (CurSceneRoot != null && !sameRoot)
            HideManagedUI(CurSceneRoot);

        CurSceneRoot = root;

        Mount(root, _layerUIRoot);

        if (!sameRoot)
        {
            ApplyState(
                root,
                active: false,
                interactable: false,
                blocksRaycasts: false,
                alpha: 0f);
        }

        InvokeAfterPatch(root, () =>
        {
            ApplyState(
                root,
                active: true,
                interactable: true,
                blocksRaycasts: true,
                alpha: 1f);

            afterPatched?.Invoke(root);
        });
    }
}