public static class TransitionPlayModeParser
{
    public static bool TryParseBlackoutMode(
        string value,
        out TransitionPlayMode playMode,
        out bool forceWait,
        out float holdCoveredSeconds)
    {
        forceWait = false;
        holdCoveredSeconds = 0f;

        switch ((value ?? "").Trim().ToLowerInvariant())
        {
            case "cover":
            case "close":
            case "in":
                playMode = TransitionPlayMode.CoverOnly;
                forceWait = true;
                return true;

            case "uncover":
            case "open":
            case "out":
                playMode = TransitionPlayMode.UncoverOnly;
                forceWait = true;
                return true;

            case "cover_then_uncover":
            case "cover-uncover":
            case "cover_uncover":
            case "full":
                playMode = TransitionPlayMode.CoverThenUncover;
                holdCoveredSeconds = 1.5f;
                return true;

            default:
                playMode = TransitionPlayMode.CoverThenUncover;
                holdCoveredSeconds = 1.5f;
                return false;
        }
    }
}