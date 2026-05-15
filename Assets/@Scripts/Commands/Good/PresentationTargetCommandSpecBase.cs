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
{
    [Header("Presentation Target")]
    public PresentationTarget target = PresentationTarget.Stage00_Root;

    [Header("Resolve")]
    public bool strict = true;
}