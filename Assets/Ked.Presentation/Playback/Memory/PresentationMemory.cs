using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

/// <summary>
/// 연출 기억의 문자열 형식 — 싣고 되읽는 법.
///
/// 연출은 저장이 없어도 두 가지를 늘 들고 있다(PLAN_playback-unit E1).
///
///   백로그          재생 단위를 넘어 이어지는 대사 기록              BacklogRecorder
///   재개 지점        단위 루트에서 이 줄까지 — 줄 좌표                  LineTarget
///
/// ⛔ 전에는 셋이었다 — "단위의 선택 기록"(루트부터 다시 달릴 때 자동 응답할 Yarn 선택, ChoiceHistory)은
/// 대사 안 선택지와 함께 2026-11-05에 걷혔다. 선택지는 에피소드 끝의 진행 선택지뿐이고 그 기록은 진행(Ked.Progression)의 몫이라,
/// 단위 루트부터의 재생은 줄 좌표만으로 결정된다. 옛 재개 지점의 yarnChoices 칸은 읽을 때 무시한다(저장 호환).
///
/// 저장은 이것을 실어 나를 뿐이다. 진행 저장(ked-progression-runtime의 Ked.Progression.Save)은
/// 호스트 상태를 문자열 한 칸으로 받으므로, 진행 호스트는 이 문자열들을 그 칸들에 넣는다. 이 타입은 진행을 모른다.
///
/// 호스트는 이 형식을 직접 부르지 않는다 — 입구는 PlaybackMemory다(그래서 internal).
///
/// 형식: camelCase JSON. 바꾸면 이미 저장된 기억을 못 읽는다.
/// </summary>
internal static class PresentationMemory
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
    private sealed class BacklogDto
    {
        public List<DialogueLogEntry> Backlog = new();
    }

    // 옛 저장의 yarnChoices 칸은 모르는 필드로 흘려보낸다(역직렬화는 모르는 필드를 무시한다).
    private sealed class ResumePointDto
    {
        public LineTarget Target;
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

    #region 재개 지점

    public static string ResumePoint(LineTarget target)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));

        return Write(new ResumePointDto
        {
            Target = new LineTarget
            {
                LineId = target.LineId,
                Occurrence = target.Occurrence,
            },
        });
    }

    public static LineTarget ReadResumePoint(string memory)
    {
        ResumePointDto dto = Read<ResumePointDto>(memory, "재개 지점");

        LineTarget target = dto.Target;
        if (target == null
            || string.IsNullOrWhiteSpace(target.LineId)
            || target.Occurrence < 1)
            throw new InvalidDataException("저장된 줄 좌표가 잘못됐다.");

        return target;
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
