using System.Collections.Generic;

namespace Ked.Save
{
    // 세이브 슬롯 저장소 (M7-1) — 레포가 비워 뒀던 그 자리.
    //
    // 동기다(계획서의 "권장 async"와 다른 결정 — M7-check에 기록).
    // 유일한 구현이 로컬 파일이고 파일은 수 KB라, async로 만들면 스레드 풀에서 돌다
    // Unity API를 못 만지는 제약만 생기고 얻는 게 없다. 서버 동기화는 저장소가 아니라
    // ServerSyncSaveStore의 일이고 그쪽은 처음부터 async다 — 느린 것만 비동기로 한다.
    public interface ISaveStore
    {
        // 슬롯 번호는 save.SlotNo. 같은 슬롯에 다시 쓰면 덮는다(서버 PUT과 같은 의미).
        void Save(LocalSaveFile save);

        // 없거나 읽을 수 없으면 null.
        LocalSaveFile Load(int slotNo);

        // 존재하는 슬롯 전부. 세이브 목록 화면(뒤 M)의 재료.
        IReadOnlyList<LocalSaveFile> ListAll();
    }
}
