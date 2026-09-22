using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ked.Progression;
using UnityEngine;

// 수동 저장/불러오기와 과거 지점 갈라지기 흐름.
// 화면은 슬롯이나 백로그 한 줄만 전달하고,
// 실제 저장 데이터 캡처와 회차 전환은 여기서 조립한다.
public sealed class ManualSaveFlow
{
    private readonly SaveCoordinator _saveCoordinator;
    private readonly SaveSlotService _saveSlots;
    private readonly PlaythroughForkService _fork;
    private readonly ProgressionLauncher _progressionLauncher;
    private readonly VNFeatureController _vnFeatures;

    public bool CanSave => _progressionLauncher.IsRunning;

    public ManualSaveFlow(
        SaveCoordinator saveCoordinator,
        SaveSlotService saveSlots,
        PlaythroughForkService fork,
        ProgressionLauncher progressionLauncher,
        VNFeatureController vnFeatures)
    {
        _saveCoordinator = saveCoordinator;
        _saveSlots = saveSlots;
        _fork = fork;
        _progressionLauncher = progressionLauncher;
        _vnFeatures = vnFeatures;
    }

    public bool TryCreate()
    {
        if (!TryCapture(out ManualSaveData save))
            return false;

        SaveSlotEntry slot = _saveSlots.Create(
            _saveCoordinator.Capture(),
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
            _saveSlots.Find(slotId);

        if (!TryCapture(out ManualSaveData save))
            return false;

        _saveSlots.Overwrite(
            slot.Id,
            _saveCoordinator.Capture(),
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
            SaveSlotEntry slot = _saveSlots.Find(slotId);
            SaveSlotData data = _saveSlots.Load(slot.Id);

            await _progressionLauncher.TransitionAndResumeAsync(() =>
                {
                    onTransitionStarted?.Invoke();

                    _fork.ForkFromSaveSlot(slot, data);
                });
        }
        catch (Exception error) { Debug.LogError($"[수동 저장] 불러오기 실패\n{error}"); }
    }

    // 백로그 한 줄이 이미 확정된 Scene의 것이면 그 지점에서 갈라질 수 있다.
    public bool CanForkFrom(in DialogueLogEntry entry) =>
        _fork.CanForkFrom(_saveCoordinator.Capture(), entry);

    // 확정된 과거 Scene으로 되돌아간다.
    // 되돌아갈 지점을 먼저 확정하고, 그 상태 그대로 새 회차를 만든다.
    public async Task ForkFromBacklogAsync(
        DialogueLogEntry entry,
        Action onTransitionStarted = null)
    {
        PlaythroughSaveSnapshot playthrough = _saveCoordinator.Capture();

        if (!_fork.TryResolveForkTarget(playthrough, entry, out SaveForkTarget forkTarget))
            return;

        onTransitionStarted?.Invoke();

        await _progressionLauncher.TransitionAndResumeAsync(
            () => _fork.ForkFromScene(playthrough, forkTarget));
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
