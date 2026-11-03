using System.Collections;
using UnityEngine;

// BGM policy: single active track; crossfade on replace; snap while skipping.
// BGM은 장면 전역 의도다 — 장면을 넘어 이어 간다. 시크(롤백·로드) 중에는 의도만 쌓고(Hold),
// 시크가 끝나면 실제와 맞춘다(Release). 같은 곡이 흐르고 있으면 처음부터 다시 틀지 않는다.
public sealed class BgmPlayer
{
    private readonly AudioSource _sourceA;
    private readonly AudioSource _sourceB;
    private readonly MonoBehaviour _host;

    private AudioSource _current;
    private AudioSource _next;

    private Coroutine _fadeRoutine;

    private bool _held;

    public AudioClip CurrentClip => _current?.clip;
    public bool IsPlaying => _current != null && _current.isPlaying;

    // 지금 흘러야 할 곡. 없으면 null.
    public AudioClip Intent { get; private set; }

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
    public void Play(AudioClip clip, float fadeDuration, bool isSkipping)
    {
        if (clip == null)
        {
            return;
        }

        Intent = clip;

        if (_held)
            return;

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
        Intent = null;

        if (_held)
            return;

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

    // 시크 시작 — 의도를 출발점(장면 머리의 곡)으로 되돌리고, 실제 소리는 시크가 끝날 때까지 그대로 둔다.
    public void Hold(AudioClip intent)
    {
        _held = true;
        Intent = intent;

        StopFade();
    }

    // 시크 끝 — 의도와 실제를 맞춘다. 의도한 곡이 이미 흐르고 있으면(페이드 도중이어도) 그 자리에서 이어 간다.
    public void Release()
    {
        if (!_held)
            return;

        _held = false;
        StopFade();

        if (Intent == null)
        {
            Stop(0f, isSkipping: true);
            return;
        }

        if (_next.clip == Intent && _next.isPlaying)
            (_current, _next) = (_next, _current);

        if (_current.clip == Intent && _current.isPlaying)
        {
            _current.volume = 1f;

            _next.Stop();
            _next.clip   = null;
            _next.volume = 0f;
            return;
        }

        SnapTo(Intent);
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