using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

/// <summary>
/// 연출 기억을 문자열로 싣고 되읽는 법.
///
/// 연출은 저장이 없어도 세 가지를 늘 들고 있다(PLAN_playback-unit E1).
///
///   백로그          재생 단위를 넘어 이어지는 대사 기록              BacklogRecorder
///   단위의 선택 기록 이 단위를 루트부터 다시 달릴 때 자동 응답할 Yarn 선택 ChoiceHistory
///   재개 지점        단위 루트에서 이 줄까지 — 줄 좌표 + 거기까지의 선택   LineTarget + ChoiceHistory
///
/// 저장은 이것을 실어 나를 뿐이다. 진행 저장(ked-progression-runtime의 Ked.Progression.Save)은
/// 호스트 상태를 문자열 한 칸으로 받으므로, 진행 호스트는 이 세 문자열을 그 칸들에 넣는다.
/// 연출 단독 호스트도 같은 문자열로 저장한다. 이 타입은 진행을 모른다.
///
/// 형식: camelCase JSON. 바꾸면 이미 저장된 기억을 못 읽는다.
/// </summary>
public static class PresentationMemory
{
    private static readonly JsonSerializerSettings Settings = new()
    {
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new CamelCaseNamingStrategy
            {
                ProcessDictionaryKeys = false,
                OverrideSpecifiedNames = true,
            },
        },
        NullValueHandling = NullValueHandling.Include,
    };

    // 필드 이름이 곧 저장 형식이다.
    private sealed class UnitChoicesDto
    {
        public List<VNChoiceRecord> YarnChoices = new();
    }

    private sealed class BacklogDto
    {
        public List<DialogueLogEntry> Backlog = new();
    }

    private sealed class ResumePointDto
    {
        public LineTarget Target;
        public List<VNChoiceRecord> YarnChoices = new();
    }

    #region 백로그

    public static string Backlog(IReadOnlyList<DialogueLogEntry> backlog) =>
        Write(new BacklogDto { Backlog = new List<DialogueLogEntry>(backlog) });

    // 백로그 순번은 단조 증가해야 한다 — 롤백·갈라지기가 그 순번으로 단위를 찾는다.
    public static List<DialogueLogEntry> ReadBacklog(string memory)
    {
        if (string.IsNullOrEmpty(memory))
            return new List<DialogueLogEntry>();

        List<DialogueLogEntry> backlog = Read<BacklogDto>(memory, "백로그").Backlog ?? new List<DialogueLogEntry>();

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

    #region 단위의 선택 기록

    public static string UnitChoices(IReadOnlyList<VNChoiceRecord> yarnChoices) =>
        Write(new UnitChoicesDto { YarnChoices = new List<VNChoiceRecord>(yarnChoices) });

    public static List<VNChoiceRecord> ReadUnitChoices(string memory)
    {
        if (string.IsNullOrEmpty(memory))
            return new List<VNChoiceRecord>();

        return Read<UnitChoicesDto>(memory, "선택 기록").YarnChoices ?? new List<VNChoiceRecord>();
    }

    #endregion

    #region 재개 지점

    public static string ResumePoint(LineTarget target, IReadOnlyList<VNChoiceRecord> yarnChoices)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));

        return Write(new ResumePointDto
        {
            Target = new LineTarget
            {
                NodeName = target.NodeName,
                LineId = target.LineId,
                Occurrence = target.Occurrence,
            },
            YarnChoices = new List<VNChoiceRecord>(yarnChoices),
        });
    }

    public static PresentationResumePoint ReadResumePoint(string memory)
    {
        ResumePointDto dto = Read<ResumePointDto>(memory, "재개 지점");

        LineTarget target = dto.Target;
        if (target == null
            || string.IsNullOrWhiteSpace(target.NodeName)
            || string.IsNullOrWhiteSpace(target.LineId)
            || target.Occurrence < 1)
            throw new InvalidDataException("저장된 줄 좌표가 잘못됐다.");

        return new PresentationResumePoint(target, dto.YarnChoices ?? new List<VNChoiceRecord>());
    }

    #endregion

    private static string Write(object dto) => JsonConvert.SerializeObject(dto, Settings);

    private static T Read<T>(string memory, string what) where T : class
    {
        T dto;

        try
        {
            dto = JsonConvert.DeserializeObject<T>(memory, Settings);
        }
        catch (Exception error)
        {
            throw new InvalidDataException($"저장된 {what}을(를) 읽지 못했다: {error.Message}");
        }

        return dto ?? throw new InvalidDataException($"저장된 {what}이(가) 비어 있다.");
    }
}

/// <summary>재개 지점 — 단위 루트에서 이 줄까지 달리기 위해 연출이 필요로 하는 것.</summary>
public sealed class PresentationResumePoint
{
    public LineTarget Target { get; }
    public IReadOnlyList<VNChoiceRecord> YarnChoices { get; }

    public PresentationResumePoint(LineTarget target, IReadOnlyList<VNChoiceRecord> yarnChoices)
    {
        Target = target;
        YarnChoices = yarnChoices;
    }
}
