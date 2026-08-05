using System;
using UnityEngine;

// Authored shot intent state.
// This is not a final Transform state.
// Values are solved later into camera root transforms and per-target response transforms.
[Serializable]
public struct PresentationIntentState
{
    // Authored zoom intent, not final camera scale.
    public float zoom;

    // Camera pan offset in shared rig space.
    public Vector2 panInRigSpace;

    // Logical focus point in shared rig space.
    // Camera framing and target spread are solved relative to this point.
    public Vector2 focusPointInRigSpace;

    public static PresentationIntentState Default => new()
    {
        zoom = 0f,
        panInRigSpace = Vector2.zero,
        focusPointInRigSpace = Vector2.zero,
    };
}

// 코어 ShotIntentState(샷 축의 리듀서 상태)와의 다리. 필드 대응은 1:1이다.
public static class PresentationIntentStateCoreBridge
{
    public static Ked.Presentation.Core.ShotIntentState ToCore(in PresentationIntentState state)
    {
        return new Ked.Presentation.Core.ShotIntentState(
            state.zoom,
            new Ked.Presentation.Core.Vec2(state.panInRigSpace.x, state.panInRigSpace.y),
            new Ked.Presentation.Core.Vec2(state.focusPointInRigSpace.x, state.focusPointInRigSpace.y));
    }

    public static PresentationIntentState FromCore(in Ked.Presentation.Core.ShotIntentState state)
    {
        return new PresentationIntentState
        {
            zoom = state.Zoom,
            panInRigSpace = new Vector2(state.PanInRigSpace.X, state.PanInRigSpace.Y),
            focusPointInRigSpace = new Vector2(state.FocusPointInRigSpace.X, state.FocusPointInRigSpace.Y),
        };
    }
}