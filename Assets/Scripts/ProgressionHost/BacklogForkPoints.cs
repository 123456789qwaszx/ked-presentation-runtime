using System;
using System.Collections.Generic;

// 백로그 한 줄을 "과거 장면의 어느 지점"으로 읽는 셈법.
//
// 어느 장면인지는 진행 저장이 호스트 순번으로 답한다(PlaythroughSaveSnapshot.FindSceneIndexBySerial).
// 그 장면 안 어디인지는 줄을 아는 연출만 셀 수 있다 — 여기다.
public static class BacklogForkPoints
{
    // 장면이 시작되기 전까지의 줄. 갈라진 새 회차가 물려받는 백로그다.
    public static List<DialogueLogEntry> Before(IReadOnlyList<DialogueLogEntry> backlog, int sceneStartSerial)
    {
        var inherited = new List<DialogueLogEntry>();

        for (int i = 0; i < backlog.Count; i++)
            if (backlog[i].lineSequence < sceneStartSerial)
                inherited.Add(backlog[i]);

        return inherited;
    }

    // occurrence = (NodeName, LineId)가 그 장면 안에서 몇 번째인지 (첫 번째 = 1).
    // 그 줄이 백로그에 없으면(기록이 잘려 나갔으면) false.
    public static bool TryMakeLineTarget(
        IReadOnlyList<DialogueLogEntry> backlog,
        in DialogueLogEntry entry,
        int sceneStartSerial,
        out SaveLineTarget target)
    {
        target = null;
        int occurrence = 0;

        for (int i = 0; i < backlog.Count; i++)
        {
            DialogueLogEntry candidate = backlog[i];

            // 이 장면 이전의 대사와 고른 대사 이후는 제외.
            if (candidate.lineSequence < sceneStartSerial
                || candidate.lineSequence > entry.lineSequence)
                continue;

            if (!string.Equals(candidate.nodeName, entry.nodeName, StringComparison.Ordinal)
                || !string.Equals(candidate.lineId, entry.lineId, StringComparison.Ordinal))
                continue;

            occurrence++;
        }

        if (occurrence == 0)
            return false;

        target = new SaveLineTarget
        {
            NodeName = entry.nodeName,
            LineId = entry.lineId,
            Occurrence = occurrence,
        };

        return true;
    }
}
