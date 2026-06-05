using System;
using UnityEngine;

// Shot intent state.
// This is not a final Transform state.
// zoom, pan, and focusPoint are authored/logical values used to solve camera roots and response targets.
[Serializable]
public struct PresentationIntentState
{
    [Range(-10f, 10f)]
    public float zoom; // Camera zoom intensity
    public Vector2 pan;  // Camera pan offset in rig space
    public Vector2 focusPoint; // Target position in rig space (Stage_Root) the camera should focus on

    public static PresentationIntentState Default => new ()
    {
        zoom = 0f,
        pan = Vector2.zero,
        focusPoint = Vector2.zero,
    };
}