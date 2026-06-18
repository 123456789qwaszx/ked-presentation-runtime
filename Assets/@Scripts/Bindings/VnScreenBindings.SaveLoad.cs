using UnityEngine;

public sealed partial class VnScreenBindings
{
    private SaveLoadMenuMode _currentSaveLoadMode;
    
    private void OpenSaveLoadMenu(SaveLoadMenuMode mode)
    {
        _currentSaveLoadMode = mode;

        UI.PushPanel<SaveLoadMenuUIPanel>(panel =>
        {
            BindPanel(panel, ApplyBindings);
            Refresh(panel);
        });
    }

    private void ApplyBindings(SaveLoadMenuUIPanel panel)
    {
        AddBinding(panel,
            p => p.SlotClicked += HandleSlotClicked,
            p => p.SlotClicked -= HandleSlotClicked);

        AddBinding(panel,
            p => p.CloseClicked += ClosePanel,
            p => p.CloseClicked -= ClosePanel);
    }
    
    
    private void HandleSlotClicked(int slotIndex)
    {
        if (_currentSaveLoadMode == SaveLoadMenuMode.Save)
        {
            if (!_vnSaveLoadSystem.SaveService.SaveManual(slotIndex))
                return;
        }
        else
        {
            if (!_vnSaveLoadSystem.LoadService.Load(slotIndex))
                return;
            
            CloseAllPanels();
            return;
        }

        Refresh(UIManager.Instance.GetUI<SaveLoadMenuUIPanel>());
    }
    
    private void Refresh(SaveLoadMenuUIPanel saveLoadPanel)
    {
        VNSaveSlotMeta[] metas = _vnSaveLoadSystem.GetAllSaveSlotMetas();

        saveLoadPanel.Rebuild(_currentSaveLoadMode, metas);
    }
}