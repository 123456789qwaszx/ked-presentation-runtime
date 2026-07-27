// 상시 표시 오버레이 (v3)
public sealed partial class VnScreenBindings
{
    public void ShowGuesthouseHud() => UI.ShowOverlay<GuesthouseStatusOverlay>();
    public void HideGuesthouseHud() => UI.HideOverlay<GuesthouseStatusOverlay>();

    public void UpdateGuesthouseHud(in GuesthouseHudSnapshot snapshot)
    {
        GuesthouseStatusOverlay overlay = UI.GetUI<GuesthouseStatusOverlay>();

        overlay.Apply(snapshot);
    }
}
