using UnityEngine;

public static class BackgroundRigRootMaskParser
{
    public static BackgroundRigRootMask Parse(
        string value,
        BackgroundRigRootMask fallback = BackgroundRigRootMask.VisualLayers)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        string normalized = value.Trim().ToLowerInvariant();

        if (TryParseSingle(normalized, out BackgroundRigRootMask singleMask))
            return singleMask;

        BackgroundRigRootMask mask = ParseComposite(value);

        if (mask != BackgroundRigRootMask.None)
            return mask;

        Debug.LogWarning(
            $"[BackgroundRigRootMaskParser] Unknown background root mask '{value}'. Fallback to {fallback}.");

        return fallback;
    }

    private static BackgroundRigRootMask ParseComposite(string value)
    {
        BackgroundRigRootMask mask = BackgroundRigRootMask.None;
        string[] tokens = value.Split('|', ',', '+');

        for (int i = 0; i < tokens.Length; i++)
        {
            string token = tokens[i].Trim().ToLowerInvariant();

            if (TryParseSingle(token, out BackgroundRigRootMask tokenMask))
                mask |= tokenMask;
        }

        return mask;
    }

    private static bool TryParseSingle(string value, out BackgroundRigRootMask mask)
    {
        switch (value)
        {
            case "visual":
            case "visual_layers":
            case "layers":
                mask = BackgroundRigRootMask.VisualLayers;
                return true;

            case "all":
                mask = BackgroundRigRootMask.All;
                return true;

            case "root":
                mask = BackgroundRigRootMask.Background_Root;
                return true;

            case "layer":
            case "layer_root":
                mask = BackgroundRigRootMask.Background_LayerRoot;
                return true;

            case "back":
            case "back_layer":
                mask = BackgroundRigRootMask.Background_BackLayer_Root;
                return true;

            case "object":
            case "objects":
            case "object_slots":
                mask = BackgroundRigRootMask.Background_ObjectSlotRoot;
                return true;

            case "front":
            case "front_layer":
                mask = BackgroundRigRootMask.Background_FrontLayer_Root;
                return true;

            case "extensions":
            case "extension":
                mask = BackgroundRigRootMask.Background_ExtensionsRoot;
                return true;
        }

        mask = BackgroundRigRootMask.None;
        return false;
    }
}