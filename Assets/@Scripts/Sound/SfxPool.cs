// SfxPool.cs
// 책임: SFX 다중 채널 풀 관리, overflow 시 oldest 교체

using System.Collections.Generic;
using UnityEngine;

public sealed class SfxPool
{
    private readonly List<AudioSource> _pool;
    private readonly int _maxSize;

    public SfxPool(List<AudioSource> sources, int maxSize)
    {
        _pool    = sources;
        _maxSize = maxSize;
    }

    public void Play(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("[SfxPool] clip is null.");
            return;
        }

        AudioSource source = GetAvailable();
        source.clip = clip;
        source.Play();
    }

    public void StopAll()
    {
        for (int i = 0; i < _pool.Count; i++)
        {
            _pool[i].Stop();
            _pool[i].clip = null;
        }
    }

    // ── 사용 가능한 소스 반환 ─────────────────────────────────
    private AudioSource GetAvailable()
    {
        // 1. 재생 중이 아닌 소스 우선
        for (int i = 0; i < _pool.Count; i++)
        {
            if (!_pool[i].isPlaying)
                return _pool[i];
        }

        // 2. 풀이 maxSize 미만이면 확장 (soft cap까지)
        if (_pool.Count < _maxSize)
        {
            AudioSource newSource = CreateSource();
            _pool.Add(newSource);
            return newSource;
        }

        // 3. 풀이 꽉 찼으면 oldest 교체
        return GetOldest();
    }

    private AudioSource GetOldest()
    {
        AudioSource oldest  = _pool[0];
        float       minTime = _pool[0].time;

        for (int i = 1; i < _pool.Count; i++)
        {
            if (_pool[i].time < minTime)
            {
                minTime = _pool[i].time;
                oldest  = _pool[i];
            }
        }

        oldest.Stop();
        return oldest;
    }

    private AudioSource CreateSource()
    {
        // 풀의 첫 번째 소스의 GameObject에서 새 AudioSource 추가
        AudioSource newSource = _pool[0].gameObject.AddComponent<AudioSource>();
        newSource.loop        = false;
        newSource.playOnAwake = false;
        
        // Mixer 출력 그룹 복사
        newSource.outputAudioMixerGroup = _pool[0].outputAudioMixerGroup;
        
        return newSource;
    }
}