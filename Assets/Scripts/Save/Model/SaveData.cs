using System.Collections.Generic;

// 자동 저장 한 건. 완료 장면의 시작 상태와 경로를 보존하여 이어하기와 과거 장면 이동을 지원한다.
public sealed class LocalSaveFile
{
    public string PlaythroughId;
    public string ContentVersion;
    public string ChapterId;
    public string CurrentEpisodeId;
    public Dictionary<string, int> Stats = new();
    public bool ChapterCompleted;
    public List<SceneRecord> Scenes = new();
    public List<DialogueLogEntry> Backlog = new();
    public SavedLoadPlan PendingLoad;
    public int PlaySeconds;
    public string SavedAtUtc;
}

// 대본 안의 한 라인. 같은 line ID가 반복될 수 있어 장면 안 등장 순번까지 사용한다.
public sealed class SaveLineTarget
{
    public string NodeName;
    public string LineId;
    public int Occurrence;
}

// 장면 진입 당시 상태. 완료된 장면으로 돌아갈 때 유일한 시작점이다.
public sealed class SceneCheckpoint
{
    public string ChapterId;
    public string EpisodeId;
    public Dictionary<string, int> Stats = new();
    public int BacklogSerialStart;
    public int PlaySecondsAtEntry;
    public string EnteredAtUtc;
}

// 완료된 장면 하나: 진입 상태와 그 장면 안에서 확정된 선택 기록.
public sealed class SceneRecord
{
    public SceneCheckpoint Checkpoint;
    public List<SavedChoice> Path = new();
    public List<VNChoiceRecord> YarnChoices = new();
    public int BacklogSerialEnd;
}

public sealed class SavedChoice
{
    public string FromEpisodeId;
    public int OptionIndex;
}

// 장면 루트에서 저장한 라인까지 재생하기 위한 일회성 계획.
public sealed class SavedLoadPlan
{
    public List<SavedChoice> Path = new();
    public List<VNChoiceRecord> YarnChoices = new();
    public SaveLineTarget Target;
}

// 슬롯 목록 파일에 들어가는 가벼운 표시 정보.
public sealed class SaveSlotIndexFile
{
    public int FormatVersion;
    public List<SaveSlotEntry> Slots = new();
}

public sealed class SaveSlotEntry
{
    public string Id;
    public string DataKey;
    public string Label;
    public string Preview;
    public string ChapterId;
    public string SavedAtUtc;
    public int PlaySeconds;
}

// 슬롯 본문 파일의 envelope.
public sealed class SaveSlotFile
{
    public int FormatVersion;
    public SaveSlotData Data;
}

// 슬롯 하나를 원래 회차 파일 없이도 복원할 수 있는 독립 데이터.
public sealed class SaveSlotData
{
    public string Id;
    public string ContentVersion;
    public SceneCheckpoint Checkpoint;
    public SavedLoadPlan LoadPlan;
    public List<SceneRecord> Scenes = new();
    public List<DialogueLogEntry> Backlog = new();
    public int PlaySeconds;
}
