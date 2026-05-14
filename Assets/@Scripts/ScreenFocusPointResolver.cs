using System;
using UnityEngine;

public enum ScreenFocusPoint
{
    Center,

    TopLeft,
    Top,
    TopRight,

    Left,
    Right,

    BottomLeft,
    Bottom,
    BottomRight,

    ThirdsUpperLeft,
    ThirdsUpperRight,
    ThirdsLowerLeft,
    ThirdsLowerRight
}

public static class ScreenFocusPointResolver
{
    public static Vector2 Resolve(RectTransform frameRoot, ScreenFocusPoint point)
    {
        if (frameRoot == null)
            return Vector2.zero;

        Rect rect = frameRoot.rect;
        float w = rect.width;
        float h = rect.height;

        switch (point)
        {
            case ScreenFocusPoint.TopLeft:
                return new Vector2(-w / 3f, h / 3f);

            case ScreenFocusPoint.Top:
                return new Vector2(0f, h / 3f);

            case ScreenFocusPoint.TopRight:
                return new Vector2(w / 3f, h / 3f);

            case ScreenFocusPoint.Left:
                return new Vector2(-w / 3f, 0f);

            case ScreenFocusPoint.Right:
                return new Vector2(w / 3f, 0f);

            case ScreenFocusPoint.BottomLeft:
                return new Vector2(-w / 3f, -h / 3f);

            case ScreenFocusPoint.Bottom:
                return new Vector2(0f, -h / 3f);

            case ScreenFocusPoint.BottomRight:
                return new Vector2(w / 3f, -h / 3f);

            case ScreenFocusPoint.ThirdsUpperLeft:
                return new Vector2(-w / 6f, h / 6f);

            case ScreenFocusPoint.ThirdsUpperRight:
                return new Vector2(w / 6f, h / 6f);

            case ScreenFocusPoint.ThirdsLowerLeft:
                return new Vector2(-w / 6f, -h / 6f);

            case ScreenFocusPoint.ThirdsLowerRight:
                return new Vector2(w / 6f, -h / 6f);

            default:
                return Vector2.zero;
        }
    }
}

public static class ScreenFocusPointParser
{
    public static bool TryParse(string raw, out ScreenFocusPoint point)
    {
        point = ScreenFocusPoint.Center;

        if (string.IsNullOrWhiteSpace(raw))
            return false;

        string s = raw.Trim().ToLowerInvariant();
        s = s.Replace("-", "_");
        s = s.Replace(".", "_");

        switch (s)
        {
            case "center":
            case "c":
            case "middle":
                point = ScreenFocusPoint.Center;
                return true;

            case "top_left":
            case "topleft":
            case "tl":
                point = ScreenFocusPoint.TopLeft;
                return true;

            case "top":
            case "t":
                point = ScreenFocusPoint.Top;
                return true;

            case "top_right":
            case "topright":
            case "tr":
                point = ScreenFocusPoint.TopRight;
                return true;

            case "left":
            case "l":
                point = ScreenFocusPoint.Left;
                return true;

            case "right":
            case "r":
                point = ScreenFocusPoint.Right;
                return true;

            case "bottom_left":
            case "bottomleft":
            case "bl":
                point = ScreenFocusPoint.BottomLeft;
                return true;

            case "bottom":
            case "b":
                point = ScreenFocusPoint.Bottom;
                return true;

            case "bottom_right":
            case "bottomright":
            case "br":
                point = ScreenFocusPoint.BottomRight;
                return true;

            case "thirds_upper_left":
            case "third_upper_left":
            case "rule_upper_left":
            case "rule_ul":
            case "third_ul":
                point = ScreenFocusPoint.ThirdsUpperLeft;
                return true;

            case "thirds_upper_right":
            case "third_upper_right":
            case "rule_upper_right":
            case "rule_ur":
            case "third_ur":
                point = ScreenFocusPoint.ThirdsUpperRight;
                return true;

            case "thirds_lower_left":
            case "third_lower_left":
            case "rule_lower_left":
            case "rule_ll":
            case "third_ll":
                point = ScreenFocusPoint.ThirdsLowerLeft;
                return true;

            case "thirds_lower_right":
            case "third_lower_right":
            case "rule_lower_right":
            case "rule_lr":
            case "third_lr":
                point = ScreenFocusPoint.ThirdsLowerRight;
                return true;
        }

        return Enum.TryParse(raw.Trim(), true, out point);
    }
}