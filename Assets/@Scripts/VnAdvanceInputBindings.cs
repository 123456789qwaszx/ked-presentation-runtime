using System;
using UnityEngine;

[Serializable]
public sealed class VnAdvanceInputBindings
{
    [Header("Advance")]
    public KeyCode advance = KeyCode.Space;

    [Header("Rapid Skip")]
    public KeyCode rapidSkipLeft = KeyCode.LeftControl;
    public KeyCode rapidSkipRight = KeyCode.RightControl;

    [Header("SpeedUp Mode")]
    public KeyCode speedUpHold = KeyCode.None;
    public KeyCode speedUpToggle = KeyCode.S;

    [Header("VN Features")]
    public KeyCode autoToggle = KeyCode.A;
    public KeyCode rollback = KeyCode.R;

    public bool IsRapidSkipHeld()
    {
        return IsHeld(rapidSkipLeft) || IsHeld(rapidSkipRight);
    }

    public bool IsSpeedUpHeld()
    {
        return IsHeld(speedUpHold);
    }

    public bool IsSpeedUpTogglePressed()
    {
        return IsPressed(speedUpToggle);
    }

    public bool IsAutoTogglePressed()
    {
        return IsPressed(autoToggle);
    }

    public bool IsRollbackPressed()
    {
        return IsPressed(rollback);
    }

    public bool IsAdvancePressed()
    {
        return IsPressed(advance);
    }

    private static bool IsHeld(KeyCode key)
    {
        return key != KeyCode.None && Input.GetKey(key);
    }

    private static bool IsPressed(KeyCode key)
    {
        return key != KeyCode.None && Input.GetKeyDown(key);
    }
}