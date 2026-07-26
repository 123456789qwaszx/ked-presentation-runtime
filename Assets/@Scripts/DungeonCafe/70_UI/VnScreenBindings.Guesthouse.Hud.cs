// 상시 표시 오버레이
public sealed partial class VnScreenBindings
{
    // 하루 리포트와 엔딩이 읽어 가는 누적값.
    private int _hudTotalEnergy;
    private int _hudEnergyQuota;

    public void ShowGuesthouseHud() => UI.ShowOverlay<GuesthouseStatusOverlay>();
    public void HideGuesthouseHud() => UI.HideOverlay<GuesthouseStatusOverlay>();
    
    public void UpdateGuesthouseHud(in GuesthouseHudSnapshot snapshot)
    {
        _hudTotalEnergy = snapshot.EnergyTotal;
        _hudEnergyQuota = snapshot.EnergyQuota;

        GuesthouseStatusOverlay overlay = UI.GetUI<GuesthouseStatusOverlay>();

        overlay.Apply(snapshot);
    }
}
