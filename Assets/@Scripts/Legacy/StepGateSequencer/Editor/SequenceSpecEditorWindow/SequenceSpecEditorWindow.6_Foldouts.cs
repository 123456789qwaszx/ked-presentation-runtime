// #if UNITY_EDITOR
// using System;
// using System.Collections.Generic;
// using UnityEditor;
// using UnityEngine;
//
// /// <summary>
// /// [SerializeReference] 커맨드 리스트의 Foldout(펼침/접힘) UI 상태를 안정적으로 관리/저장한다.
// ///
// /// 배경:
// /// - 커맨드 UI는 ReorderableList + ManagedReference 인스펙터로 렌더링된다.
// /// - Unity의 SerializedProperty.isExpanded는 리스트가 재생성되는 상황(선택 변경/리빌드/추가/삭제/리오더)에서
// ///   상태가 쉽게 흔들리므로, Foldout 상태를 “에디터 UI 상태”로 보고 별도 캐시/저장을 한다.
// ///
// /// 핵심 아이디어:
// /// - (commands 리스트의 propertyPath) + (managedReferenceId) -> expanded(bool)
// ///   를 키로 삼아, 각 커맨드 인스턴스의 펼침 상태를 재빌드 이후에도 유지한다.
// ///
// /// 하는 일:
// /// 1) 런타임 캐시
// ///    - _commandFoldoutsByPath: commandsPath 단위로 managedReferenceId->bool 맵을 유지한다.
// ///    - GetFoldoutMap(): 해당 commandsPath의 맵을 보장(없으면 생성)한다.
// ///
// /// 2) 변경 작업(추가/삭제/리오더/리빌드) 중 상태 보존
// ///    - SnapshotCommandFoldouts(): 현재 배열의 펼침 상태를 스냅샷으로 떠둔다.
// ///    - RestoreCommandFoldouts(): 작업 후 상태를 복원하고, 사라진 ID는 정리(prune)한다.
// ///      * newIdToCollapse를 통해 “새로 들어온 커맨드는 기본 접힘” 같은 정책도 적용 가능.
// ///
// /// 3) 일괄 토글
// ///    - SetAllCommandFoldouts(): 한 트랙(한 commands 리스트) 전체를 펼침/접힘.
// ///    - SetAllCommandFoldouts_ForCurrentNode(): 현재 노드의 모든 스텝/모든 트랙을 대상으로 일괄 펼침/접힘.
// ///
// /// 4) 영속화(에셋 단위)
// ///    - 에셋 GUID 기반 키(FoldoutKeyPrefix + guid)로 SessionState(세션) + EditorPrefs(영구)에 저장한다.
// ///    - SaveFoldouts()/LoadFoldouts(): 전체 맵을 JSON으로 직렬화/역직렬화하며, 깨진 데이터는 무시한다.
// ///
// /// 여기서 수정하면 좋은 것들:
// /// - 키 정책: per-asset(현재) vs 전역, propertyPath를 정규화할지 여부
// /// - 기본값 정책: 처음 보는 커맨드/새로 삽입된 커맨드를 기본 펼침으로 둘지/접힘으로 둘지
// /// - 정리 전략: stale ID를 언제/어느 정도 공격적으로 prune 할지
// /// - “Expand/Collapse All” 범위: 현재 리스트/현재 노드/전체 시퀀스 중 어디까지 적용할지
// /// </summary>
//
// public sealed partial class SequenceSpecEditorWindow
// {
//     private const string FoldoutKeyPrefix = "CPS.SequenceEditor.Foldouts.";
//     
//     private readonly Dictionary<string, Dictionary<long, bool>> _commandFoldoutsByPath = new();
//
//     [Serializable]
//     private sealed class FoldoutStateBox
//     {
//         public List<PathEntry> entries = new();
//     }
//
//     [Serializable]
//     private sealed class PathEntry
//     {
//         public string path;
//         public List<long> ids = new();
//         public List<bool> values = new();
//     }
//
//     private Dictionary<long, bool> GetFoldoutMap(string commandsPath)
//     {
//         if (string.IsNullOrEmpty(commandsPath))
//             return null;
//
//         if (!_commandFoldoutsByPath.TryGetValue(commandsPath, out var map) || map == null)
//         {
//             map = new Dictionary<long, bool>();
//             _commandFoldoutsByPath[commandsPath] = map;
//         }
//
//         return map;
//     }
//
//     private Dictionary<long, bool> SnapshotCommandFoldouts(SerializedProperty commandsProp)
//     {
//         var snapshot = new Dictionary<long, bool>();
//         if (commandsProp == null || !commandsProp.isArray) return snapshot;
//
//         string path = commandsProp.propertyPath;
//         var map = GetFoldoutMap(path);
//
//         for (int i = 0; i < commandsProp.arraySize; i++)
//         {
//             var el = commandsProp.GetArrayElementAtIndex(i);
//             if (el == null) continue;
//             if (el.propertyType != SerializedPropertyType.ManagedReference) continue;
//
//             long id = el.managedReferenceId;
//             if (id == 0) continue;
//
//             bool expanded;
//             if (map != null && map.TryGetValue(id, out bool saved))
//             {
//                 expanded = saved;
//             }
//             else
//             {
//                 expanded = el.isExpanded;
//                 if (map != null) map[id] = expanded;
//             }
//
//             snapshot[id] = expanded;
//         }
//
//         return snapshot;
//     }
//
//     private void RestoreCommandFoldouts(SerializedProperty commandsProp, Dictionary<long, bool> snapshot,
//         long newIdToCollapse)
//     {
//         if (commandsProp == null || !commandsProp.isArray) return;
//
//         string path = commandsProp.propertyPath;
//         var map = GetFoldoutMap(path);
//         if (map == null) return;
//
//         var alive = new HashSet<long>();
//
//         for (int i = 0; i < commandsProp.arraySize; i++)
//         {
//             var el = commandsProp.GetArrayElementAtIndex(i);
//             if (el == null) continue;
//             if (el.propertyType != SerializedPropertyType.ManagedReference) continue;
//
//             long id = el.managedReferenceId;
//             if (id == 0) continue;
//
//             alive.Add(id);
//
//             bool next;
//             if (id == newIdToCollapse)
//             {
//                 next = false;
//             }
//             else if (snapshot != null && snapshot.TryGetValue(id, out bool saved))
//             {
//                 next = saved;
//             }
//             else if (map.TryGetValue(id, out bool cur))
//             {
//                 next = cur;
//             }
//             else
//             {
//                 next = false;
//             }
//
//             map[id] = next;
//             el.isExpanded = next;
//         }
//
//         if (map.Count > alive.Count)
//         {
//             var toRemove = new List<long>();
//             foreach (var kv in map)
//             {
//                 if (!alive.Contains(kv.Key))
//                     toRemove.Add(kv.Key);
//             }
//
//             for (int i = 0; i < toRemove.Count; i++)
//                 map.Remove(toRemove[i]);
//         }
//     }
//
//     private void SetAllCommandFoldouts(SerializedProperty commandsProp, bool expanded)
//     {
//         if (commandsProp == null || !commandsProp.isArray) return;
//
//         string commandsPath = commandsProp.propertyPath;
//         var map = GetFoldoutMap(commandsPath);
//         if (map == null) return;
//
//         var alive = new HashSet<long>();
//
//         for (int i = 0; i < commandsProp.arraySize; i++)
//         {
//             var el = commandsProp.GetArrayElementAtIndex(i);
//             if (el == null) continue;
//             if (el.propertyType != SerializedPropertyType.ManagedReference) continue;
//
//             long id = el.managedReferenceId;
//             if (id == 0) continue;
//
//             alive.Add(id);
//             map[id] = expanded;
//             el.isExpanded = expanded;
//         }
//
//         if (map.Count > alive.Count)
//         {
//             var toRemove = new List<long>();
//             foreach (var kv in map)
//             {
//                 if (!alive.Contains(kv.Key))
//                     toRemove.Add(kv.Key);
//             }
//
//             for (int i = 0; i < toRemove.Count; i++)
//                 map.Remove(toRemove[i]);
//         }
//
//         Repaint();
//     }
//
//     private void SetAllCommandFoldouts_ForCurrentNode(bool expanded)
//     {
//         if (!TryGetCurrentNodeStepsProp(out var stepsProp))
//             return;
//
//         for (int si = 0; si < stepsProp.arraySize; si++)
//         {
//             var stepProp = stepsProp.GetArrayElementAtIndex(si);
//             if (stepProp == null)
//                 continue;
//
//             var listProp = FindUnifiedCommandsProp(stepProp);
//             if (listProp == null || !listProp.isArray)
//                 continue;
//
//             SetAllCommandFoldouts(listProp, expanded);
//         }
//
//         SaveFoldouts();
//
//         _commandsList = null;
//         _commandsPropPath = null;
//
//         Repaint();
//     }
//
//     private bool TryGetCurrentNodeStepsProp(out SerializedProperty stepsProp)
//     {
//         stepsProp = null;
//
//         if (_nodesProp == null || !_nodesProp.isArray) return false;
//         if (_selectedNode < 0 || _selectedNode >= _nodesProp.arraySize) return false;
//
//         var nodeProp = _nodesProp.GetArrayElementAtIndex(_selectedNode);
//         if (nodeProp == null) return false;
//
//         var sp = nodeProp.FindPropertyRelative("steps");
//         if (sp == null || !sp.isArray) return false;
//
//         stepsProp = sp;
//         return true;
//     }
//
//     private string GetFoldoutStorageKey()
//     {
//         if (targetSequence == null) return null;
//
//         string assetPath = AssetDatabase.GetAssetPath(targetSequence);
//         if (string.IsNullOrEmpty(assetPath)) return null;
//
//         string guid = AssetDatabase.AssetPathToGUID(assetPath);
//         if (string.IsNullOrEmpty(guid)) return null;
//
//         return FoldoutKeyPrefix + guid;
//     }
//
//     private void SaveFoldouts()
//     {
//         string key = GetFoldoutStorageKey();
//         if (string.IsNullOrEmpty(key)) return;
//
//         var box = new FoldoutStateBox();
//
//         foreach (var kv in _commandFoldoutsByPath)
//         {
//             if (string.IsNullOrEmpty(kv.Key) || kv.Value == null) continue;
//
//             var entry = new PathEntry { path = kv.Key };
//             foreach (var kv2 in kv.Value)
//             {
//                 entry.ids.Add(kv2.Key);
//                 entry.values.Add(kv2.Value);
//             }
//
//             box.entries.Add(entry);
//         }
//
//         string json = JsonUtility.ToJson(box);
//         SessionState.SetString(key, json);
//         EditorPrefs.SetString(key, json);
//     }
//
//     private void LoadFoldouts()
//     {
//         string key = GetFoldoutStorageKey();
//         if (string.IsNullOrEmpty(key)) return;
//
//         string json = SessionState.GetString(key, "");
//         if (string.IsNullOrEmpty(json))
//             json = EditorPrefs.GetString(key, "");
//
//         _commandFoldoutsByPath.Clear();
//
//         if (string.IsNullOrEmpty(json)) return;
//
//         try
//         {
//             var box = JsonUtility.FromJson<FoldoutStateBox>(json);
//             if (box?.entries == null) return;
//
//             foreach (var entry in box.entries)
//             {
//                 if (entry == null || string.IsNullOrEmpty(entry.path)) continue;
//                 if (entry.ids == null || entry.values == null) continue;
//
//                 var map = new Dictionary<long, bool>();
//                 int n = Mathf.Min(entry.ids.Count, entry.values.Count);
//
//                 for (int i = 0; i < n; i++)
//                 {
//                     long id = entry.ids[i];
//                     if (id == 0) continue;
//                     map[id] = entry.values[i];
//                 }
//
//                 _commandFoldoutsByPath[entry.path] = map;
//             }
//         }
//         catch
//         {
//             // ignore broken data
//         }
//     }
// }
// #endif
