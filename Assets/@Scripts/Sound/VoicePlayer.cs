// VoicePlayer.cs
// 책임: Voice 단일 채널 재생, 새 재생 요청 시 이전 것 중단

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