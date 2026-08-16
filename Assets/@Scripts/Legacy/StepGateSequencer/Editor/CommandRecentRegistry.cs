// #if UNITY_EDITOR
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using UnityEditor;
//
// public static class CommandRecentRegistry
// {
//     // EditorPrefs에 저장할 키
//     private const string PrefKey  = "CPS.SeqRecentCommands";
//     private const int    MaxCount = 8;   // 최근 명령 최대 개수(원하면 조절)
//
//     /// <summary>
//     /// 현재 프로젝트에 존재하는 타입 중에서
//     /// Recent 리스트와 매칭되는 타입들을 순서대로 돌려준다.
//     /// </summary>
//     public static List<Type> GetRecentTypes(IReadOnlyList<Type> allTypes)
//     {
//         var result = new List<Type>();
//         if (allTypes == null || allTypes.Count == 0)
//             return result;
//
//         // 1) 새 키(assembly 포함) 맵
//         var mapNew = new Dictionary<string, Type>(StringComparer.Ordinal);
//         // 2) 구 키(fullname) 맵 (마이그레이션용)
//         var mapOld = new Dictionary<string, Type>(StringComparer.Ordinal);
//
//         foreach (var t in allTypes)
//         {
//             if (t == null) continue;
//
//             var newId = MakeId(t);
//             if (!string.IsNullOrEmpty(newId) && !mapNew.ContainsKey(newId))
//                 mapNew.Add(newId, t);
//
//             var oldId = t.FullName;
//             if (!string.IsNullOrEmpty(oldId) && !mapOld.ContainsKey(oldId))
//                 mapOld.Add(oldId, t);
//         }
//
//         var rawList = LoadRawList();
//         bool dirty = false;
//
//         for (int i = 0; i < rawList.Count; i++)
//         {
//             var id = rawList[i];
//             if (string.IsNullOrEmpty(id)) { dirty = true; continue; }
//
//             // (A) 새 키로 먼저 매칭
//             if (mapNew.TryGetValue(id, out var tNew))
//             {
//                 result.Add(tNew);
//                 continue;
//             }
//
//             // (B) 구버전 값(FullName)이라면 old 맵으로 매칭 후, 새 키로 교체(마이그레이션)
//             if (mapOld.TryGetValue(id, out var tOld))
//             {
//                 result.Add(tOld);
//
//                 var migrated = MakeId(tOld);
//                 if (!string.IsNullOrEmpty(migrated))
//                 {
//                     rawList[i] = migrated; // 교체
//                     dirty = true;
//                 }
//                 continue;
//             }
//
//             // (C) 못 찾으면 제거 대상
//             rawList[i] = null;
//             dirty = true;
//         }
//
//         if (dirty)
//         {
//             rawList.RemoveAll(s => string.IsNullOrEmpty(s));
//             SaveRawList(rawList);
//         }
//
//         return result;
//     }
//
//
//     // -------------------------------------------------
//     // 내부: EditorPrefs <-> List<string> 직렬화 유틸
//     // -------------------------------------------------
//     private static List<string> LoadRawList()
//     {
//         string raw = EditorPrefs.GetString(PrefKey, "");
//         if (string.IsNullOrEmpty(raw))
//             return new List<string>();
//
//         // | 로 구분 (단순하고 문제 없음)
//         return raw
//             .Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
//             .Select(s => s.Trim())
//             .Where(s => !string.IsNullOrEmpty(s))
//             .ToList();
//     }
//
//     private static void SaveRawList(List<string> list)
//     {
//         if (list == null || list.Count == 0)
//         {
//             EditorPrefs.DeleteKey(PrefKey);
//             return;
//         }
//
//         string raw = string.Join("|", list);
//         EditorPrefs.SetString(PrefKey, raw);
//     }
//     
//     private static string MakeId(Type type)
//     {
//         if (type == null) return null;
//
//         // 가장 안전: AssemblyQualifiedName (어셈블리 포함)
//         // 길긴 하지만 안정성 최강.
//         return type.AssemblyQualifiedName;
//     
//         // 짧게 하고 싶으면 아래 방식도 OK (충돌 방지용 assemblyName 포함)
//         // return type.Assembly.GetName().Name + ":" + type.FullName;
//     }
//     
//     public static void Record(Type type)
//     {
//         if (type == null) return;
//
//         string id = MakeId(type);
//         if (string.IsNullOrEmpty(id)) return;
//
//         var list = LoadRawList();
//
//         list.RemoveAll(s => s == id);
//         list.Insert(0, id);
//
//         if (list.Count > MaxCount)
//             list.RemoveRange(MaxCount, list.Count - MaxCount);
//
//         SaveRawList(list);
//     }
// }
// #endif
