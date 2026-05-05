using System;
using UnityEngine;

[Serializable]
public abstract class CharacterRigCommandSpecBase : CommandSpecBase
{
    [Header("Character Rig Target")]
    [Tooltip("characterKey 또는 roleKey/slotKey. Resolver는 characterKey를 먼저 찾고, 없으면 roleKey/slotKey로 찾습니다.")]
    public string roleKey;
}