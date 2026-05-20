public static class CharRigDirectionParser
{
    public static CharRigDirection ParseSlideDirection(
        string direction,
        CharRigDirection fallback = CharRigDirection.Left)
    {
        if (TryParseSlideDirection(direction, out CharRigDirection result))
            return result;

        return fallback;
    }

    public static bool TryParseSlideDirection(
        string direction,
        out CharRigDirection result)
    {
        switch (direction?.Trim().ToLowerInvariant())
        {
            case "left":
            case "l":
                result = CharRigDirection.Left;
                return true;

            case "right":
            case "r":
                result = CharRigDirection.Right;
                return true;

            case "up":
            case "u":
            case "top":
            case "t":
                result = CharRigDirection.Up;
                return true;

            case "down":
            case "d":
            case "bottom":
            case "b":
                result = CharRigDirection.Down;
                return true;

            default:
                result = default;
                return false;
        }
    }
}