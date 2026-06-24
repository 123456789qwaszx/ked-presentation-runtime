public static class ScreenEffectRuntime
{
    private static ScreenEffectRigProvider _provider;

    public static IScreenEffectHost Host => _provider != null
        ? _provider.Host
        : null;

    public static void Bind(ScreenEffectRigProvider provider)
    {
        if (provider == null)
            return;

        _provider = provider;
    }

    public static void Unbind(ScreenEffectRigProvider provider)
    {
        if (_provider == provider)
            _provider = null;
    }
}