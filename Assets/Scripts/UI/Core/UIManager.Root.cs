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

        // 숨기기 전에 티켓을 무효화한다. 로더가 비동기가 되면 이미 숨긴 UI에 패치가 적용될 수 있기 때문.
        BumpShowVersion();

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