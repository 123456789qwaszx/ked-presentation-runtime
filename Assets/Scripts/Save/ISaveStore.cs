// 세이브 슬롯 저장소 (M7) — 레포가 비워 뒀던 자리.
//
// 동기다. 유일한 구현이 수 KB 로컬 파일이라 async는 얻는 것 없이 스레드 제약만 얹는다.
// 느린 쪽(서버)은 저장소가 아니라 ServerSyncSaveStore의 일이고 그쪽이 async다.
public interface ISaveStore
{
    // 같은 슬롯에 다시 쓰면 덮는다.
    void Save(LocalSaveFile save);

    // 없으면 null.
    LocalSaveFile Load(int slotNo);

    // 없어도 조용하다.
    void Delete(int slotNo);
}
