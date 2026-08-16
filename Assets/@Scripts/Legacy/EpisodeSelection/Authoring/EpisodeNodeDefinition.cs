// // ============================================================
// // EpisodeNodeDefinition.cs  (Progression 전용)
// // 경로: Assets/_Project/Scripts/VN/Episode/Progression/
// //
// // 주의: 이 파일은 렌더링용 EpisodeNodeRigSchema / EpisodeNodeRefs와
// //       완전히 분리된 순수 Authoring 데이터 클래스다.
// //
// // EpisodeNodeKind enum은 기존 EpisodeGraphData.cs에 이미 존재하므로
// // 이 파일에서 재정의하지 않는다.
// // ============================================================
//
// using System;
// using System.Collections.Generic;
//
// [Serializable]
// public sealed class EpisodeNodeDefinition
// {
//     // ─── 식별 ───────────────────────────────────────────────
//
//     /// <summary>
//     /// 에피소드 고유 ID. SO 내에서 중복 불가.
//     /// 예: "main05.02", "branch05.02A"
//     /// </summary>
//     public string EpisodeId;
//
//     /// <summary>UI / 에디터에 표시될 에피소드 제목.</summary>
//     public string Title;
//
//     /// <summary>카드 인덱스 텍스트. 예: "05", "05A"</summary>
//     public string IndexText;
//
//     // ─── 종류 ───────────────────────────────────────────────
//
//     /// <summary>
//     /// Main: 메인 스토리 라인. Attachment: 부착 노드.
//     /// EpisodeNodeKind는 EpisodeGraphData.cs에서 정의된 enum을 사용한다.
//     /// </summary>
//     public EpisodeNodeKind Kind;
//
//     // ─── 다이얼로그 ─────────────────────────────────────────
//
//     /// <summary>
//     /// 클릭 시 실행할 Yarn node / 다이얼로그 엔트리 ID.
//     /// 비어 있으면 Validator가 Error를 발생시킨다.
//     /// </summary>
//     public string DialogueEntryId;
//
//     // ─── 조건 ───────────────────────────────────────────────
//
//     /// <summary>
//     /// 이 노드가 그래프에 표시되기 위한 조건 목록 (AND).
//     /// 비어 있으면 항상 표시.
//     /// </summary>
//     public List<EpisodeCondition> VisibleConditions = new List<EpisodeCondition>();
//
//     /// <summary>
//     /// 이 노드가 클릭 가능(해금 상태)이 되기 위한 조건 목록 (AND).
//     /// 비어 있으면 항상 해금.
//     /// </summary>
//     public List<EpisodeCondition> UnlockConditions = new List<EpisodeCondition>();
//
//     // ─── 진행 ───────────────────────────────────────────────
//
//     /// <summary>
//     /// 이 에피소드 클리어 후 제시될 다음 선택지 목록.
//     /// 조건부 분기, 다중 루트 모두 이 목록으로 표현한다.
//     /// </summary>
//     public List<EpisodeNextOption> NextOptions = new List<EpisodeNextOption>();
//
//     /// <summary>
//     /// 이 노드에 부착된 부가 이벤트 목록.
//     /// Attachment는 메인 진행과 별개로 접근 가능한 사이드 콘텐츠다.
//     /// </summary>
//     public List<EpisodeAttachmentDefinition> Attachments = new List<EpisodeAttachmentDefinition>();
//
//     // ─── 엔딩 ───────────────────────────────────────────────
//
//     /// <summary>
//     /// 이 노드가 챕터 엔딩 후보인지 여부.
//     /// true이면 EndingKey를 반드시 입력해야 한다.
//     /// </summary>
//     public bool IsChapterEndingCandidate;
//
//     /// <summary>
//     /// 이 노드에 연결된 챕터 엔딩 키.
//     /// ChapterEpisodeProgressionSO.EndingRules 목록의 EndingKey와 대응한다.
//     /// </summary>
//     public string EndingKey;
//
//     // ─── 메타 ───────────────────────────────────────────────
//
//     /// <summary>기획자용 메모. 빌드에 영향 없음.</summary>
//     public string DesignerNote;
// }
