using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 연출 기억의 입구 — 꺼내기와 되돌리기.
///
/// 연출은 저장이 없어도 기억을 들고 있다(PLAN_playback-unit E1). 호스트는 이 입구로 그 기억을 문자열로 꺼내
/// 어딘가에 두었다가, 같은 문자열로 되돌린다. 문자열의 생김새는 PresentationMemory가 정하고,
/// 호스트는 백로그·선택 기록·줄 좌표가 어떻게 생겼는지 모른다.
///
///   꺼내기
///     CaptureBacklog             백로그 전부. 단위가 끝난 자리에서 꺼내면 = 다음 단위 루트 앞까지
///     CaptureBacklogBeforeUnit   지금 단위 루트 앞까지. 단위 한가운데서 저장할 때
///     CaptureUnitChoices         지금 단위의 Yarn 선택 기록. 나중에 이 단위에서 갈라질 때 쓰인다
///     TryCaptureResumePoint      지금 줄까지 — 줄 좌표 + 거기까지의 선택
///     NextSerial                 다음 줄이 받을 순번. 호스트가 단위 경계를 이 순번으로 적는다
///
///   되돌리기
///     Forget                     처음부터 — 백로그를 비우고 재개 예약을 지운다
///     Restore                    백로그 + 재개 지점. 재개 지점은 다음에 열리는 단위가 가져간다
///
///   과거 단위
///     ReadPastUnit               백로그 한 줄 → 그 단위가 물려받을 백로그 + 그 줄의 재개 지점.
///                                과거 단위의 루트를 보관하는 일은 진행의 몫이다(E3) — 여기는 줄을 세는 일만 한다
///
/// 두 예제가 같은 문자열을 쓴다. 진행·저장 예제는 진행 저장의 HostState 칸에,
/// 연출 단독 예제는 노드 이름과 함께 파일 하나에 둔다.
/// </summary>
public sealed class PlaybackMemory
{
    private readonly PlaybackUnit _unit;
    private readonly BacklogRecorder _backlog;
    private readonly ChoiceHistory _choices;
    private readonly RollbackHistory _rollback;

    internal PlaybackMemory(
        PlaybackUnit unit,
        BacklogRecorder backlog,
        ChoiceHistory choices,
        RollbackHistory rollback)
    {
        _unit = unit;
        _backlog = backlog;
        _choices = choices;
        _rollback = rollback;
    }

    #region 꺼내기

    /// <summary>
    /// 다음 줄이 받을 순번(백로그 순번). 되감기는 지금 단위 안의 꼬리만 자르므로
    /// 단위 경계에서 적은 순번은 흔들리지 않는다.
    /// </summary>
    public int NextSerial => _backlog.NextLineSequence;

    public string CaptureBacklog() =>
        PresentationMemory.Backlog(_backlog.Entries);

    public string CaptureBacklogBeforeUnit()
    {
        var before = new List<DialogueLogEntry>();

        IReadOnlyList<DialogueLogEntry> entries = _backlog.Entries;

        for (int i = 0; i < entries.Count; i++)
            if (!_backlog.IsInCurrentScene(entries[i]))
                before.Add(entries[i]);

        return PresentationMemory.Backlog(before);
    }

    public string CaptureUnitChoices() =>
        PresentationMemory.UnitChoices(_choices.CreateChoiceSnapshot());

    /// <summary>
    /// 지금 보이는 줄까지의 재개 지점. 시크 중이거나 아직 보인 줄이 없으면 false.
    /// preview는 그 줄의 대사 — 저장 슬롯 미리보기에 쓴다.
    /// </summary>
    public bool TryCaptureResumePoint(out string resumePoint, out string preview)
    {
        resumePoint = null;
        preview = null;

        if (_unit.IsSeeking || _rollback.Points.Count == 0)
            return false;

        RollbackPoint point = _rollback.Points[^1];

        var target = new LineTarget
        {
            NodeName = point.nodeName,
            LineId = point.lineId,
            Occurrence = point.occurrence,
        };

        resumePoint = PresentationMemory.ResumePoint(target, _choices.CreateChoiceSnapshot());
        preview = point.rawText;
        return true;
    }

    #endregion

    #region 되돌리기

    /// <summary>처음부터 — 백로그를 비우고, 다음 단위는 루트에서 그냥 시작한다.</summary>
    public void Forget()
    {
        _backlog.Restore(null);
        _unit.StageResume(null);
    }

    /// <summary>
    /// 꺼내 두었던 기억으로 되돌린다.
    /// backlog: 단위 루트 앞까지의 백로그(빈 문자열이면 빈 백로그).
    /// resumePoint: 재개 지점. null이면 다음 단위는 루트에서 시작한다.
    ///
    /// 둘 다 읽은 뒤에야 바꾼다 — 하나라도 깨져 있으면 InvalidDataException을 던지고 아무것도 바꾸지 않는다.
    /// </summary>
    public void Restore(string backlog, string resumePoint)
    {
        List<DialogueLogEntry> entries = PresentationMemory.ReadBacklog(backlog);

        PresentationResumePoint resume = resumePoint == null
            ? null
            : PresentationMemory.ReadResumePoint(resumePoint);

        _backlog.Restore(entries);
        _unit.StageResume(resume);
    }

    #endregion

    #region 과거 단위

    /// <summary>
    /// 백로그 한 줄을 과거 단위의 재개 기억으로 읽는다.
    ///
    /// unitStartSerial: 그 단위가 시작된 순번(호스트가 단위 경계에 적어 둔 NextSerial).
    /// unitChoices: 그 단위가 끝날 때 꺼내 둔 CaptureUnitChoices.
    ///
    /// inheritedBacklog: 그 단위 앞까지의 백로그 — 그 단위를 다시 여는 쪽이 물려받는다.
    /// resumePoint: 그 줄까지의 재개 지점. 줄을 셀 수 없거나(기록이 잘려 나갔다) 선택 기록을 읽지 못하면 null —
    ///              그 단위의 루트에서 시작하면 된다.
    /// </summary>
    public void ReadPastUnit(
        in DialogueLogEntry line,
        int unitStartSerial,
        string unitChoices,
        out string inheritedBacklog,
        out string resumePoint)
    {
        IReadOnlyList<DialogueLogEntry> entries = _backlog.Entries;

        inheritedBacklog = PresentationMemory.Backlog(BacklogForkPoints.Before(entries, unitStartSerial));
        resumePoint = null;

        if (!BacklogForkPoints.TryMakeLineTarget(entries, line, unitStartSerial, out LineTarget target))
            return;

        List<VNChoiceRecord> choices;

        try
        {
            choices = PresentationMemory.ReadUnitChoices(unitChoices);
        }
        catch (InvalidDataException error)
        {
            Debug.LogWarning($"[연출 기억] 과거 단위의 선택 기록을 읽지 못해 그 단위의 루트에서 시작한다. — {error.Message}");
            return;
        }

        resumePoint = PresentationMemory.ResumePoint(target, choices);
    }

    #endregion
}
