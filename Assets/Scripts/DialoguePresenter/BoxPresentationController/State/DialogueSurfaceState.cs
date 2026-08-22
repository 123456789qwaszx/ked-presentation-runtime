public sealed class DialogueSurfaceState
{
    // 오버라이드된 프리셋 키. BoxKind로 결정.
    public string OverrideLayoutKey { get; private set; }

    public bool HasOverride => OverrideLayoutKey != null;

    public void SetLayout(string key)
    {
        // 빈 값을 기본 키로.
        OverrideLayoutKey = DialogueSurfaceLayoutPresetDBSO.NormalizeKey(key);
    }

    public void Reset()
    {
        OverrideLayoutKey = null;
    }
}