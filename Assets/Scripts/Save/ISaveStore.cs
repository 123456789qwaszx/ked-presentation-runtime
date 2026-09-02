// 회차 파일 저장소. slotNo는 "활성 회차"를 뜻하는 옛 좌표 — 파일은 회차 id로 산다.
public interface ISaveStore
{
    // 활성 회차로 저장한다. 파일은 회차 id 이름, 활성 포인터가 그 id를 가리킨다.
    void Save(LocalSaveFile save);

    // 활성 회차. 없으면 null.
    LocalSaveFile Load(int slotNo);

    // 활성 포인터를 지운다. 회차 파일은 남는다(보관 정책 — UI에서만 접는다).
    void Delete(int slotNo);

    // 지금 활성 회차 id. 없으면 null.
    string ActiveId { get; }

    // 회차 id로 직접.
    LocalSaveFile LoadPlaythrough(string playthroughId);

    // 그 회차의 큐 파일 경로.
    string QueuePathOf(string playthroughId);
}
