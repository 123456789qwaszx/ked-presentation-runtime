/// <summary>
/// 무대 틀의 계층 — 무대 루트 **바깥**. 무대 리그(StageRigSchema)가 무대 안의 세상(카메라 · 깊이 · 마스크)을 맡고,
/// 틀은 그 무대를 화면 위에 어떻게 띄울지를 맡는다. 틀은 리그를 알고, 리그는 틀을 모른다.
///
///   {stage}_Window              무대 전체를 들고 팝 · 회전한다(컷인). 중립 항등 — 무대 루트와 같은 rect. 피벗은 컷인을 열 때 판 아래 가운데로
///     {stage}_Panel             컷인 판 그림(Image). 무대 루트보다 뒤(형제 0). 닫혀 있으면 꺼져 있다
///     {stage}_Stage_Root        (무대 리그)
///   {stage}_Popup               창의 다음 형제. 뒷막과 그림을 한 덩어리로 든다(CanvasGroup) — 불투명도 하나가 둘을 같이 페이드한다
///     {stage}_Popup_Backdrop    화면 전체 어두운 막(Image, 색 · 세기는 프리셋)
///     {stage}_Popup_Image       그림(Image). 원본 픽셀 × 프리셋 배율, 무대 중심 기준 자리
///
/// 무대 루트 바깥이라 리그 좌표 계산(측정 체인 · 샷 식)은 틀을 모른다 — 창이 팝하는 도중에 place를 해도 같다.
/// 팝업은 창 밖이라 컷인 팝에 끌려가지 않고, 창 바로 다음이라 그 무대의 내용 위 · 다음 무대 아래에 그려진다.
/// StageFrameBuilder가 세운다. 설계와 실측은 docs/PLAN_cutin.md · docs/PLAN_popup.md.
/// </summary>
public static class StageFrameSchema
{
    /// <summary>창 — 무대 루트의 부모.</summary>
    public static string WindowName(string stageKey) => $"{stageKey}_Window";

    /// <summary>컷인 판 — 창의 첫 자식(무대 루트보다 뒤에 그려진다).</summary>
    public static string PanelName(string stageKey) => $"{stageKey}_Panel";

    /// <summary>그림 팝업 그룹 — 창의 다음 형제.</summary>
    public static string PopupName(string stageKey) => $"{stageKey}_Popup";

    /// <summary>팝업 뒷막 — 그룹의 첫 자식(그림보다 뒤).</summary>
    public static string PopupBackdropName(string stageKey) => $"{stageKey}_Popup_Backdrop";

    /// <summary>팝업 그림 — 그룹의 둘째 자식.</summary>
    public static string PopupImageName(string stageKey) => $"{stageKey}_Popup_Image";
}
