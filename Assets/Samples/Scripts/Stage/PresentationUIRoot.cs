using UnityEngine;

[DisallowMultipleComponent]
public sealed class PresentationUIRoot : MonoBehaviour, IStageMount
{
    [SerializeField]private RectTransform stageViewportLayer;
    [SerializeField]private RectTransform screenEffectLayer;

    public RectTransform StageMount => stageViewportLayer;
    public RectTransform ScreenEffectMount => screenEffectLayer;
}
