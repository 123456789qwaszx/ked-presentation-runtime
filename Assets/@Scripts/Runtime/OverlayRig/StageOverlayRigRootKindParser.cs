using UnityEngine;

public static class StageOverlayRigRootKindParser
{
    public static StageOverlayRigRootKind Parse(
        string raw,
        StageOverlayRigRootKind fallback = StageOverlayRigRootKind.Sprite)
    {
        return TryParse(raw, out StageOverlayRigRootKind kind)
            ? kind
            : fallback;
    }

    public static bool TryParse(
        string raw,
        out StageOverlayRigRootKind kind)
    {
        string s = (raw ?? string.Empty).Trim().ToLowerInvariant();

        switch (s)
        {
            case "":
            case "sprite":
            case "spr":
            case "image":
            case "img":
            case "s":
                kind = StageOverlayRigRootKind.Sprite;
                return true;

            case "text":
            case "txt":
            case "t":
                kind = StageOverlayRigRootKind.Text;
                return true;

            default:
                kind = StageOverlayRigRootKind.Sprite;
                Debug.LogWarning(
                    $"[StageOverlayRigRootKindParser] Unknown root kind '{raw}'. " +
                    $"Fallback to '{kind}'.");
                return false;
        }
    }
}