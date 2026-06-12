public static class PresentationKeyNormalizer
{
    public static string NormalizeCharacterKey(string key)
    {
        return (key ?? "").Trim().ToLowerInvariant();
    }

    public static string NormalizeVariantKey(string key)
    {
        return (key ?? "").Trim().ToLowerInvariant();
    }

    public static char NormalizeVariantSuffix(string variantKey)
    {
        variantKey = NormalizeVariantKey(variantKey);

        if (variantKey.Length == 0)
            return '\0';

        return variantKey[^1];
    }
}