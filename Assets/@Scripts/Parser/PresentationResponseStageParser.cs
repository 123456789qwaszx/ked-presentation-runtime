public static class PresentationResponseStageParser
{
    public static PresentationResponseStage Parse(
        string stageKey,
        PresentationResponseStage fallback = PresentationResponseStage.Stage00)
    {
        if (TryParse(stageKey, out PresentationResponseStage stage))
            return stage;

        return fallback;
    }

    public static bool TryParse(
        string stageKey,
        out PresentationResponseStage stage)
    {
        string key = (stageKey ?? "").Trim().ToLowerInvariant();

        switch (key)
        {
            case "0":
            case "00":
            case "a":
            case "stage0":
            case "stage00":
            case "stage00_root":
                stage = PresentationResponseStage.Stage00;
                return true;

            case "1":
            case "01":
            case "b":
            case "stage1":
            case "stage01":
            case "stage01_root":
                stage = PresentationResponseStage.Stage01;
                return true;

            case "2":
            case "02":
            case "c":
            case "stage2":
            case "stage02":
            case "stage02_root":
                stage = PresentationResponseStage.Stage02;
                return true;

            default:
                stage = default;
                return false;
        }
    }
}