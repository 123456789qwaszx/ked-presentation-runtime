using UnityEngine;

[CreateAssetMenu(menuName = "CPS/CharRig/Tuning/Stage Anchor Tuning", fileName = "CharStageTuning")]
public sealed class CharStageTuningSO : ScriptableObject
{
    [Header("Global Offsets (px)")]
    public CharPlacementTuningSet offsets = default;
    
    [Header("Global Scale Multipliers")]
    public CharScaleTuningSet scales = CharScaleTuningSet.Default;
}