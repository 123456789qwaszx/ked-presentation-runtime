using Yarn.Unity;

/// <summary>
/// 상시 표시 오버레이. 패널과 달리 스택에 쌓이지 않고 캠페인 내내 떠 있다.
///
/// 갱신은 전부 동기다. 여기서 await 하면 노드 재생 시점이 뒤로 밀린다.
/// </summary>
public sealed partial class VnScreenBindings
{
    // 하루 리포트와 엔딩이 읽어 가는 누적값. 스냅숏이 올 때마다 함께 받아 둔다.
    private int _hudTotalEnergy;
    private int _hudEnergyQuota;

    /// <summary>
    /// 캠페인이 시작될 때 한 번 올린다.
    /// 오버레이 레이어는 blocksRaycasts=false 로 올라가므로 대사 진행 입력을 가로채지 않는다.
    /// </summary>
    public void ShowGuesthouseHud()
    {
        UI.ShowOverlay<GuesthouseStatusOverlay>();
    }

    public void HideGuesthouseHud()
    {
        UI.HideOverlay<GuesthouseStatusOverlay>();
    }

    /// <summary>
    /// 노드를 재생하기 직전에 호출된다. 동기 갱신이므로 대사 시작이 밀리지 않는다.
    /// 오버레이가 아직 올라오지 않았으면 조용히 넘어간다.
    /// </summary>
    public void UpdateGuesthouseHud(in GuesthouseHudSnapshot snapshot)
    {
        _hudTotalEnergy = snapshot.EnergyTotal;
        _hudEnergyQuota = snapshot.EnergyQuota;

        GuesthouseStatusOverlay overlay = UI.GetUI<GuesthouseStatusOverlay>();

        if (overlay == null)
            return;

        overlay.Apply(snapshot);
    }
}
