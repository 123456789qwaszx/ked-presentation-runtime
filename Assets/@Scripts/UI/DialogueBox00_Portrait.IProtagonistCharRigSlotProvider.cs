using UnityEngine;

public sealed partial class DialogueBox00_Portrait : IProtagonistCharRigSlotProvider
{
    public RectTransform ProtagonistSlot => View.Rect(Refs.DialogueBox00ProtagonistCutinViewport_Mask);
}