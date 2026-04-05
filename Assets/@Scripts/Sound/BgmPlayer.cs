// BgmPlayer.cs
// 책임: BGM 단일 채널 재생, 크로스페이드, skip 시 즉시 스냅

using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public sealed class BgmPlayer
{
    private readonly AudioSource _sourceA;
    private readonly AudioSource _sourceB;
    private readonly MonoBehaviour _host;

    private AudioSource _current;  // 지금 재생 중인 소스
    private AudioSource _next;     // 페이드인 중인 소스

    private Coroutine _fadeRoutine;

    public AudioClip CurrentClip => _current?.clip;
    public bool IsPlaying => _current != null && _current.isPlaying;

    public BgmPlayer(AudioSource sourceA, AudioSource sourceB, MonoBehaviour host)
    {
        _sourceA = sourceA;
        _sourceB = sourceB;
        _host    = host;

        _sourceA.loop   = true;
        _sourceB.loop   = true;
        _sourceA.volume = 0f;
        _sourceB.volume = 0f;

        _current = _sourceA;
        _next    = _sourceB;
    }

    // ── 재생 ─────────────────────────────────────────────────
    public void Play(AudioClip clip, float fadeDuration, bool isSkipping)
    {
        if (clip == null)
        {
            Debug.LogWarning("[BgmPlayer] clip is null.");
            return;
        }

        // 같은 클립이면 무시
        if (_current.clip == clip && _current.isPlaying)
            return;

        StopFade();

        if (isSkipping || fadeDuration <= 0f)
        {
            SnapTo(clip);
            return;
        }

        _fadeRoutine = _host.StartCoroutine(CrossFade(clip, fadeDuration));
    }

    public void Stop(float fadeDuration, bool isSkipping)
    {
        StopFade();

        if (isSkipping || fadeDuration <= 0f)
        {
            _current.Stop();
            _current.clip   = null;
            _current.volume = 0f;
            return;
        }

        _fadeRoutine = _host.StartCoroutine(FadeOut(fadeDuration));
    }

    // ── 즉시 스냅 ─────────────────────────────────────────────
    private void SnapTo(AudioClip clip)
    {
        _current.Stop();
        _next.Stop();

        _current.volume = 1f;
        _current.clip   = clip;
        _current.Play();

        _next.volume = 0f;
        _next.clip   = null;
    }

    // ── 크로스페이드 ──────────────────────────────────────────
    private IEnumerator CrossFade(AudioClip clip, float duration)
    {
        // next 준비
        _next.clip   = clip;
        _next.volume = 0f;
        _next.Play();

        float elapsed = 0f;
        float startVolume = _current.volume;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            _current.volume = Mathf.Lerp(startVolume, 0f, t);
            _next.volume    = Mathf.Lerp(0f, 1f, t);

            yield return null;
        }

        // 페이드 완료 후 정리
        _current.Stop();
        _current.clip   = null;
        _current.volume = 0f;

        // current ↔ next 스왑
        (_current, _next) = (_next, _current);

        _fadeRoutine = null;
    }

    private IEnumerator FadeOut(float duration)
    {
        float elapsed     = 0f;
        float startVolume = _current.volume;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            _current.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        _current.Stop();
        _current.clip   = null;
        _current.volume = 0f;

        _fadeRoutine = null;
    }

    private void StopFade()
    {
        if (_fadeRoutine == null) return;
        _host.StopCoroutine(_fadeRoutine);
        _fadeRoutine = null;
    }
}