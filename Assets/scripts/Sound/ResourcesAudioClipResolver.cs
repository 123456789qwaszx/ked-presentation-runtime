using UnityEngine;

public interface IAudioClipResolver
{
    // 현재: 동기 로딩 (Resources)
    // Addressable 전환 시 비동기 버전으로 교체 필요
    bool TryResolve(string clipKey, out AudioClip clip);
}

public sealed class ResourcesAudioClipResolver : IAudioClipResolver
{
    public bool TryResolve(string clipKey, out AudioClip clip)
    {
        clip = null;

        if (string.IsNullOrWhiteSpace(clipKey))
            return false;

        clip = Resources.Load<AudioClip>(clipKey);
        return clip != null;
    }
}