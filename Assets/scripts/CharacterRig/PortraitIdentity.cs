using System;
using UnityEngine;

[Serializable]
public sealed class PortraitIdentity
{
    [Tooltip("캐릭터 ID (Amber)")]
    public string character;

    [Tooltip("표정 (neutral / smile / angry / 2 / 02)")]
    public string emotion;

    [Tooltip("의상/변형 (비우면 default)")]
    public string variant;
}