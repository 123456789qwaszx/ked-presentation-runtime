using System.Collections;
using UnityEngine;

// BGM policy: single active track; crossfade on replace; snap while skipping.
public sealed class BgmPlayer
{
    private readonly AudioSource _sourceA;
    private readonly AudioSource _sourceB;
    private readonly MonoBehaviour _host;

    private AudioSource _current;
    private AudioSource _next;

    private Coroutine _fadeRoutine;

    public AudioClip CurrentClip => _current?.clip;
    public bool IsPlaying => _current != null && _current.isPlaying;

    // 현재(또는 페이드가 끝나면) 재생 중일 BGM의 문자열 키. U12-전체 / U15 상태 스냅샷의 전제.
    // "예약된 최종값" 의미론이다: Play를 받아들인 순간 목표 키가 되고, Stop이면 null이다.
    // directClip으로만 재생돼 키가 없으면 빈 문자열일 수 있다 — U15가 이 경우를 경고로 다룬다.
    public string CurrentClipKey { get; private set; }

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

    // Replace policy.
    // If another BGM is already playing, transition to the new clip.
    // While skipping, apply the final state immediately without fade.
    public void Play(AudioClip clip, string clipKey, float fadeDuration, bool isSkipping)
    {
        if (clip == null)
        {
            //Debug.LogWarning("[BgmPlayer] clip is null.");
            return;
        }

        CurrentClipKey = clipKey;

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
        CurrentClipKey = null;

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

    private IEnumerator CrossFade(AudioClip clip, float duration)
    {
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

        _current.Stop();
        _current.clip   = null;
        _current.volume = 0f;

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