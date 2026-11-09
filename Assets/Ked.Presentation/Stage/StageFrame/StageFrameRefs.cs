using UnityEngine;

/// <summary>
/// 무대 하나의 틀 — 창 · 컷인 · 그림 팝업. 빌더가 만들고 보장한다 — null 검사하지 않는다.
/// </summary>
public sealed class StageFrameRefs
{
    public string StageKey { get; }

    /// <summary>창 — 무대 루트의 부모. 컷인이 들고 움직이고, 팝업은 그 다음 형제에 선다.</summary>
    public RectTransform Window { get; }

    public StageCutInSlot CutIn { get; }
    public StagePopupSlot Popup { get; }

    public StageFrameRefs(string stageKey, RectTransform window, StageCutInSlot cutIn, StagePopupSlot popup)
    {
        StageKey = stageKey;
        Window = window;
        CutIn = cutIn;
        Popup = popup;
    }

    /// <summary>중립 — 창 · 판과 그림 팝업을 닫는다. 도는 모션 · 페이드는 끝값 없이 끊는다.</summary>
    public void Reset()
    {
        CutIn.Reset();
        Popup.Reset();
    }
}
