// using System.Collections.Generic;
// using TMPro;
// using UnityEngine;
// using Yarn.Unity;
//
// public sealed partial class VNOptionsPresenter
// {
//     DialogueRunner _dialogueRunner;
//     
//     public void AttachDialogueRunner(DialogueRunner dialogueRunner)
//     {
//         _dialogueRunner = dialogueRunner;
//     }
//     
//     [SerializeField] private TextMeshProUGUI _accumulatedStatusText;
//     [SerializeField] private bool _hideAccumulatedStatusWhenEmpty = true;
//     
//     private void RefreshAccumulatedStatus(List<VNOptionViewModel> viewModels)
//     {
//         List<string> statKeys = CollectStatKeys(viewModels);
//
//         if (statKeys.Count == 0)
//         {
//             SetAccumulatedStatusText(string.Empty);
//             return;
//         }
//
//         List<string> parts = new List<string>();
//
//         for (int i = 0; i < statKeys.Count; i++)
//         {
//             string statKey = statKeys[i];
//             float value = ReadYarnNumber(_dialogueRunner, statKey);
//
//             string displayName = VNOptionEffectDisplayNameResolver.Resolve(statKey);
//             parts.Add(string.Format("{0} {1}", displayName, FormatNumber(value)));
//         }
//
//         SetAccumulatedStatusText("현재 누적  " + string.Join(" / ", parts));
//     }
//
//     private static List<string> CollectStatKeys(List<VNOptionViewModel> viewModels)
//     {
//         var result = new List<string>();
//
//         if (viewModels == null)
//             return result;
//
//         for (int i = 0; i < viewModels.Count; i++)
//         {
//             VNOptionViewModel viewModel = viewModels[i];
//
//             if (viewModel == null || viewModel.Effects == null)
//                 continue;
//
//             for (int j = 0; j < viewModel.Effects.Count; j++)
//             {
//                 string statKey = viewModel.Effects[j].StatKey;
//
//                 if (string.IsNullOrEmpty(statKey))
//                     continue;
//
//                 if (!result.Contains(statKey))
//                     result.Add(statKey);
//             }
//         }
//
//         return result;
//     }
//
//     
//     private static float ReadYarnNumber(DialogueRunner runner, string statKey)
//     {
//         string variableName = statKey.StartsWith("$") ? statKey : "$" + statKey;
//
//         if (runner.VariableStorage.TryGetValue<float>(variableName, out float floatValue))
//             return floatValue;
//
//         return 0f;
//     }
//
//     private static string FormatNumber(float value)
//     {
//         if (Mathf.Approximately(value, Mathf.Round(value)))
//             return Mathf.RoundToInt(value).ToString();
//
//         return value.ToString("0.##");
//     }
//
//     private void SetAccumulatedStatusText(string text)
//     {
//         _accumulatedStatusText.text = text ?? string.Empty;
//
//         if (_hideAccumulatedStatusWhenEmpty)
//             _accumulatedStatusText.gameObject.SetActive(!string.IsNullOrEmpty(_accumulatedStatusText.text));
//     }
// }