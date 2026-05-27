using UnityEngine;

public sealed partial class VnScreenBindings
{
    private SaveLoadMenuMode _currentSaveLoadMode;

    public void GoToSaveMenu()
    {
        OpenSaveLoadMenu(SaveLoadMenuMode.Save);
    }

    public void GoToLoadMenu()
    {
        OpenSaveLoadMenu(SaveLoadMenuMode.Load);
    }

    private void OpenSaveLoadMenu(SaveLoadMenuMode mode)
    {
        _currentSaveLoadMode = mode;

        UI.SwitchRoot<SaveLoadMenuUIPanel>(root =>
        {
            BindMain(root, BindSaveLoadRoot);
        });
    }

    private void BindSaveLoadRoot(SaveLoadMenuUIPanel saveLoadRoot)
    {
        BindEvent(
            saveLoadRoot,
            r => r.OnSlotSelected += OnSaveLoadSlotSelected,
            r => r.OnSlotSelected -= OnSaveLoadSlotSelected);

        BindEvent(
            saveLoadRoot,
            r => r.OnCloseRequested += OnSaveLoadCloseRequested,
            r => r.OnCloseRequested -= OnSaveLoadCloseRequested);

        RefreshSaveLoadRoot(saveLoadRoot);
    }

    private void RefreshSaveLoadRoot(SaveLoadMenuUIPanel saveLoadRoot)
    {
        if (saveLoadRoot == null)
            return;

        if (_vnSaveLoadSystem == null)
        {
            Debug.LogWarning("[VnScreenBindings] VNSaveLoadSystem is null.");
            return;
        }

        VNSaveSlotMeta[] metas = _vnSaveLoadSystem.GetAllSaveSlotMetas();

        saveLoadRoot.Rebuild(
            _currentSaveLoadMode,
            metas);
    }

    private void OnSaveLoadSlotSelected(int slotIndex)
    {
        if (_currentSaveLoadMode == SaveLoadMenuMode.Save)
        {
            HandleSaveSlotSelected(slotIndex);
            return;
        }

        HandleLoadSlotSelected(slotIndex);
    }

    private void HandleSaveSlotSelected(int slotIndex)
    {
        if (_vnSaveLoadSystem == null)
        {
            Debug.LogWarning("[VnScreenBindings] VNSaveLoadSystem is null.");
            return;
        }

        if (!_vnSaveLoadSystem.SaveService.SaveManual(slotIndex))
        {
            Debug.LogWarning($"[VnScreenBindings] Save failed. slotIndex={slotIndex}");
            return;
        }

        if (_boundMain is SaveLoadMenuUIPanel saveLoadRoot)
            RefreshSaveLoadRoot(saveLoadRoot);
    }

    private void HandleLoadSlotSelected(int slotIndex)
    {
        if (_vnSaveLoadSystem == null)
        {
            Debug.LogWarning("[VnScreenBindings] VNSaveLoadSystem is null.");
            return;
        }

        if (!_vnSaveLoadSystem.LoadService.Load(slotIndex))
        {
            Debug.LogWarning($"[VnScreenBindings] Load failed. slotIndex={slotIndex}");
            return;
        }
    }

    private void OnSaveLoadCloseRequested()
    {
        GoToTitle();
    }
}