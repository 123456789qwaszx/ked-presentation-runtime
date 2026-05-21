using System;
using UnityEngine;

public static class BackgroundRigSlotParser
{
    public static BackgroundRigSlot Parse(string value, BackgroundRigSlot fallback)
    {
        string normalized = (value ?? "").Trim().ToLowerInvariant();

        switch (normalized)
        {
            case "stage00":
            case "stage0":
            case "s0":
            case "a":
            case "0":
                return BackgroundRigSlot.Stage00BackgroundSlot;

            case "stage01":
            case "stage1":
            case "s1":
            case "b":
            case "1":
                return BackgroundRigSlot.Stage01BackgroundSlot;

            case "stage02":
            case "stage2":
            case "s2":
            case "c":
            case "2":
                return BackgroundRigSlot.Stage02BackgroundSlot;

            default:
                Debug.LogWarning(
                    $"[BackgroundRigSlotParser] Unknown background rig slot '{value}'. " +
                    $"Fallback to '{fallback}'.");
                return fallback;
        }
    }
}