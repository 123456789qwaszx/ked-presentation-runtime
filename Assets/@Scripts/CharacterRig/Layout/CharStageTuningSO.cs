using UnityEngine;

[CreateAssetMenu(menuName = "CPS/CharRig/Tuning/Stage Anchor Tuning", fileName = "CharStageTuning")]
public sealed class CharStageTuningSO : ScriptableObject
{
    [Header("Global Scale Multipliers")]
    public CharScaleTuningSet scales = CharScaleTuningSet.Default;
}