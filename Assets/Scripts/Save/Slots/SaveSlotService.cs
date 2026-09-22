using System;
using System.Collections.Generic;
using Ked.Progression;
using UnityEngine;

// 수동 저장 슬롯. 슬롯 본문은 원본 회차 없이도 복원되는 독립 데이터다.
//
// 목록/찾기/복제/이름/삭제는 저장소만 알면 되고,
// 새로 쓰거나 덮어쓸 때만 지금 진행 중인 상태의 capture가 필요하다.
public sealed class SaveSlotService
{
    private readonly ILocalSaveStore _store;
    private readonly string _contentVersion;

    public SaveSlotService(
        ILocalSaveStore store,
        string contentVersion)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));

        if (string.IsNullOrWhiteSpace(contentVersion))
            throw new ArgumentException("저장 콘텐츠 버전이 비어 있다.", nameof(contentVersion));

        _contentVersion = contentVersion;
    }

    public IReadOnlyList<SaveSlotEntry> Slots => _store.LoadSaveSlotIndex().Slots;

    public SaveSlotEntry Find(string id) =>
        _store.LoadSaveSlotIndex()
            .Slots
            .Find(slot => slot.Id == id);

    public SaveSlotData Load(string id)
    {
        SaveSlotData data = 
            _store.LoadSaveSlot(id);
        
        if (data == null)
            return null;
        
        if (string.IsNullOrEmpty(data.ContentVersion))
            data.ContentVersion = _contentVersion;
        
        if (!string.Equals(data.ContentVersion, _contentVersion, StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"수동 저장의 콘텐츠 버전이 다르다: {data.ContentVersion} → {_contentVersion}");
        
        return data;
    }

    public SaveSlotEntry Duplicate(string id, string label = null)
    {
        SaveSlotEntry source = Find(id);
        SaveSlotData data = Load(id);
        if (source == null || data == null) return null;

        string newId = SaveStamp.NewId();
        data.Id = newId;
        data.ContentVersion = _contentVersion;

        var entry = new SaveSlotEntry
        {
            Id = newId,
            Label = label ?? source.Label,
            Preview = source.Preview,
            ChapterId = source.ChapterId,
            SavedAtUtc = SaveStamp.NowUtc(),
            PlaySeconds = source.PlaySeconds,
        };
        _store.WriteSaveSlot(entry, data);
        return entry;
    }

    public SaveSlotEntry Create(
        PlaythroughSaveSnapshot playthrough,
        IReadOnlyList<CommittedChoice> path,
        IReadOnlyList<VNChoiceRecord> yarnChoices,
        SaveLineTarget target,
        string preview, 
        string label = null) => 
        Write(
            null, 
            playthrough,
            path,
            yarnChoices,
            target,
            preview,
            label);

    public SaveSlotEntry Overwrite(
        string id, PlaythroughSaveSnapshot playthrough, IReadOnlyList<CommittedChoice> path,
        IReadOnlyList<VNChoiceRecord> yarnChoices,
        SaveLineTarget target, string preview, string label = null) =>
        Write(id ?? throw new ArgumentNullException(nameof(id)), playthrough, path, yarnChoices, target, preview, label);

    private SaveSlotEntry Write(
        string id,
        PlaythroughSaveSnapshot playthrough,
        IReadOnlyList<CommittedChoice> path,
        IReadOnlyList<VNChoiceRecord> yarnChoices,
        SaveLineTarget target,
        string preview, 
        string label)
    {
        SceneCheckpoint checkpoint = playthrough.CurrentEntry;

        if (checkpoint == null || target == null)
            return null;

        SaveSlotEntry previous = id == null ? null : Find(id);
        if (id != null && previous == null)
            throw new InvalidOperationException("덮어쓸 수동 저장이 없다.");

        string slotId = id ?? SaveStamp.NewId();
        string now = SaveStamp.NowUtc();
        var entry = new SaveSlotEntry
        {
            Id = slotId,
            Label = string.IsNullOrEmpty(label) ? preview : label,
            Preview = preview,
            ChapterId = checkpoint.ChapterId,
            SavedAtUtc = now,
            PlaySeconds = playthrough.PlaySeconds,
        };
        var data = new SaveSlotData
        {
            Id = slotId,
            ContentVersion = _contentVersion,
            Scenes = new List<SceneRecord>(playthrough.Scenes),
            Checkpoint = checkpoint,
            LoadPlan = SavedLoadPlan.Create(path, yarnChoices, target),
            Backlog = _store.LoadActive()?.Backlog is List<DialogueLogEntry> backlog
                ? new List<DialogueLogEntry>(backlog)
                : new List<DialogueLogEntry>(),
            PlaySeconds = playthrough.PlaySeconds,
        };

        _store.WriteSaveSlot(entry, data);

        Debug.Log(
            $"[수동 저장] \"{entry.Preview}\" @ {target.NodeName}/{target.LineId}#{target.Occurrence}, " +
            $"경로 {data.LoadPlan.Path.Count}개, Yarn 선택 {data.LoadPlan.YarnChoices.Count}개");
        return entry;
    }

    public bool Delete(string id)
    {
        SaveSlotIndexFile index = _store.LoadSaveSlotIndex();
        if (index.Slots.RemoveAll(slot => slot.Id == id) == 0) return false;
        _store.WriteSaveSlotIndex(index);
        return true;
    }

    public bool Rename(string id, string label)
    {
        SaveSlotIndexFile index = _store.LoadSaveSlotIndex();
        SaveSlotEntry entry = index.Slots.Find(slot => slot.Id == id);
        if (entry == null) return false;
        entry.Label = string.IsNullOrEmpty(label) ? entry.Preview : label;
        _store.WriteSaveSlotIndex(index);
        return true;
    }
}
