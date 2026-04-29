using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct SfxCueMapEntry
{
    public string cue;
    public string clipKey;
}

public sealed class InlineSfxHost : MonoBehaviour, InlineEventMarkupHandler.IInlineAudioHost
{
    private AudioSystem _audioSystem;

    [Header("Cue -> ClipKey")]
    [SerializeField] private List<SfxCueMapEntry> cueMap = new();

    private IAudioClipResolver _clipResolver;
    private Dictionary<string, string> _cueToClipKey;

    public void Initialize(AudioSystem audioSystem, IAudioClipResolver clipResolver)
    {
        _audioSystem = audioSystem;
        _clipResolver = clipResolver;
        RebuildMap();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        RebuildMap();
    }
#endif

    public void PlaySfxCue(string cue, float gain = 1f)
    {
        if (string.IsNullOrWhiteSpace(cue))
        {
            Debug.LogWarning("[InlineSfxHost] PlaySfxCue called with null or empty cue.");
            return;
        }

        string clipKey = ResolveCueToClipKey(cue);

        if (string.IsNullOrWhiteSpace(clipKey))
        {
            Debug.LogWarning($"[InlineSfxHost] ClipKey is empty. cue={cue}", this);
            return;
        }

        if (!_clipResolver.TryResolve(clipKey, out AudioClip clip) || clip == null)
        {
            Debug.LogWarning($"[InlineSfxHost] Failed to resolve SFX cue. cue={cue}, clipKey={clipKey}", this);
            return;
        }

        _audioSystem.Sfx.Play(clip, Mathf.Max(0f, gain));
    }

    private string ResolveCueToClipKey(string cue)
    {
        if (_cueToClipKey == null)
        {
            Debug.LogWarning($"[InlineSfxHost] Cue map is null. Falling back to raw cue as clipKey. cue={cue}");
            return cue;
        }

        if (_cueToClipKey.TryGetValue(cue, out string clipKey))
            return clipKey;

        return cue;
    }

    private void RebuildMap()
    {
        _cueToClipKey = new Dictionary<string, string>(StringComparer.Ordinal);

        if (cueMap == null)
            return;

        for (int i = 0; i < cueMap.Count; i++)
        {
            SfxCueMapEntry entry = cueMap[i];

            if (string.IsNullOrWhiteSpace(entry.cue))
                continue;

            _cueToClipKey[entry.cue] = entry.clipKey;
        }
    }
}