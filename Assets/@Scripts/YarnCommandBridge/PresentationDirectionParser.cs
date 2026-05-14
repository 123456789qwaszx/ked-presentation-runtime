public static class PresentationDirectionParser
{
    public static bool TryParse(string raw, out PresentationDirection direction)
    {
        direction = PresentationDirection.Left;

        if (string.IsNullOrWhiteSpace(raw))
            return false;

        string s = raw.Trim().ToLowerInvariant();

        switch (s)
        {
            case "left":
            case "l":
                direction = PresentationDirection.Left;
                return true;

            case "right":
            case "r":
                direction = PresentationDirection.Right;
                return true;

            case "up":
            case "u":
                direction = PresentationDirection.Up;
                return true;

            case "down":
            case "d":
                direction = PresentationDirection.Down;
                return true;
        }

        return false;
    }
}