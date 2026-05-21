using System;
using UnityEngine;

public enum PresentationDirection
{
    Left,
    Right,
    Up,
    Down
}

[Serializable]
public abstract class PresentationTargetCommandSpecBase : CommandSpecBase
{//***
    [Header("Presentation Target")]
    public RectTransform target = UIManager.Instance.GetUI<PresentationUIRoot>().Stage00BackgroundSlot;

    [Header("Resolve")]
    public bool strict = true;
}