using Ked.Presentation.Core;
using UnityEngine;

// 줄 태그 — 넘김. 박스 태그(#boxhide …)와 따로 둔다: 대사창 모양이 아니라 "다음 줄로 언제 가나"의 일이다.
//
//   #auto        줄이 다 보이면(타자기 끝 · 그 줄 앞머리의 연출 명령 끝) 입력 없이 다음으로 넘긴다.
//   #auto=1.8s   줄이 들어온 때부터 1.8초가 지나고 글도 다 보이면 넘긴다 — 글 카드(장소 카드 · 장제목)가 머무는 시간
//                (docs/PLAN_line-beat.md R9, 표기는 코어 AutoAdvanceTag). 머무는 동안의 클릭은 그대로 넘긴다.
//           글 카드처럼 보여 주고 스스로 지나가는 줄에 쓴다 — 노드의 마지막 줄이면 그대로 노드가 끝난다.
//           대사 없이 연출만 하는 자리는 줄이 아니라 무대 스텝(<<step>>)이다.
//           자동 진행(Auto)의 대기 시간은 쓰지 않는다 — 다 보인 다음 프레임에 넘긴다.
//           재생 입력이 멈춘 동안(메뉴 · 팝업 · UI 숨김)은 기다린다. 넘기는 길이 재생 입력의 프레임(PlaybackFeatureController.Tick)이다.
//           되감기로 닿은 줄에서는 넘기지 않는다 — 다시 보려고 돌아온 것이다(LineAdvancePolicy).
//
// 표기는 박스 태그와 같은 정규화를 거친다(#auto · #Auto · #AUTO가 같다).
public static class LineAdvanceTag
{
    /// <summary>#auto 줄인가(#auto=길이 포함). 넘김 정책이 부른다 — 경고는 TryGetAuto가 한 번 낸다.</summary>
    public static bool IsAuto(string[] metadata) => Find(metadata, out _, out _);

    /// <summary>#auto 줄이면 true와 머무름(초, 없으면 0). 읽지 못한 길이는 경고하고 머무름 없이.</summary>
    public static bool TryGetAuto(string[] metadata, out float holdSeconds)
    {
        if (!Find(metadata, out holdSeconds, out string error))
            return false;

        if (error != null)
            Debug.LogWarning($"[LineAdvanceTag] {error}");

        return true;
    }

    private static bool Find(string[] metadata, out float holdSeconds, out string error)
    {
        holdSeconds = 0f;
        error = null;

        if (metadata == null)
            return false;

        for (int i = 0; i < metadata.Length; i++)
        {
            if (AutoAdvanceTag.TryRead(DialogueBoxTagNormalizer.Normalize(metadata[i]), out holdSeconds, out error))
                return true;
        }

        return false;
    }
}
