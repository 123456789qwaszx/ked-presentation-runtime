using System;

public static class CharRigSlotParser
{
    public static bool TryParse(string raw, out CharRigSlot slot)
    {
        slot = default;

        if (string.IsNullOrWhiteSpace(raw))
            return false;

        string s = raw.Trim();

        if (TryParseAlias(s, out slot))
            return true;

        return Enum.TryParse(s, true, out slot);
    }

    private static bool TryParseAlias(string raw, out CharRigSlot slot)
    {
        slot = default;

        string s = raw.Trim().ToLowerInvariant();

        switch (s)
        {
            case "a":
            case "s0":
            case "0":
            case "stage00":
            case "stage0":
                slot = CharRigSlot.Stage00CharacterSlot;
                return true;

            case "b":
            case "s1":
            case "1":
            case "stage01":
            case "stage1":
                slot = CharRigSlot.Stage01CharacterSlot;
                return true;

            case "c":
            case "s2":
            case "2":
            case "stage02":
            case "stage2":
                slot = CharRigSlot.Stage02CharacterSlot;
                return true;

            case "d":
            case "s3":
            case "me":
            case "protagonist":
            case "protagonistslot":
            case "protagonist_slot":
            case "boxside":
            case "portrait":
                slot = CharRigSlot.ProtagonistSlot;
                return true;
        }

        return false;
    }

    public static string GetUsage()
    {
        return "Use 'a/s0/stage00', 'b/s1/stage01', 'c/s2/stage02', or 'd/me/protagonist'.";
    }
}