// #if UNITY_EDITOR
// using System;
// using System.Collections.Generic;
// using UnityEditor;
// using UnityEngine;
//
// [InitializeOnLoad]
// public static class SequenceEditorMenuInstaller
// {
//     static SequenceEditorMenuInstaller()
//     {
//         SequenceEditorMenuHooks.ShowCommandMenu =
//             (allTypes, onSingle, onBatch, extendMenu) =>
//             {
//                 var menu = new GenericMenu();
//
//                 // Record를 강제로 보장하는 래퍼
//                 Action<Type> onSingleWithRecent = t =>
//                 {
//                     if (t != null) CommandRecentRegistry.Record(t);
//                     onSingle?.Invoke(t);
//                 };
//
//                 Action<IReadOnlyList<Type>> onBatchWithRecent = types =>
//                 {
//                     if (types != null)
//                     {
//                         // 정책 선택:
//                         // 1) 전부 기록
//                         foreach (var t in types)
//                             if (t != null) CommandRecentRegistry.Record(t);
//
//                         // 2) 또는 "마지막으로 추가한 것만" 원하면 위 foreach 대신 이것만:
//                         // var last = types.Count > 0 ? types[types.Count - 1] : null;
//                         // if (last != null) CommandRecentRegistry.Record(last);
//                     }
//
//                     onBatch?.Invoke(types);
//                 };
//
//                 // 1) Sets
//                 CommandMenuUtility.BuildSetsMenu(menu, allTypes, onSingleWithRecent, onBatchWithRecent);
//                 menu.AddSeparator("");
//
//                 // 2) Recent
//                 AddRecentSection(menu, allTypes, onSingleWithRecent);
//                 menu.AddSeparator("");
//
//                 // 3) Category
//                 CommandMenuUtility.BuildCategoryMenu(menu, allTypes, onSingleWithRecent);
//
//                 // 4) Extension
//                 extendMenu?.Invoke(menu);
//
//                 menu.ShowAsContext();
//                 return true;
//             };
//     }
//
//     private static void AddRecentSection(GenericMenu menu, IReadOnlyList<Type> allTypes, Action<Type> onSingle)
//     {
//         var recent = CommandRecentRegistry.GetRecentTypes(allTypes);
//
//         if (recent == null || recent.Count == 0)
//         {
//             menu.AddDisabledItem(new GUIContent("Recent/(empty)"));
//             return;
//         }
//
//         foreach (var t in recent)
//         {
//             var tt = t;
//             string label = GetDisplayLabel(tt);
//             menu.AddItem(new GUIContent($"Recent/{label}"), false, () => onSingle(tt));
//         }
//     }
//
//     private static string GetDisplayLabel(Type t)
//     {
//         if (t == null) return "(null)";
//
//         var hint = (CommandMenuHintAttribute)Attribute.GetCustomAttribute(t, typeof(CommandMenuHintAttribute));
//         string label = hint != null ? hint.DisplayName : null;
//
//         if (string.IsNullOrWhiteSpace(label))
//             label = t.Name;
//
//         return label.Trim();
//     }
// }
// #endif
