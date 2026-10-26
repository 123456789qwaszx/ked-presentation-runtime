using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Ked.Progression;
using Ked.Progression.Save;
using UnityEngine;

// 수동 저장/불러오기와 과거 지점 갈라지기 흐름.
// 화면은 슬롯이나 백로그 한 줄만 전달하고,
// 실제 저장 데이터 캡처와 회차 전환은 여기서 조립한다.
//
// 진행 저장은 연출 상태를 문자열로만 받는다. 그 문자열을 만드는 일(PresentationMemory)과
// 백로그 한 줄을 장면 안의 줄 좌표로 바꾸는 일은 여기 — 연출 쪽 — 의 몫이다.
public sealed class ManualSaveFlow
{
    private readonly SaveCoordinator _saveCoordinator;
    private readonly SaveSlotService _saveSlots;
    private readonly PlaythroughForkService _fork;
    private readonly ProgressionLauncher _progressionLauncher;
    private readonly VNFeatureController _vnFeatures;
    private readonly BacklogRecorder _backlog;

    public bool CanSave => _progressionLauncher.IsRunning;

    public ManualSaveFlow(
        SaveCoordinator saveCoordinator,
        SaveSlotService saveSlots,
        PlaythroughForkService fork,
        ProgressionLauncher progressionLauncher,
        VNFeatureController vnFeatures,
        BacklogRecorder backlog)
    {
        _saveCoordinator = saveCoordinator;
        _saveSlots = saveSlots;
        _fork = fork;
        _progressionLauncher = progressionLauncher;
        _vnFeatures = vnFeatures;
        _backlog = backlog;
    }

    #region 수동 슬롯

    public bool TryCreate()
    {
        if (!TryCapture(out ManualSaveData save))
            return false;

        SaveSlotEntry slot = _saveSlots.Create(
            _saveCoordinator.Capture(),
            save.Path,
            save.LoadHostState,
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
            save.LoadHostState,
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

    #endregion

    #region 백로그에서 갈라지기

    // 백로그 한 줄이 이미 확정된 장면의 것이면 그 지점에서 갈라질 수 있다.
    public bool CanForkFrom(in DialogueLogEntry entry) =>
        _fork.CanForkFrom(_saveCoordinator.Capture(), entry.lineSequence);

    // 확정된 과거 장면으로 되돌아간다.
    // 되돌아갈 지점을 먼저 확정하고, 그 상태 그대로 새 회차를 만든다.
    public async Task ForkFromBacklogAsync(
        DialogueLogEntry entry,
        Action onTransitionStarted = null)
    {
        PlaythroughSaveSnapshot playthrough = _saveCoordinator.Capture();

        int sceneIndex = playthrough.FindSceneIndexBySerial(entry.lineSequence);

        if (sceneIndex < 0)
            return;

        SceneRecord origin = playthrough.Scenes[sceneIndex];
        int sceneStart = origin.Checkpoint.HostSerialStart;

        // 그 장면이 시작되기 전까지의 백로그만 새 회차가 물려받는다.
        string inheritedHostState =
            PresentationMemory.Backlog(BacklogForkPoints.Before(_backlog.Entries, sceneStart));

        // 그 줄이 장면 안에서 몇 번째 등장인지 세어 줄 좌표를 만든다.
        // 셀 수 없으면(기록이 잘려 나갔으면) 장면 루트부터 일반 플레이.
        string loadHostState = null;

        if (BacklogForkPoints.TryMakeLineTarget(_backlog.Entries, entry, sceneStart, out LineTarget target))
        {
            IReadOnlyList<VNChoiceRecord> yarnChoices;

            try
            {
                yarnChoices = PresentationMemory.ReadUnitChoices(origin.HostState);
            }
            catch (InvalidDataException error)
            {
                Debug.LogWarning($"[저장] 장면 {sceneIndex}의 연출 기록을 읽지 못해 장면 루트에서 시작한다. — {error.Message}");
                yarnChoices = null;
            }

            if (yarnChoices != null)
                loadHostState = PresentationMemory.ResumePoint(target, yarnChoices);
        }

        onTransitionStarted?.Invoke();

        await _progressionLauncher.TransitionAndResumeAsync(
            () => _fork.ForkFromScene(playthrough, sceneIndex, inheritedHostState, loadHostState));
    }

    #endregion

    private bool TryCapture(out ManualSaveData save)
    {
        save = null;

        if (!_progressionLauncher.IsRunning)
            return false;

        if (!_vnFeatures.TryGetCurrentLine(
                out LineTarget target,
                out string preview))
        {
            Debug.Log("[수동 저장] 지금은 저장할 수 있는 대사 위치가 아니다.");
            return false;
        }

        save = new ManualSaveData(
            _progressionLauncher.PendingPath,
            PresentationMemory.ResumePoint(target, _vnFeatures.CreateYarnChoiceSnapshot()),
            preview);

        return true;
    }

    private sealed class ManualSaveData
    {
        public IReadOnlyList<CommittedChoice> Path { get; }
        public string LoadHostState { get; }
        public string Preview { get; }

        public ManualSaveData(
            IReadOnlyList<CommittedChoice> path,
            string loadHostState,
            string preview)
        {
            Path = path;
            LoadHostState = loadHostState;
            Preview = preview;
        }
    }
}
