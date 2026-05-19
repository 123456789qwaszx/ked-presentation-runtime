using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
[CommandMenuHint("Char Rig", "Show Emoji (Preset)", Order = -960)]
public sealed class ShowEmojiCommandSpecCharR : CharacterRigCommandSpecBase
{
    public CharacterEmojiDatabaseSO database;
    public string emojiKey = "sparkle";

    [Header("Behavior")]
    public bool hideIfKeyEmpty = true;

    [Header("Override (optional)")]
    public float fadeInOverride = -1f;  // <0이면 preset 사용
    public Ease ease = Ease.OutCubic;

    public bool snapOnSkip = true;
}

public sealed class ShowEmojiCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly ShowEmojiCommandSpecCharR _spec;

    private RectTransform _emojiRoot;
    private RectTransform _emojiAnchor;
    private Image _emojiImage;
    private CanvasGroup _emojiCanvasGroup;

    private CharacterEmojiPresetSO _preset;
    private Sequence _seq;
    private bool _resolveAttempted;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public ShowEmojiCommandCharR(ShowEmojiCommandSpecCharR spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_spec.hideIfKeyEmpty && string.IsNullOrWhiteSpace(_spec.emojiKey))
        {
            HideEmojiImmediate();
            yield break;
        }

        ResolvePreset();

        Apply();

        if (!_spec.wait)
            yield break;

        while (_seq != null && _seq.IsActive() && _seq.IsPlaying())
            yield return null;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_spec.hideIfKeyEmpty && string.IsNullOrWhiteSpace(_spec.emojiKey))
        {
            HideEmojiImmediate();
            return;
        }

        ResolvePreset();

        if (_spec.snapOnSkip)
        {
            ApplyImmediate();
            return;
        }

        OnCommandCompleted(scope);
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        OnSkip(scope);
    }

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (_spec.snapOnSkip)
            _seq?.Complete(true);
        else
            _seq?.Kill(false);

        _seq = null;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        CharacterRigRefs rig =
            CharacterRigTargetResolver.ResolveCharRigFromTargetKey(
                scope,
                _spec.targetKey);

        _emojiRoot = rig.CharacterEmojiSlot00_Root;
        _emojiAnchor = rig.CharacterEmojiSlot00_CastTransform;
        _emojiImage = rig.EmojiSlot00_Image;

        if (_emojiRoot == null || _emojiAnchor == null || _emojiImage == null)
        {
            throw new InvalidOperationException(
                $"[ShowEmojiCommandCharR] Emoji refs are missing. targetKey='{_spec.targetKey}'.");
        }

        _emojiCanvasGroup = GetRootCanvasGroup(_emojiRoot, "CharacterEmoji_Root");
    }

    private void ResolvePreset()
    {
        if (_spec.database == null)
        {
            throw new InvalidOperationException(
                $"[ShowEmojiCommandCharR] Database is null. targetKey='{_spec.targetKey}', emojiKey='{_spec.emojiKey}'.");
        }

        string emojiKey = _spec.emojiKey.Trim();

        if (!_spec.database.TryGet(emojiKey, out _preset) || _preset == null)
        {
            throw new InvalidOperationException(
                $"[ShowEmojiCommandCharR] Emoji preset not found. targetKey='{_spec.targetKey}', emojiKey='{emojiKey}'.");
        }
    }

    private void Apply()
    {
        KillCurrentTween();

        EnsureRootsVisible();

        _emojiImage.sprite = _preset.sprite;
        _emojiImage.preserveAspect = true;

        ApplyLayout(_emojiAnchor, _preset.layout);
        KillPersistentOn(_emojiAnchor);

        float fadeIn = _spec.fadeInOverride >= 0f
            ? _spec.fadeInOverride
            : _preset.fadeIn;

        if (fadeIn <= 0f)
        {
            ApplyImmediate();
            return;
        }

        _emojiCanvasGroup.alpha = 0f;

        _seq = DOTween.Sequence()
            .SetUpdate(true)
            .Append(_emojiCanvasGroup
                .DOFade(1f, fadeIn)
                .SetEase(_spec.ease))
            .AppendCallback(() =>
            {
                ApplyPersistent(_preset, _emojiAnchor);
            });
    }

    private void ApplyImmediate()
    {
        KillCurrentTween();

        EnsureRootsVisible();

        _emojiImage.sprite = _preset.sprite;
        _emojiImage.preserveAspect = true;

        ApplyLayout(_emojiAnchor, _preset.layout);

        _emojiCanvasGroup.alpha = 1f;

        KillPersistentOn(_emojiAnchor);
        ApplyPersistent(_preset, _emojiAnchor);
    }

    private void HideEmojiImmediate()
    {
        KillCurrentTween();

        _emojiCanvasGroup.alpha = 0f;
        KillPersistentOn(_emojiAnchor);
    }

    private void EnsureRootsVisible()
    {
        if (!_emojiRoot.gameObject.activeSelf)
            _emojiRoot.gameObject.SetActive(true);

        if (!_emojiAnchor.gameObject.activeSelf)
            _emojiAnchor.gameObject.SetActive(true);
    }

    private void KillCurrentTween()
    {
        _emojiCanvasGroup.DOKill(false);
        _seq?.Kill(false);
        _seq = null;
    }

    private static CanvasGroup GetRootCanvasGroup(RectTransform root, string debugName)
    {
        CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
            return canvasGroup;

        throw new InvalidOperationException(
            $"[ShowEmojiCommandCharR] CanvasGroup missing on Root: {debugName} ({root.name})");
    }

    private static void ApplyLayout(RectTransform anchor, CharacterEmojiPresetSO.EmojiLayout layout)
    {
        anchor.anchorMin = layout.anchorMin;
        anchor.anchorMax = layout.anchorMax;
        anchor.anchoredPosition = layout.anchoredPos;

        Vector3 scale = anchor.localScale;
        scale.x = layout.scale.x;
        scale.y = layout.scale.y;
        anchor.localScale = scale;

        Vector3 euler = anchor.localEulerAngles;
        euler.z = layout.rotationZ;
        anchor.localEulerAngles = euler;
    }

    private static void KillPersistentOn(RectTransform anchor)
    {
        anchor.DOKill(false);
    }

    private static void ApplyPersistent(CharacterEmojiPresetSO preset, RectTransform anchor)
    {
        var p = preset.effectParams;

        switch (preset.effect)
        {
            case CharacterEmojiPresetSO.EmojiPersistentEffect.None:
                break;

            case CharacterEmojiPresetSO.EmojiPersistentEffect.Sway:
                anchor.DOLocalRotate(
                        new Vector3(0f, 0f, preset.layout.rotationZ + 8f * p.amp),
                        0.6f / Mathf.Max(0.0001f, p.freq)
                    )
                    .SetEase(Ease.InOutSine)
                    .SetLoops(p.duration > 0f ? Mathf.RoundToInt(p.duration * p.freq) : -1, LoopType.Yoyo)
                    .SetUpdate(true);
                break;

            case CharacterEmojiPresetSO.EmojiPersistentEffect.Bob:
                anchor.DOLocalMoveY(
                        anchor.localPosition.y + 12f * p.amp,
                        0.5f / Mathf.Max(0.0001f, p.freq)
                    )
                    .SetEase(Ease.InOutSine)
                    .SetLoops(p.duration > 0f ? Mathf.RoundToInt(p.duration * p.freq) : -1, LoopType.Yoyo)
                    .SetUpdate(true);
                break;

            case CharacterEmojiPresetSO.EmojiPersistentEffect.Shake:
                anchor.DOShakeRotation(
                        0.6f / Mathf.Max(0.0001f, p.freq),
                        strength: new Vector3(0f, 0f, 6f * p.amp),
                        vibrato: 6,
                        randomness: 60f
                    )
                    .SetLoops(p.duration > 0f ? Mathf.RoundToInt(p.duration * p.freq) : -1, LoopType.Restart)
                    .SetUpdate(true);
                break;

            case CharacterEmojiPresetSO.EmojiPersistentEffect.Pulse:
                anchor.DOPunchScale(
                        new Vector3(0.08f * p.amp, 0.08f * p.amp, 0f),
                        0.5f / Mathf.Max(0.0001f, p.freq),
                        vibrato: 6,
                        elasticity: 0.7f
                    )
                    .SetLoops(p.duration > 0f ? Mathf.RoundToInt(p.duration * p.freq) : -1, LoopType.Restart)
                    .SetUpdate(true);
                break;
        }
    }
}