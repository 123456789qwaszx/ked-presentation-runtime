// 회차 파일 저장소. slotNo는 "활성 회차"를 뜻하는 옛 좌표 — 파일은 회차 id로 산다.
public interface ISaveStore
{
    void Save(LocalSaveFile save);
    
    // 현재 회차 로드.
    LocalSaveFile LoadActive();

    // 활성 포인터를 지운다. 회차 파일은 남는다.
    void ClearActive();

    string ActiveId { get; }

    // 회차 id로 직접 로드.
    LocalSaveFile LoadPlaythrough(string playthroughId);

    string QueuePathOf(string playthroughId);

    BookmarkFile LoadBookmarks();
    
    void SaveBookmarks(BookmarkFile bookmarks);

    // 보관 중인 회차 id 전부(활성 포함). 이력 화면이 접기/펼치기를 판단하는 재료.
    System.Collections.Generic.IReadOnlyList<string> ListPlaythroughIds();
}