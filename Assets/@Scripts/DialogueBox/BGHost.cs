// using System;
// using System.Collections.Generic;
// using UnityEngine;
//
// public interface IBGRuntimeRegistry
// {
//     void RegisterRuntimeBackground(string bgKey, RectTransformResponseTarget view);
//     void UnregisterRuntimeBackground(string bgKey, RectTransformResponseTarget expected = null);
//     void DestroyRuntimeBackground(string bgKey);
//     void ClearRuntimeBackgrounds();
// }
//
// public sealed class BGHost : MonoBehaviour, IBGRuntimeRegistry, IBGViewPrefabProvider
// {
//     [Serializable]
//     public struct BackgroundPrefabEntry
//     {
//         public string key;
//         public RectTransformResponseTarget prefab;
//     }
//
//     [SerializeField] private BackgroundPrefabEntry[] prefabMap;
//
//     private readonly Dictionary<string, RectTransformResponseTarget> _runtimeViews = new();
//
//     public void RegisterRuntimeBackground(string bgKey, RectTransformResponseTarget view)
//     {
//         if (view == null)
//         {
//             Debug.LogWarning($"[BGHost] RegisterRuntimeBackground failed. view is null. bgKey={bgKey}", this);
//             return;
//         }
//
//         _runtimeViews[bgKey] = view;
//     }
//     
//     // expected가 지정된 경우, 등록된 view가 바뀌었으면 제거하지 않는다.
//     // (같은 key로 새 배경이 등록된 뒤 이전 배경이 뒤늦게 해제를 시도하는 상황 방어)
//     public void UnregisterRuntimeBackground(string bgKey, RectTransformResponseTarget expected = null)
//     {
//         if (!_runtimeViews.TryGetValue(bgKey, out RectTransformResponseTarget current))
//             return;
//
//         if (expected != null && !ReferenceEquals(current, expected))
//             return;
//
//         _runtimeViews.Remove(bgKey);
//     }
//
//     public void DestroyRuntimeBackground(string bgKey)
//     {
//         if (!_runtimeViews.TryGetValue(bgKey, out RectTransformResponseTarget view))
//             return;
//
//         _runtimeViews.Remove(bgKey);
//
//         if (view == null)
//         {
//             Debug.LogWarning($"[BGHost] DestroyRuntimeBackground skipped. view is already null. bgKey={bgKey}", this);
//             return;
//         }
//
//         Destroy(view.gameObject);
//     }
//
//     public void ClearRuntimeBackgrounds()
//     {
//         foreach (RectTransformResponseTarget view in _runtimeViews.Values)
//         {
//             if (view == null)
//                 continue;
//             
//             Destroy(view.gameObject);
//         }
//
//         _runtimeViews.Clear();
//     }
//
//     public bool TryGetBackgroundViewPrefab(string key, out RectTransformResponseTarget prefab)
//     {
//         prefab = null;
//
//         if (prefabMap == null || prefabMap.Length == 0)
//             return false;
//
//         for (int i = 0; i < prefabMap.Length; i++)
//         {
//             var entry = prefabMap[i];
//
//             if (string.IsNullOrWhiteSpace(entry.key) || entry.prefab == null)
//                 continue;
//
//             if (!string.Equals(entry.key.Trim(), key, StringComparison.Ordinal))
//                 continue;
//
//             prefab = entry.prefab;
//             return true;
//         }
//
//         Debug.LogWarning($"[BGHost] Background view prefab not found. key={key}", this);
//         return false;
//     }
// }