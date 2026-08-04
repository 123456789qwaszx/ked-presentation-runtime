using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class InlineSfxPlaybackHost : MonoBehaviour
{
    [Serializable]
    private struct CueMapEntry
    {
        public string cue;
        public string clipKey;
    }

    private AudioSystem _audioSystem;

    [Header("Cue -> ClipKey")]
    [SerializeField] private List<CueMapEntry> cueMap = new();

    private Dictionary<string, string> _cueToClipKey;

    public void Initialize(AudioSystem audioSystem)
    {
        _audioSystem = audioSystem;

        RebuildCueLookup();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        RebuildCueLookup();
    }
#endif

    public void PlaySfxCue(string cue, float gain = 1f)
    {
        if (string.IsNullOrWhiteSpace(cue))
        {
            Debug.LogWarning("[InlineSfxPlaybackHost] PlaySfxCue called with null or empty cue.", this);
            return;
        }

        string clipKey = ResolveCueToClipKey(cue);

        if (!ResourcesAudioClipResolver.TryResolve(clipKey, out AudioClip clip))
        {
            Debug.LogWarning($"[InlineSfxPlaybackHost] Failed to resolve SFX cue. cue={cue}, clipKey={clipKey}", this);
            return;
        }

        _audioSystem.Sfx.Play(clip, Mathf.Max(0f, gain));
    }

    private string ResolveCueToClipKey(string cue)
    {
        if (_cueToClipKey == null)
        {
            Debug.LogWarning($"[InlineSfxPlaybackHost] Cue lookup is not ready. Initialize() may not have been called. Using cue as clipKey. cue={cue}", this);
            return cue;
        }

        if (!_cueToClipKey.TryGetValue(cue, out string clipKey))
            return cue;
        
        if (string.IsNullOrWhiteSpace(clipKey))
        {
            Debug.LogWarning($"[InlineSfxPlaybackHost] Cue map entry has empty clipKey. Using cue as clipKey. cue={cue}", this);
            return cue;
        }
        
        return clipKey;
    }

    private void RebuildCueLookup()
    {
        if (_cueToClipKey == null)
            _cueToClipKey = new Dictionary<string, string>(StringComparer.Ordinal);
        else
            _cueToClipKey.Clear();

        if (cueMap == null)
            return;

        foreach (CueMapEntry entry in cueMap)
        {
            if (string.IsNullOrWhiteSpace(entry.cue))
                continue;

            _cueToClipKey[entry.cue] = entry.clipKey;
        }
    }
}