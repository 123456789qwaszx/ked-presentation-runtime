public sealed class DialogueSurfaceState
{
    public string CurrentLayoutKey { get; private set; } =
        DialogueSurfaceLayoutPresetDBSO.DefaultPresetKey;

    public void SetLayout(string key)
    {
        CurrentLayoutKey = DialogueSurfaceLayoutPresetDBSO.NormalizeKey(key);
    }

    public void Reset()
    {
        CurrentLayoutKey = DialogueSurfaceLayoutPresetDBSO.DefaultPresetKey;
    }
}