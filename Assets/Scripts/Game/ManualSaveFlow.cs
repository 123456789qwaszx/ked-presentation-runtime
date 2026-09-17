using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ked.Progression;
using UnityEngine;

// 수동 저장/불러오기 흐름.
// 화면은 슬롯 선택만 전달하고,
// 실제 저장 데이터 캡처와 회차 전환은 여기서 처리한다.
public sealed class ManualSaveFlow
{
    private readonly SaveCoordinator _saveCoordinator;
    private readonly ProgressionLauncher _progressionLauncher;
    private readonly VNFeatureController _vnFeatures;

    public bool CanSave => _progressionLauncher.IsRunning;

    public ManualSaveFlow(
        SaveCoordinator saveCoordinator,
        ProgressionLauncher progressionLauncher,
        VNFeatureController vnFeatures)
    {
        _saveCoordinator = saveCoordinator;
        _progressionLauncher = progressionLauncher;
        _vnFeatures = vnFeatures;
    }

    public bool TryCreate()
    {
        if (!TryCapture(out ManualSaveData save))
            return false;

        SaveSlotEntry slot = _saveCoordinator.CreateSaveSlot(
            save.Path,
            save.YarnChoices,
            save.Target,
            save.Preview);

        if (slot != null)
            return true;

        Debug.LogWarning("[수동 저장] 저장하지 못했다.");

        return false;
    }

    public bool TryOverwrite(string slotId)
    {
        SaveSlotEntry slot = 
            _saveCoordinator.FindSaveSlot(slotId);
        
        if (!TryCapture(out ManualSaveData save))
            return false;

        _saveCoordinator.OverwriteSaveSlot(
            slot.Id,
            save.Path,
            save.YarnChoices,
            save.Target,
            save.Preview,
            slot.Label);

        return true;
    }

    public async Task LoadAsync(string slotId, Action onTransitionStarted = null)
    {
        try
        {
            SaveSlotEntry slot = _saveCoordinator.FindSaveSlot(slotId);
            SaveSlotData data = _saveCoordinator.LoadSaveSlot(slot.Id);

            await _progressionLauncher.TransitionAsync(() =>
                { 
                    onTransitionStarted?.Invoke(); 
                    
                    return _saveCoordinator.ForkFromSaveSlot(slot, data);
                });
        }
        catch (Exception error) { Debug.LogError($"[수동 저장] 불러오기 실패\n{error}"); }
    }

    private bool TryCapture(out ManualSaveData save)
    {
        save = null;

        if (!_progressionLauncher.IsRunning)
            return false;

        if (!_vnFeatures.TryGetCurrentLine(
                out SaveLineTarget target,
                out string preview))
        {
            Debug.Log("[수동 저장] 지금은 저장할 수 있는 대사 위치가 아니다.");
            return false;
        }

        save = new ManualSaveData(
            _progressionLauncher.PendingPath,
            _vnFeatures.CreateYarnChoiceSnapshot(),
            target,
            preview);

        return true;
    }

    private sealed class ManualSaveData
    {
        public IReadOnlyList<CommittedChoice> Path { get; }
        public IReadOnlyList<VNChoiceRecord> YarnChoices { get; }

        public SaveLineTarget Target { get; }
        public string Preview { get; }

        public ManualSaveData(
            IReadOnlyList<CommittedChoice> path,
            IReadOnlyList<VNChoiceRecord> yarnChoices,
            SaveLineTarget target,
            string preview)
        {
            Path = path;
            YarnChoices = yarnChoices;
            Target = target;
            Preview = preview;
        }
    }
}