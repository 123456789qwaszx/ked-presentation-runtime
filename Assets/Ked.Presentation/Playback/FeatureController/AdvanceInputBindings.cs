using System;
using UnityEngine;

[Serializable]
public sealed class AdvanceInputBindings
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

    // 그 일은 타이틀과 시스템 메뉴가 한다(framework-scope 4단계).

    public bool IsRapidSkipHeld() => IsHeld(rapidSkipLeft) || IsHeld(rapidSkipRight);
    public bool IsSpeedUpHeld() => IsHeld(speedUpHold);
    public bool IsSpeedUpTogglePressed() => IsPressed(speedUpToggle);
    public bool IsAutoTogglePressed() => IsPressed(autoToggle);
    public bool IsRollbackPressed() => IsPressed(rollback);
    public bool IsAdvancePressed() => IsPressed(advance);


    private static bool IsHeld(KeyCode key)
    {
        return key != KeyCode.None && Input.GetKey(key);
    }

    private static bool IsPressed(KeyCode key)
    {
        return key != KeyCode.None && Input.GetKeyDown(key);
    }
}
