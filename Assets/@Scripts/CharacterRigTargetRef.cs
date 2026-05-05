// using System;
// using UnityEngine;
//
// public enum CharacterRigTargetResolveMode
// {
//     Auto,
//     CharacterKey,
//     RoleKey
// }
//
// [Serializable]
// public sealed class CharacterRigTargetRef
// {
//     [Tooltip("Auto: characterKey로 먼저 찾고, 실패하면 roleKey/slotKey로 찾습니다.")]
//     public CharacterRigTargetResolveMode mode = CharacterRigTargetResolveMode.Auto;
//
//     [Tooltip("characterKey 또는 roleKey/slotKey")]
//     public string key;
// }