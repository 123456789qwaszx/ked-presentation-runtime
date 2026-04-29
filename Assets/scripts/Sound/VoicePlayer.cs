// Voice policy: single channel; no overlap; newest request wins.
using UnityEngine;

public sealed class VoicePlayer
{
    private readonly AudioSource _source;

    public AudioClip CurrentClip => _source.clip;
    public bool IsPlaying => _source.isPlaying;

    public VoicePlayer(AudioSource source)
    {
        _source      = source;
        _source.loop = false;
    }
    
    // Interrupt-and-replace policy.
    // A new voice clip stops the previous one immediately.
    public void Play(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("[VoicePlayer] clip is null.");
            return;
        }

        _source.Stop();
        _source.clip = clip;
        _source.Play();
    }

    public void Stop()
    {
        _source.Stop();
        _source.clip = null;
    }
}