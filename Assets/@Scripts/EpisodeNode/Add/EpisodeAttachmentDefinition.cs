// ============================================================
// EpisodeAttachmentDefinition.cs
// 경로: Assets/_Project/Scripts/VN/Episode/Progression/
//
// 특정 MainNode에 부착되는 부가 이벤트 정의.
// 사이드 대화, 보너스씬, 캐릭터 이벤트, 회상, 애프터스토리 등.
//
// 주의:
//   ParentEpisodeId는 소속 노드를 명시한다.
//   Editor에서 "Add Attachment" 시 현재 선택된 EpisodeId가 자동으로 입력된다.
// ============================================================

using System;
using System.Collections.Generic;

[Serializable]
public sealed class EpisodeAttachmentDefinition
{
    /// <summary>
    /// Attachment 고유 ID. SO 전체에서 중복 불가.
    /// 예: "attach_main05.02_sideA"
    /// </summary>
    public string AttachmentId;

    /// <summary>부착될 MainNode의 EpisodeId.</summary>
    public string ParentEpisodeId;

    /// <summary>UI에 표시될 Attachment 제목.</summary>
    public string Title;

    /// <summary>UI 인덱스 텍스트. 예: "BE", "IF", "★"</summary>
    public string IndexText;

    /// <summary>Attachment 종류.</summary>
    public EpisodeAttachmentKind Kind;

    /// <summary>클릭 시 실행할 Yarn/다이얼로그 엔트리 ID.</summary>
    public string DialogueEntryId;

    /// <summary>이 Attachment가 UI에 표시되기 위한 조건 목록 (AND).</summary>
    public List<EpisodeCondition> VisibleConditions = new List<EpisodeCondition>();

    /// <summary>이 Attachment가 클릭 가능하기 위한 조건 목록 (AND).</summary>
    public List<EpisodeCondition> UnlockConditions = new List<EpisodeCondition>();

    /// <summary>클리어 후 반복 실행 가능 여부.</summary>
    public bool IsRepeatable;

    /// <summary>기획자용 메모.</summary>
    public string DesignerNote;
}

public enum EpisodeAttachmentKind
{
    SideDialogue,
    BonusScene,
    CharacterEvent,
    Memory,
    AfterStory
}
