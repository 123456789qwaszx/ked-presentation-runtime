using System;
using UnityEngine;

[Serializable]
public sealed class VNAdvanceInputBindings
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

    public KeyCode runYarn = KeyCode.Alpha2;

    public KeyCode runEpisodeChain = KeyCode.Alpha3;

    public KeyCode loadProgression = KeyCode.Alpha4;

    public KeyCode newGame = KeyCode.Alpha5;

    public bool IsRapidSkipHeld() => IsHeld(rapidSkipLeft) || IsHeld(rapidSkipRight);
    public bool IsSpeedUpHeld() => IsHeld(speedUpHold);
    public bool IsSpeedUpTogglePressed() => IsPressed(speedUpToggle);
    public bool IsAutoTogglePressed() => IsPressed(autoToggle);
    public bool IsRollbackPressed() => IsPressed(rollback);
    public bool IsAdvancePressed() => IsPressed(advance);
    public bool IsRunYarnPressed() => IsPressed(runYarn);
    public bool IsRunEpisodeChainPressed() => IsPressed(runEpisodeChain);
    public bool IsLoadProgressionPressed() => IsPressed(loadProgression);
    public bool IsNewGamePressed() => IsPressed(newGame);


    private static bool IsHeld(KeyCode key)
    {
        return key != KeyCode.None && Input.GetKey(key);
    }

    private static bool IsPressed(KeyCode key)
    {
        return key != KeyCode.None && Input.GetKeyDown(key);
    }
}
