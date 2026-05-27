using UnityEngine;

public sealed partial class VnScreenBindings
{
    private SaveLoadMenuMode _currentSaveLoadMode;
    private bool IsSaveMode => _currentSaveLoadMode == SaveLoadMenuMode.Save;
    private bool IsLoadMode => _currentSaveLoadMode == SaveLoadMenuMode.Load;
    

    private void GoToSaveMenu()
    {
        OpenSaveLoadMenu(SaveLoadMenuMode.Save);
    }

    private void GoToLoadMenu()
    {
        OpenSaveLoadMenu(SaveLoadMenuMode.Load);
    }

    private void OpenSaveLoadMenu(SaveLoadMenuMode mode)
    {
        _currentSaveLoadMode = mode;

        UI.PushPanel<SaveLoadMenuUIPanel>(saveLoadRoot =>
        {
            BindPanel(saveLoadRoot, ApplyBindings);
            RefreshSaveLoadPanel(saveLoadRoot);
        });
    }

    private void ApplyBindings(SaveLoadMenuUIPanel saveLoadRoot)
    {
        AddBinding(saveLoadRoot,
            r => r.SlotClicked += HandleSlotClicked,
            r => r.SlotClicked -= HandleSlotClicked);

        AddBinding(saveLoadRoot,
            r => r.CloseClicked += OnSaveLoadCloseClicked,
            r => r.CloseClicked -= OnSaveLoadCloseClicked);
    }
    
    
    private void HandleSlotClicked(int slotIndex)
    {
        if (_currentSaveLoadMode == SaveLoadMenuMode.Save)
        {
            if (!_vnSaveLoadSystem.SaveService.SaveManual(slotIndex))
            {
                Debug.LogWarning($"[VnScreenBindings] Save failed. slotIndex={slotIndex}");
                return;
            }
        }
        else
        {
            if (!_vnSaveLoadSystem.LoadService.Load(slotIndex))
            {
                Debug.LogWarning($"[VnScreenBindings] Load failed. slotIndex={slotIndex}");
                return;
            }
        }

        RefreshSaveLoadPanel(UIManager.Instance.GetUI<SaveLoadMenuUIPanel>());
    }
    
    private void OnSaveLoadCloseClicked()
    {
        CloseTopPanel();
    }
    
    private void RefreshSaveLoadPanel(SaveLoadMenuUIPanel saveLoadPanel)
    {
        VNSaveSlotMeta[] metas = _vnSaveLoadSystem.GetAllSaveSlotMetas();

        saveLoadPanel.Rebuild(_currentSaveLoadMode, metas);
    }
}