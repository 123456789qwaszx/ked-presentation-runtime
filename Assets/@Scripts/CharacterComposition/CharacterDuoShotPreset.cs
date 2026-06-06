using System;
using UnityEngine;

public enum CharacterDuoShotPreset
{
    Balanced = 0,
    Close = 10,
    Wide = 20,
    Confrontation = 30,
    LeftDominant = 40,
    RightDominant = 50
}

[Serializable]
public sealed class CharacterDuoShotSideLayout
{
    public CharacterFocusPreset focusPreset = CharacterFocusPreset.Face;
    public string customFocusKey = "";
    public Vector2 focusOffset = Vector2.zero;

    public ScreenFocusPoint screenPoint = ScreenFocusPoint.ThirdsUpperLeft;
    public Vector2 screenOffset = Vector2.zero;

    public Vector2 scale = Vector2.one;
}

[Serializable]
public sealed class CharacterDuoShotLayout
{
    public CharacterDuoShotSideLayout left = new CharacterDuoShotSideLayout();
    public CharacterDuoShotSideLayout right = new CharacterDuoShotSideLayout();
}

public static class CharacterDuoShotPresetResolver
{
    public static CharacterDuoShotLayout Resolve(CharacterDuoShotPreset preset)
    {
        switch (preset)
        {
            case CharacterDuoShotPreset.Close:
                return Close();

            case CharacterDuoShotPreset.Wide:
                return Wide();

            case CharacterDuoShotPreset.Confrontation:
                return Confrontation();

            case CharacterDuoShotPreset.LeftDominant:
                return LeftDominant();

            case CharacterDuoShotPreset.RightDominant:
                return RightDominant();

            default:
                return Balanced();
        }
    }

    private static CharacterDuoShotLayout Balanced()
    {
        return new CharacterDuoShotLayout
        {
            left = new CharacterDuoShotSideLayout
            {
                focusPreset = CharacterFocusPreset.Face,
                screenPoint = ScreenFocusPoint.ThirdsUpperLeft,
                screenOffset = new Vector2(-80f, 0f),
                scale = Vector2.one
            },
            right = new CharacterDuoShotSideLayout
            {
                focusPreset = CharacterFocusPreset.Face,
                screenPoint = ScreenFocusPoint.ThirdsUpperRight,
                screenOffset = new Vector2(80f, 0f),
                scale = Vector2.one
            }
        };
    }

    private static CharacterDuoShotLayout Close()
    {
        return new CharacterDuoShotLayout
        {
            left = new CharacterDuoShotSideLayout
            {
                focusPreset = CharacterFocusPreset.Face,
                screenPoint = ScreenFocusPoint.ThirdsUpperLeft,
                screenOffset = new Vector2(40f, 20f),
                scale = new Vector2(1.08f, 1.08f)
            },
            right = new CharacterDuoShotSideLayout
            {
                focusPreset = CharacterFocusPreset.Face,
                screenPoint = ScreenFocusPoint.ThirdsUpperRight,
                screenOffset = new Vector2(-40f, 20f),
                scale = new Vector2(1.08f, 1.08f)
            }
        };
    }

    private static CharacterDuoShotLayout Wide()
    {
        return new CharacterDuoShotLayout
        {
            left = new CharacterDuoShotSideLayout
            {
                focusPreset = CharacterFocusPreset.Face,
                screenPoint = ScreenFocusPoint.ThirdsUpperLeft,
                screenOffset = new Vector2(-180f, -20f),
                scale = new Vector2(0.92f, 0.92f)
            },
            right = new CharacterDuoShotSideLayout
            {
                focusPreset = CharacterFocusPreset.Face,
                screenPoint = ScreenFocusPoint.ThirdsUpperRight,
                screenOffset = new Vector2(180f, -20f),
                scale = new Vector2(0.92f, 0.92f)
            }
        };
    }

    private static CharacterDuoShotLayout Confrontation()
    {
        return new CharacterDuoShotLayout
        {
            left = new CharacterDuoShotSideLayout
            {
                focusPreset = CharacterFocusPreset.Face,
                screenPoint = ScreenFocusPoint.ThirdsUpperLeft,
                screenOffset = new Vector2(10f, 10f),
                scale = new Vector2(1.04f, 1.04f)
            },
            right = new CharacterDuoShotSideLayout
            {
                focusPreset = CharacterFocusPreset.Face,
                screenPoint = ScreenFocusPoint.ThirdsUpperRight,
                screenOffset = new Vector2(-10f, 10f),
                scale = new Vector2(1.04f, 1.04f)
            }
        };
    }

    private static CharacterDuoShotLayout LeftDominant()
    {
        return new CharacterDuoShotLayout
        {
            left = new CharacterDuoShotSideLayout
            {
                focusPreset = CharacterFocusPreset.Face,
                screenPoint = ScreenFocusPoint.ThirdsUpperLeft,
                screenOffset = new Vector2(20f, 30f),
                scale = new Vector2(1.14f, 1.14f)
            },
            right = new CharacterDuoShotSideLayout
            {
                focusPreset = CharacterFocusPreset.Face,
                screenPoint = ScreenFocusPoint.ThirdsUpperRight,
                screenOffset = new Vector2(120f, -20f),
                scale = new Vector2(0.94f, 0.94f)
            }
        };
    }

    private static CharacterDuoShotLayout RightDominant()
    {
        return new CharacterDuoShotLayout
        {
            left = new CharacterDuoShotSideLayout
            {
                focusPreset = CharacterFocusPreset.Face,
                screenPoint = ScreenFocusPoint.ThirdsUpperLeft,
                screenOffset = new Vector2(-120f, -20f),
                scale = new Vector2(0.94f, 0.94f)
            },
            right = new CharacterDuoShotSideLayout
            {
                focusPreset = CharacterFocusPreset.Face,
                screenPoint = ScreenFocusPoint.ThirdsUpperRight,
                screenOffset = new Vector2(-20f, 30f),
                scale = new Vector2(1.14f, 1.14f)
            }
        };
    }
}

public static class CharacterDuoShotPresetParser
{
    public static CharacterDuoShotPreset Parse(
        string raw,
        CharacterDuoShotPreset fallback = CharacterDuoShotPreset.Balanced)
    {
        if (TryParse(raw, out CharacterDuoShotPreset preset))
            return preset;

        return fallback;
    }

    public static bool TryParse(string raw, out CharacterDuoShotPreset preset)
    {
        preset = CharacterDuoShotPreset.Balanced;

        if (string.IsNullOrWhiteSpace(raw))
            return false;

        string s = raw.Trim().ToLowerInvariant();
        s = s.Replace("-", "_");
        s = s.Replace(".", "_");

        switch (s)
        {
            case "balanced":
            case "balance":
            case "normal":
            case "default":
                preset = CharacterDuoShotPreset.Balanced;
                return true;

            case "close":
            case "near":
                preset = CharacterDuoShotPreset.Close;
                return true;

            case "wide":
            case "far":
                preset = CharacterDuoShotPreset.Wide;
                return true;

            case "confrontation":
            case "fight":
            case "versus":
            case "vs":
                preset = CharacterDuoShotPreset.Confrontation;
                return true;

            case "left_dominant":
            case "left":
            case "left_dom":
                preset = CharacterDuoShotPreset.LeftDominant;
                return true;

            case "right_dominant":
            case "right":
            case "right_dom":
                preset = CharacterDuoShotPreset.RightDominant;
                return true;
        }

        return Enum.TryParse(raw.Trim(), true, out preset);
    }
}