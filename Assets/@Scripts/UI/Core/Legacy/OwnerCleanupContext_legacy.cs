// using System;
// using System.Collections.Generic;
//
// public sealed class OwnerCleanupContext<TOwner> : IDisposable
//     where TOwner : class
// {
//     private readonly Dictionary<TOwner, List<Action>> _cleanupByOwner = new();
//     private readonly Action<Exception> _onException;
//     
//     public OwnerCleanupContext(Action<Exception> onException = null)
//     {
//         _onException = onException;
//     }
//
//     public void AddCleanup(TOwner owner, Action cleanup)
//     {
//         if (!_cleanupByOwner.TryGetValue(owner, out var cleanups))
//         {
//             cleanups = new List<Action>();
//             _cleanupByOwner[owner] = cleanups;
//         }
//
//         cleanups.Add(cleanup);
//     }
//
//     public void Clear(TOwner owner)
//     {
//         if (!_cleanupByOwner.TryGetValue(owner, out var cleanups))
//             return;
//
//         RunCleanups(cleanups);
//         _cleanupByOwner.Remove(owner);
//     }
//
//     public void ClearAll()
//     {
//         foreach (var kv in _cleanupByOwner)
//             RunCleanups(kv.Value);
//
//         _cleanupByOwner.Clear();
//     }
//     
//     private void RunCleanups(List<Action> cleanups)
//     {
//         for (int i = cleanups.Count - 1; i >= 0; i--)
//         {
//             try
//             {
//                 cleanups[i]?.Invoke();
//             }
//             catch (Exception e)
//             {
//                 _onException?.Invoke(e);
//             }
//         }
//     }
//
//     public void Dispose()
//     {
//         ClearAll();
//     }
// }