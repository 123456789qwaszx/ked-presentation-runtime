// using System.Collections.Generic;
//
// public interface IVNFlagStore
// {
//     List<VNFlagEntry> Capture();
//     void Restore(List<VNFlagEntry> flags);
// }
//
// public sealed class EmptyVNFlagStore : IVNFlagStore
// {
//     public List<VNFlagEntry> Capture()
//     {
//         return new List<VNFlagEntry>();
//     }
//
//     public void Restore(List<VNFlagEntry> flags)
//     {
//         //Debug.LogWarning("[EmptyVNFlagStore] Restore called, but no real flag store is bound.");
//     }
// }