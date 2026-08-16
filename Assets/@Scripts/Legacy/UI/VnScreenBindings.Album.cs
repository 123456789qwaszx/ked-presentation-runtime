// public sealed partial class VnScreenBindings
// {
//     private VNSaveLoadSystem _vnSaveLoadSystem;
//     
//     public void ConfigureAlbumView(VNSaveLoadSystem vnSaveLoadSystem)
//     {
//         _vnSaveLoadSystem = vnSaveLoadSystem;
//     }
//     
//     private void OpenAlbumMenuPanel()
//     {
//         UI.PushPanel<AlbumMenuPanel>(panel =>
//         {
//             BindPanel(panel, ApplyBindings);
//             Refresh(panel);
//         });
//     }
//
//     private void ApplyBindings(AlbumMenuPanel panel)
//     {
//         AddBinding(panel,
//             p => p.CloseClicked += ClosePanel,
//             p => p.CloseClicked -= ClosePanel);
//     }
//
//     private void Refresh(AlbumMenuPanel panel)
//     {
//         VNAlbumUnlockService albumService = _vnSaveLoadSystem.AlbumService;
//
//         panel.Rebuild(
//             albumService.GetAllItems(),
//             albumService.IsUnlocked);
//     }
// }