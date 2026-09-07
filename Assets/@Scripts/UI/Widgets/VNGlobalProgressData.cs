using System;
using System.Collections.Generic;

[Serializable]
public sealed class VNGlobalProgressData
{
    public List<string> unlockedCgKeys = new();
    public List<string> unlockedEndingKeys = new();
    public List<string> readLineIds = new();

    // Continue 버튼이 최종적으로 참조하는 슬롯. AutoSave도 Continue 대상으로 갱신한다.
    public string continueSlotId = "";

    public string latestManualSlotId = "";
    public string latestAutoSlotId = "";
    
    public void Normalize()
    {
        if (unlockedCgKeys == null)     unlockedCgKeys = new List<string>();
        if (unlockedEndingKeys == null) unlockedEndingKeys = new List<string>();
        if (readLineIds == null)        readLineIds = new List<string>();

        if (continueSlotId == null)     continueSlotId = "";
        if (latestManualSlotId == null) latestManualSlotId = "";
        if (latestAutoSlotId == null)   latestAutoSlotId = "";
    }
}