// ============================================================
// ChapterEpisodeProgressionSO.cs
// 경로: Assets/_Project/Scripts/VN/Episode/Progression/
//
// 챕터 하나의 Episode 진행 관계 전체를 보관하는 Authoring Asset.
//
// 생성 방법:
//   Project 뷰 우클릭 → Create → VN/Episode/Chapter Episode Progression
//
// 사용 흐름:
//   ChapterEpisodeProgressionSO   (이 파일 — Authoring)
//       ↓
//   EpisodeProgressionEvaluator   (런타임 조건 평가, 추후 구현)
//       ↓
//   EpisodeGraphViewModelBuilder
//       ↓
//   EpisodeGraphViewData
//       ↓
//   EpisodeGraphRenderer
// ============================================================

using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "VN/Episode/Chapter Episode Progression", fileName = "ChapterProgression")]
public sealed class ChapterEpisodeProgressionSO : ScriptableObject
{
    // ─── 챕터 정보 ──────────────────────────────────────────

    /// <summary>
    /// 챕터 고유 ID. 예: "ch05"
    /// Validator가 비어있음을 Error로 처리한다.
    /// </summary>
    public string ChapterId;

    /// <summary>에디터 / UI에 표시될 챕터 이름.</summary>
    public string DisplayName;

    // ─── 진입점 ─────────────────────────────────────────────

    /// <summary>
    /// 챕터 최초 진입 시 실행될 EpisodeId.
    /// Nodes 목록 안에 반드시 존재해야 한다.
    /// </summary>
    public string StartEpisodeId;

    // ─── 데이터 ─────────────────────────────────────────────

    /// <summary>
    /// 이 챕터에 포함된 모든 에피소드 노드 정의 목록.
    /// EpisodeId는 목록 내에서 중복 불가.
    /// </summary>
    public List<EpisodeNodeDefinition> Nodes = new List<EpisodeNodeDefinition>();

    /// <summary>
    /// 챕터 엔딩 조건 + 다음 챕터 개방 규칙 목록.
    /// EndingKey는 목록 내에서 중복 불가.
    /// </summary>
    public List<ChapterEndingRule> EndingRules = new List<ChapterEndingRule>();
}
