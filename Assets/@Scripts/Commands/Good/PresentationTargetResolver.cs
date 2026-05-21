// using System;
// using System.Collections;
// using DG.Tweening;
// using UnityEngine;
//
//
// public static class PresentationTargetResolver
// {
//     public static RectTransform ResolveRect(
//         CommandRunScope scope,
//         PresentationTarget target,
//         bool strict,
//         string commandName)
//     {
//         if (scope == null)
//         {
//             if (strict)
//                 throw new InvalidOperationException($"[{commandName}] scope is null.");
//
//             return null;
//         }
//
//         if (scope.Presentation == null)
//         {
//             if (strict)
//             {
//                 throw new InvalidOperationException(
//                     $"[{commandName}] Presentation refs are not bound. " +
//                     "Call <<setup_presentation>> before using PresentationTarget commands.");
//             }
//
//             return null;
//         }
//
//         RectTransform rect = scope.Presentation.GetRect(target);
//
//         if (rect == null && strict)
//         {
//             throw new InvalidOperationException(
//                 $"[{commandName}] PresentationTarget rect not found. target={target}");
//         }
//
//         return rect;
//     }
// }