public struct UIContext
{
    public string ThemeId;   // e.g., "Light", "Dark"
    public string LocaleId;  // e.g., "ko-KR", "ja-JP"

    public UIContext(
        string themeId,
        string localeId)
    {
        ThemeId        = themeId;
        LocaleId       = localeId;
    }

    public static UIContext Default =>
        new UIContext("Light", "ko-KR");
}