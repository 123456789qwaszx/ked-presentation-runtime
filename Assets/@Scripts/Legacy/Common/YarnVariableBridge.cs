// using UnityEngine;
// using Yarn.Unity;
//
// public sealed class YarnVariableBridge
// {
//     private readonly DialogueRunner _dialogueRunner;
//
//     public YarnVariableBridge(DialogueRunner dialogueRunner)
//     {
//         _dialogueRunner = dialogueRunner;
//     }
//
//     private VariableStorageBehaviour Storage
//     {
//         get
//         {
//             if (_dialogueRunner == null)
//                 return null;
//
//             return _dialogueRunner.VariableStorage;
//         }
//     }
//
//     public float GetFloat(string variableName, float fallback = 0f)
//     {
//         VariableStorageBehaviour storage = Storage;
//
//         if (storage == null)
//             return fallback;
//
//         if (!variableName.StartsWith("$"))
//             variableName = "$" + variableName;
//
//         if (storage.TryGetValue(variableName, out float value))
//             return value;
//
//         return fallback;
//     }
//
//     public bool GetBool(string variableName, bool fallback = false)
//     {
//         VariableStorageBehaviour storage = Storage;
//
//         if (storage == null)
//             return fallback;
//
//         if (!variableName.StartsWith("$"))
//             variableName = "$" + variableName;
//
//         if (storage.TryGetValue(variableName, out bool value))
//             return value;
//
//         return fallback;
//     }
//
//     public string GetString(string variableName, string fallback = "")
//     {
//         VariableStorageBehaviour storage = Storage;
//
//         if (storage == null)
//             return fallback;
//
//         if (!variableName.StartsWith("$"))
//             variableName = "$" + variableName;
//
//         if (storage.TryGetValue(variableName, out string value))
//             return value;
//
//         return fallback;
//     }
//
//     public void SetFloat(string variableName, float value)
//     {
//         VariableStorageBehaviour storage = Storage;
//
//         if (storage == null)
//             return;
//
//         if (!variableName.StartsWith("$"))
//             variableName = "$" + variableName;
//
//         storage.SetValue(variableName, value);
//     }
//
//     public void SetBool(string variableName, bool value)
//     {
//         VariableStorageBehaviour storage = Storage;
//
//         if (storage == null)
//             return;
//
//         if (!variableName.StartsWith("$"))
//             variableName = "$" + variableName;
//
//         storage.SetValue(variableName, value);
//     }
//
//     public void SetString(string variableName, string value)
//     {
//         VariableStorageBehaviour storage = Storage;
//
//         if (storage == null)
//             return;
//
//         if (!variableName.StartsWith("$"))
//             variableName = "$" + variableName;
//
//         storage.SetValue(variableName, value);
//     }
//
//     public void AddFloat(string variableName, float delta)
//     {
//         float current = GetFloat(variableName);
//         SetFloat(variableName, current + delta);
//     }
//     
//     
//     public void ApplyRuntimeStateBeforeDialogue(EpisodeSelectionStateData state)
//     {
//         VariableStorageBehaviour storage = _dialogueRunner.VariableStorage;
//
//         storage.SetValue("$favor", GetWillowFavorFromState(state));
//         storage.SetValue("$trust", GetWillowTrustFromState(state));
//         storage.SetValue("$anger", 0f);
//         storage.SetValue("$contract_signed", state.Flags.TryGetValue("willow_contract_signed", out bool signed) && signed);
//
//         Debug.Log("[YarnCommandBridge] Runtime state applied to Yarn variables.");
//     }
//
//     private float GetWillowFavorFromState(EpisodeSelectionStateData state)
//     {
//         if (state.Stats.TryGetValue("willow_favor", out int value))
//             return value;
//
//         return 0f;
//     }
//
//     private float GetWillowTrustFromState(EpisodeSelectionStateData state)
//     {
//         if (state.Stats.TryGetValue("willow_trust", out int value))
//             return value;
//
//         return 0f;
//     }
//     
//     public void CollectRuntimeStateAfterDialogue(EpisodeSelectionStateData state)
//     {
//         VariableStorageBehaviour storage = _dialogueRunner.VariableStorage;
//
//         if (storage.TryGetValue("$favor", out float favor))
//             state.Stats["willow_favor"] = Mathf.RoundToInt(favor);
//
//         if (storage.TryGetValue("$trust", out float trust))
//             state.Stats["willow_trust"] = Mathf.RoundToInt(trust);
//
//         if (storage.TryGetValue("$contract_signed", out bool contractSigned))
//             state.Flags["willow_contract_signed"] = contractSigned;
//
//         Debug.Log("[YarnCommandBridge] Yarn variables collected into runtime state.");
//     }
// }