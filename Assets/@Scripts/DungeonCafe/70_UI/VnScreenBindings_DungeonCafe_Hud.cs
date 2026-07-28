// 상시 표시 오버레이 (v3)
public sealed partial class VnScreenBindings
{
    public void DungeonCafeHud() => UI.ShowOverlay<DungeonCafeStatusOverlay>();
    public void HideDungeonCafeHud() => UI.HideOverlay<DungeonCafeStatusOverlay>();

    public void UpdateDungeonCafeHud(in DungeonCafeHudSnapshot snapshot)
    {
        DungeonCafeStatusOverlay overlay = UI.GetUI<DungeonCafeStatusOverlay>();

        overlay.Apply(snapshot);
    }
}
