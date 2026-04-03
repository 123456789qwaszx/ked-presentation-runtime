using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public static class TransitionOverlay
{
    public static ScreenBlackoutUITop GetOrCreate()
    {
        UIManager.Instance.ShowTop<ScreenBlackoutUITop>();
        ScreenBlackoutUITop existing = UIManager.Instance.GetUI<ScreenBlackoutUITop>();
        return existing;
    }
}

public class ScreenBlackoutUITop : UITop<ScreenBlackoutUITop.Refs>, IManagedUI
{
    public enum Refs
    {
        ScreenBlackoutBlocker_Root,
        ScreenBlackoutBlocker_Image,
    }

    [SerializeField] private AnimationCurve _ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private CanvasGroup _canvasGroup;
    public CanvasGroup CanvasGroup => _canvasGroup;

    protected override void Initialize()
    {
        _canvasGroup = View.CanvasGroup(Refs.ScreenBlackoutBlocker_Root);
        if (_canvasGroup)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }
    }

    public Image BlockerImage => View.Image(Refs.ScreenBlackoutBlocker_Image);

    public void SetInstant(float alpha, bool? blockOverride = null)
    {
        if (!_canvasGroup) _canvasGroup = View.CanvasGroup(Refs.ScreenBlackoutBlocker_Root);
        if (!_canvasGroup) return;

        alpha = Mathf.Clamp01(alpha);
        _canvasGroup.alpha = alpha;

        bool block = blockOverride ?? (alpha > 0.0001f);
        _canvasGroup.blocksRaycasts = block;
        _canvasGroup.interactable = false;
    }

    public IEnumerator FadeTo(float targetAlpha, float duration, bool? blockOverride = null, AnimationCurve easeOverride = null)
    {
        if (!_canvasGroup) _canvasGroup = View.CanvasGroup(Refs.ScreenBlackoutBlocker_Root);
        if (!_canvasGroup) yield break;

        targetAlpha = Mathf.Clamp01(targetAlpha);

        if (duration <= 0.0001f)
        {
            SetInstant(targetAlpha, blockOverride);
            yield break;
        }

        // 전환 중엔 무조건 입력 봉쇄(안전)
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.interactable = false;

        float start = _canvasGroup.alpha;
        float t = 0f;

        var curve = easeOverride != null ? easeOverride : _ease;

        while (t < duration)
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;

            float u = Mathf.Clamp01(t / duration);
            float eased = curve != null ? Mathf.Clamp01(curve.Evaluate(u)) : u;

            _canvasGroup.alpha = Mathf.Lerp(start, targetAlpha, eased);
            yield return null;
        }

        _canvasGroup.alpha = targetAlpha;

        bool block = blockOverride ?? (targetAlpha > 0.0001f);
        _canvasGroup.blocksRaycasts = block;
        _canvasGroup.interactable = false;
    }
}
