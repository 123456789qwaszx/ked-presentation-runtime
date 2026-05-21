using UnityEngine;

public sealed class BackgroundSpriteResolver
{
    private readonly string _resourcesRoot;

    public BackgroundSpriteResolver(string resourcesRoot = "Backgrounds")
    {
        _resourcesRoot = resourcesRoot;
    }

    public Sprite Resolve(string spriteKey, string caller)
    {
        if (string.IsNullOrEmpty(spriteKey))
        {
            Debug.LogWarning($"[{caller}] spriteKey is null or empty.");
            return null;
        }

        string path = string.IsNullOrEmpty(_resourcesRoot)
            ? spriteKey
            : $"{_resourcesRoot}/{spriteKey}";

        Sprite sprite = Resources.Load<Sprite>(path);

        if (sprite == null)
            Debug.LogWarning($"[{caller}] Failed to resolve background sprite. path='{path}'.");

        return sprite;
    }
}