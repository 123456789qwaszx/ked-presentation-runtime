// using System;
// using System.Collections.Generic;
//
// [Serializable]
// public sealed class VNStoryGraphConditionSet
// {
//     public List<string> trueConditionKeys = new();
//
//     public bool Evaluate(string conditionKey)
//     {
//         if (string.IsNullOrWhiteSpace(conditionKey))
//             return true;
//
//         for (int i = 0; i < trueConditionKeys.Count; i++)
//         {
//             if (trueConditionKeys[i] == conditionKey)
//                 return true;
//         }
//
//         return false;
//     }
// }