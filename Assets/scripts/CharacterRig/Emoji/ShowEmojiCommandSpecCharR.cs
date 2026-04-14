using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
[CommandMenuHint("Char Rig", "Show Emoji (Preset)", Order = -960)]
public sealed class ShowEmojiCommandSpecCharR : CommandSpecBase
{
    public CharacterEmojiDatabaseSO database;
    public string emojiKey = "sparkle";

    [Header("Behavior")]
    public bool hideIfKeyEmpty = true;
    public bool wait = false;

    [Header("Override (optional)")]
    public float fadeInOverride = -1f;  // <0이면 preset 사용
    public Ease ease = Ease.OutCubic;

    public bool snapOnSkip = true;
}

public sealed class ShowEmojiCommandCharR : CommandBase, IStepScopedCommand
{
    private readonly ShowEmojiCommandSpecCharR _spec;

    private Sequence _seq;

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public ShowEmojiCommandCharR(ShowEmojiCommandSpecCharR spec) => _spec = spec;

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        Debug.Log("showEmojiCommandCharR");
        if (!scope.Refs.TryGetCharRigRefs(_spec.roleKey, out CharacterRigRefs rig) || rig == null)
            yield break;

        // 빈 키면 hide 처리
        if (_spec.hideIfKeyEmpty && string.IsNullOrWhiteSpace(_spec.emojiKey))
        {
            HideEmojiImmediate(rig);
            yield break;
        }

        if (_spec.database == null || !_spec.database.TryGet(_spec.emojiKey.Trim(), out var preset) || preset == null)
            yield break;

        Debug.Log("showEmojiCommandCharR??");
        // 필수 refs
        RectTransform emojiRoot   = rig.CharacterEmoji_Root;
        RectTransform emojiAnchor = rig.CharacterEmoji_Anchor;
        Image emojiImg            = rig.CharacterEmoji_Image;

        if (emojiRoot == null || emojiAnchor == null || emojiImg == null)
            yield break;

        CanvasGroup cg = GetRootCanvasGroup(emojiRoot, "CharacterEmoji_Root");

        // 항상 켜둠(깜빡임 방지)
        if (!emojiRoot.gameObject.activeSelf) emojiRoot.gameObject.SetActive(true);
        if (!emojiAnchor.gameObject.activeSelf) emojiAnchor.gameObject.SetActive(true);

        // 진행 중 페이드 중단
        cg.DOKill(false);
        _seq?.Kill(false);
        _seq = null;

        // 1) Sprite 세팅
        emojiImg.sprite = preset.sprite;

        // (옵션) preserveAspect 기본 on 추천
        emojiImg.preserveAspect = true;

        // 2) Layout 적용(Anchor에 “한 번에”)
        ApplyLayout(emojiAnchor, preset.layout);

        // 3) Persistent 효과 교체
        // - 기존 지속 효과가 이미 돌아가고 있을 수 있으니 정리
        KillPersistentOn(emojiAnchor);

        // 4) FadeIn
        float fadeIn = (_spec.fadeInOverride >= 0f) ? _spec.fadeInOverride : preset.fadeIn;

        if (fadeIn <= 0f)
        {
            cg.alpha = 1f;
            ApplyPersistent(preset, emojiAnchor);
            yield break;
        }

        _seq = DOTween.Sequence().SetUpdate(true);
        _seq.Append(cg.DOFade(1f, fadeIn).SetEase(_spec.ease));
        _seq.AppendCallback(() =>
        {
            // Fade 끝난 후 지속 효과 시작(원하면 Fade 시작 전에 해도 됨)
            ApplyPersistent(preset, emojiAnchor);
        });

        if (!_spec.wait)
            yield break;
        Debug.Log("showEmojiCommandCharR?@#@#?");

        while (_seq != null && _seq.IsActive() && _seq.IsPlaying())
            yield return null;
    }

    protected override void OnSkip(CommandRunScope scope) => OnCommandCompleted(scope);

    protected override void OnCommandCompleted(CommandRunScope scope)
    {
        if (_spec.snapOnSkip)
            _seq?.Complete(true);
        else
            _seq?.Kill(false);

        _seq = null;
    }

    private static CanvasGroup GetRootCanvasGroup(RectTransform root, string debugName)
    {
        var cg = root.GetComponent<CanvasGroup>();
        if (cg != null) return cg;
        throw new InvalidOperationException($"[ShowEmoji] CanvasGroup missing on Root: {debugName} ({root.name})");
    }

    private static void HideEmojiImmediate(CharacterRigRefs rig)
    {
        if (rig.CharacterEmoji_Root == null) return;
        var cg = rig.CharacterEmoji_Root.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 0f;

        if (rig.CharacterEmoji_Anchor != null)
            KillPersistentOn(rig.CharacterEmoji_Anchor);
    }

    private static void ApplyLayout(RectTransform anchor, CharacterEmojiPresetSO.EmojiLayout layout)
    {
        // anchorMin/Max를 쓰고 싶으면(네가 Anchor로 조정한다고 했으니)
        anchor.anchorMin = layout.anchorMin;
        anchor.anchorMax = layout.anchorMax;

        anchor.anchoredPosition = layout.anchoredPos;

        var s = anchor.localScale;
        s.x = layout.scale.x;
        s.y = layout.scale.y;
        anchor.localScale = s;

        var e = anchor.localEulerAngles;
        e.z = layout.rotationZ;
        anchor.localEulerAngles = e;
    }

    // 지속 효과는 “Anchor”에 건다고 가정 (Sway/Shake/Pulse 등을 anchor 아래로 전파)
    private static void KillPersistentOn(RectTransform anchor)
    {
        if (anchor == null) return;
        anchor.DOKill(false);
    }

    private static void ApplyPersistent(CharacterEmojiPresetSO preset, RectTransform anchor)
    {
        if (preset == null || anchor == null) return;

        var p = preset.effectParams;

        switch (preset.effect)
        {
            case CharacterEmojiPresetSO.EmojiPersistentEffect.None:
                break;

            case CharacterEmojiPresetSO.EmojiPersistentEffect.Sway:
                // 좌우 회전 흔들림
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