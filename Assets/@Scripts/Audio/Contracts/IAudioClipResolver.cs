using UnityEngine;

public interface IAudioClipResolver
{
    bool TryResolve(string clipKey, out AudioClip clip);
}