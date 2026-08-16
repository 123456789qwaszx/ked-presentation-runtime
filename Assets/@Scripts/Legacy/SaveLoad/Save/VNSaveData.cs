// using System;
// using System.Collections.Generic;
//
// [Serializable]
// public sealed class VNSaveData
// {
//     public string slotId = "";
//
//     public string nodeName = "";
//     public string lineId = "";
//
//     /// <summary>
//     /// RollbackHistory 기준의 방문 인덱스.
//     /// lineId만으로는 루프/재진입/분기에서 애매할 수 있으므로 같이 저장한다.
//     /// </summary>
//     public int visitedIndex = -1;
//
//     /// <summary>
//     /// 같은 node 안에서 같은 lineId가 반복되는 특수 상황 보조용.
//     /// MVP에서는 0으로 둬도 된다.
//     /// </summary>
//     public int lineVisitCountInNode = 0;
//
//     public string chapterLabel = "";
//     public string linePreview = "";
//
//     public string savedAt = "";
//     public long savedAtTicks = 0L;
//
//     public int playtimeSeconds = 0;
//
//     public List<VNFlagEntry> flags = new();
//
//     // 현재 save target line까지 도달하기 위해 재현해야 하는 선택 기록.
//     // Load/Rollback seek 중 option set을 만나면 이 기록을 사용해 UI 없이 자동 선택.
//     public List<VNChoiceRecord> choices = new();
//
//     public void Normalize()
//     {
//         if (slotId == null)       slotId = "";
//         if (nodeName == null)     nodeName = "";
//         if (lineId == null)       lineId = "";
//         if (chapterLabel == null) chapterLabel = "";
//         if (linePreview == null)  linePreview = "";
//         if (savedAt == null)      savedAt = "";
//         if (flags == null)        flags = new List<VNFlagEntry>();
//         if (choices == null)      choices = new List<VNChoiceRecord>();
//     }
// }