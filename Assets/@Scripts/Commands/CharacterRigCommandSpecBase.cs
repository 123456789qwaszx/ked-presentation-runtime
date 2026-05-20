using System;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public abstract class CharacterRigCommandSpecBase : CommandSpecBase
{
    [Header("Character Rig Target")]
    [Tooltip("characterKey 또는 slotKey. Resolver는 characterKey를 먼저 찾고, 없으면 slotKey로 찾습니다.")]
    public string slotKey;
}