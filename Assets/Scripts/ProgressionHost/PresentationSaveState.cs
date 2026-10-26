using System;
using System.Collections.Generic;
using System.IO;
using Ked.Progression.Save;

// 진행 저장(Ked.Progression.Save)에 연출 상태를 싣는 법.
//
// 진행 겹은 챕터·장면·경로·스탯만 알고, 연출이 같은 시점에 보관해야 하는 것은
// HostState 문자열 한 칸으로 받는다. 그 문자열을 만들고 되읽는 자리가 여기다.
// 연출은 저장이 없어도 백로그·줄 위치·Yarn 선택을 늘 들고 있다 — 저장은 그것을 실어 나를 뿐이다.
//
// 세 자리:
//   장면(SceneRecord.HostState)       그 장면을 다시 달릴 때 필요한 Yarn 선택 기록
//   회차(PlaythroughSave.HostState)    장면을 넘어 이어지는 백로그
//   재생 계획(SavedLoadPlan.HostState) 저장한 줄 좌표 + 거기까지 가는 Yarn 선택 기록
public static class PresentationSaveState
{
    private sealed class SceneDto
    {
        public List<VNChoiceRecord> YarnChoices = new();
    }

    private sealed class PlaythroughDto
    {
        public List<DialogueLogEntry> Backlog = new();
    }

    private sealed class LoadDto
    {
        public SaveLineTarget Target;
        public List<VNChoiceRecord> YarnChoices = new();
    }

    #region 장면

    public static string Scene(IReadOnlyList<VNChoiceRecord> yarnChoices) =>
        SaveJson.Serialize(new SceneDto { YarnChoices = new List<VNChoiceRecord>(yarnChoices) });

    public static List<VNChoiceRecord> ReadScene(string hostState)
    {
        if (string.IsNullOrEmpty(hostState))
            return new List<VNChoiceRecord>();

        SceneDto dto = Read<SceneDto>(hostState, "장면");
        return dto.YarnChoices ?? new List<VNChoiceRecord>();
    }

    #endregion

    #region 회차

    public static string Playthrough(IReadOnlyList<DialogueLogEntry> backlog) =>
        SaveJson.Serialize(new PlaythroughDto { Backlog = new List<DialogueLogEntry>(backlog) });

    // 백로그 순번은 단조 증가해야 한다 — 롤백·갈라지기가 그 순번으로 장면을 찾는다.
    public static List<DialogueLogEntry> ReadPlaythrough(string hostState)
    {
        if (string.IsNullOrEmpty(hostState))
            return new List<DialogueLogEntry>();

        PlaythroughDto dto = Read<PlaythroughDto>(hostState, "회차");
        List<DialogueLogEntry> backlog = dto.Backlog ?? new List<DialogueLogEntry>();

        int previous = -1;
        foreach (DialogueLogEntry entry in backlog)
        {
            if (entry.lineSequence < 0 || entry.lineSequence <= previous)
                throw new InvalidDataException("저장된 백로그 순번이 잘못됐다.");
            previous = entry.lineSequence;
        }

        return backlog;
    }

    #endregion

    #region 재생 계획

    public static string Load(SaveLineTarget target, IReadOnlyList<VNChoiceRecord> yarnChoices)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));

        return SaveJson.Serialize(new LoadDto
        {
            Target = new SaveLineTarget
            {
                NodeName = target.NodeName,
                LineId = target.LineId,
                Occurrence = target.Occurrence,
            },
            YarnChoices = new List<VNChoiceRecord>(yarnChoices),
        });
    }

    public static PresentationLoadPlan ReadLoad(string hostState)
    {
        LoadDto dto = Read<LoadDto>(hostState, "재생 계획");

        SaveLineTarget target = dto.Target;
        if (target == null
            || string.IsNullOrWhiteSpace(target.NodeName)
            || string.IsNullOrWhiteSpace(target.LineId)
            || target.Occurrence < 1)
            throw new InvalidDataException("저장된 줄 좌표가 잘못됐다.");

        return new PresentationLoadPlan(target, dto.YarnChoices ?? new List<VNChoiceRecord>());
    }

    #endregion

    private static T Read<T>(string hostState, string what) where T : class
    {
        T dto;

        try
        {
            dto = SaveJson.Deserialize<T>(hostState);
        }
        catch (Exception error)
        {
            throw new InvalidDataException($"저장된 {what}의 연출 상태를 읽지 못했다: {error.Message}");
        }

        return dto ?? throw new InvalidDataException($"저장된 {what}의 연출 상태가 비어 있다.");
    }
}

// 장면 루트에서 저장한 줄까지 달리기 위해 연출이 필요로 하는 것.
public sealed class PresentationLoadPlan
{
    public SaveLineTarget Target { get; }
    public IReadOnlyList<VNChoiceRecord> YarnChoices { get; }

    public PresentationLoadPlan(SaveLineTarget target, IReadOnlyList<VNChoiceRecord> yarnChoices)
    {
        Target = target;
        YarnChoices = yarnChoices;
    }
}
