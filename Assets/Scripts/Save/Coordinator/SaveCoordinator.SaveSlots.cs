using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed partial class SaveCoordinator
{
    public IReadOnlyList<SaveSlotEntry> SaveSlots => _localStore.LoadSaveSlotIndex().Slots;

    public SaveSlotData LoadSaveSlot(string id)
    {
        SaveSlotData data = _localStore.LoadSaveSlot(id);
        if (data == null) return null;
        if (string.IsNullOrEmpty(data.ContentVersion))
            data.ContentVersion = _contentVersion;
        if (!string.Equals(data.ContentVersion, _contentVersion, StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"수동 저장의 콘텐츠 버전이 다르다: {data.ContentVersion} → {_contentVersion}");
        return data;
    }

    public SaveSlotEntry DuplicateSaveSlot(string id, string label = null)
    {
        SaveSlotEntry source = FindSaveSlot(id);
        SaveSlotData data = LoadSaveSlot(id);
        if (source == null || data == null) return null;

        string newId = NewPlaythroughId();
        data.Id = newId;
        data.ContentVersion = _contentVersion;

        var entry = new SaveSlotEntry
        {
            Id = newId,
            Label = label ?? source.Label,
            Preview = source.Preview,
            ChapterId = source.ChapterId,
            SavedAtUtc = NowUtc(),
            PlaySeconds = source.PlaySeconds,
        };
        _localStore.WriteSaveSlot(entry, data);
        return entry;
    }

    public SaveSlotEntry CreateSaveSlot(
        IReadOnlyList<CommittedChoice> path, IReadOnlyList<VNChoiceRecord> yarnChoices,
        SaveLineTarget target, string preview, string label = null) =>
        WriteSaveSlot(null, path, yarnChoices, target, preview, label);

    public SaveSlotEntry OverwriteSaveSlot(
        string id, IReadOnlyList<CommittedChoice> path, IReadOnlyList<VNChoiceRecord> yarnChoices,
        SaveLineTarget target, string preview, string label = null) =>
        WriteSaveSlot(id ?? throw new ArgumentNullException(nameof(id)), path, yarnChoices, target, preview, label);

    private SaveSlotEntry WriteSaveSlot(
        string id, IReadOnlyList<CommittedChoice> path, IReadOnlyList<VNChoiceRecord> yarnChoices,
        SaveLineTarget target, string preview, string label)
    {
        if (_currentEntry == null || target == null)
            return null;

        SaveSlotEntry previous = id == null ? null : FindSaveSlot(id);
        if (id != null && previous == null)
            throw new InvalidOperationException("덮어쓸 수동 저장이 없다.");

        string slotId = id ?? NewPlaythroughId();
        string now = NowUtc();
        var entry = new SaveSlotEntry
        {
            Id = slotId,
            Label = string.IsNullOrEmpty(label) ? preview : label,
            Preview = preview,
            ChapterId = _currentEntry.ChapterId,
            SavedAtUtc = now,
            PlaySeconds = TotalSeconds,
        };
        var data = new SaveSlotData
        {
            Id = slotId,
            ContentVersion = _contentVersion,
            Scenes = PlaythroughSession.Copy(_scenes),
            Checkpoint = PlaythroughSession.Copy(_currentEntry),
            LoadPlan = new SavedLoadPlan
            {
                Path = path.Select(choice => new SavedChoice
                {
                    FromEpisodeId = choice.FromEpisodeId,
                    OptionIndex = choice.OptionIndex,
                }).ToList(),
                YarnChoices = new List<VNChoiceRecord>(yarnChoices),
                Target = PlaythroughSession.Copy(target),
            },
            Backlog = _localStore.LoadActive()?.Backlog is List<DialogueLogEntry> backlog
                ? new List<DialogueLogEntry>(backlog)
                : new List<DialogueLogEntry>(),
            PlaySeconds = TotalSeconds,
        };

        _localStore.WriteSaveSlot(entry, data);

        Debug.Log(
            $"[수동 저장] \"{entry.Preview}\" @ {target.NodeName}/{target.LineId}#{target.Occurrence}, " +
            $"경로 {data.LoadPlan.Path.Count}개, Yarn 선택 {data.LoadPlan.YarnChoices.Count}개");
        return entry;
    }

    public bool DeleteSaveSlot(string id)
    {
        SaveSlotIndexFile index = _localStore.LoadSaveSlotIndex();
        if (index.Slots.RemoveAll(slot => slot.Id == id) == 0) return false;
        _localStore.WriteSaveSlotIndex(index);
        return true;
    }

    public bool RenameSaveSlot(string id, string label)
    {
        SaveSlotIndexFile index = _localStore.LoadSaveSlotIndex();
        SaveSlotEntry entry = index.Slots.Find(slot => slot.Id == id);
        if (entry == null) return false;
        entry.Label = string.IsNullOrEmpty(label) ? entry.Preview : label;
        _localStore.WriteSaveSlotIndex(index);
        return true;
    }

    private SaveSlotEntry FindSaveSlot(string id) =>
        _localStore.LoadSaveSlotIndex().Slots.Find(slot => slot.Id == id);
}
