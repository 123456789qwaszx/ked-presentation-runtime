// 세이브 슬롯 저장소
// - 로컬 세이브 파일 저장은 굳이 async로 만들지 않음.
public interface ISaveStore
{
    // 같은 슬롯에 다시 쓰면 덮는다.
    void Save(LocalSaveFile save);

    // 없으면 null.
    LocalSaveFile Load(int slotNo);

    // 없어도 조용하다.
    void Delete(int slotNo);
}