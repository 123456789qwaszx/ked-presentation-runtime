using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
[CommandMenuHint(
    "Char Rig Emoji",
    "Animate Character Emoji Frames",
    Order = -696)]
public sealed class AnimateCharacterEmojiFramesCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Target")]
    public CharacterRigTarget rootTarget = CharacterRigTarget.CharacterEmojiSlot00_Root;
    public CharacterRigTarget imageTarget = CharacterRigTarget.EmojiSlot00_Image;

    [Header("Frame Keys")]
    public List<string> frameKeys = new();

    [Header("Timing")]
    public float frameDuration = 0.28f;

    [Tooltip("마지막 프레임에서 조금 더 머무르는 시간.")]
    public float lastFrameHold = 0.55f;

    [Tooltip("처음 프레임으로 돌아가기 전 쉬는 시간.")]
    public float loopRest = 0.15f;

    [Header("Loop")]
    public bool loopUntilStepEnd = true;

    [Tooltip("false면 step cleanup 때 root를 숨김.")]
    public bool keepVisibleOnCleanup = false;

    [Header("Image")]
    public bool preserveAspect = true;
    public bool setNativeSize = false;
}

public sealed class AnimateCharacterEmojiFramesCommandCharR : CharacterEmojiCommandBase
{
    private readonly AnimateCharacterEmojiFramesCommandSpecCharR _spec;
    private readonly CharacterEmojiResolver _resolver;

    private RectTransform _root;
    private CanvasGroup _rootCanvasGroup;
    private Image _image;

    private readonly List<Sprite> _frames = new();
    private CharacterEmojiMirrorContext _mirrorContext;

    private bool _resolveAttempted;
    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;

    public AnimateCharacterEmojiFramesCommandCharR(
        AnimateCharacterEmojiFramesCommandSpecCharR spec,
        CharacterEmojiResolver resolver)
    {
        _spec = spec;
        _resolver = resolver;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!TryResolveFrames())
            yield break;

        _mirrorContext = ResolveEmojiMirrorContext(
            scope,
            _resolver,
            _spec.slotKey,
            ResolveMirrorKey());

        ClaimTarget();

        if (_spec.loopUntilStepEnd)
        {
            while (true)
            {
                yield return PlayOneCycle();
            }
        }

        yield return PlayOneCycle();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!HasClaimedTarget)
        {
            _mirrorContext = ResolveEmojiMirrorContext(
                scope,
                _resolver,
                _spec.slotKey,
                ResolveMirrorKey());

            ClaimTarget();
        }

        Cleanup();
    }

    public override void RegisterStepLifetime(
        CommandRunScope scope,
        MonoBehaviour host,
        IEnumerator routine)
    {
        scope.TrackStep(
            cancel: () =>
            {
                if (routine != null)
                    host.StopCoroutine(routine);

                Cleanup();
            },
            finish: () =>
            {
                if (routine != null)
                    host.StopCoroutine(routine);

                OnStepLifetimeFinished(scope);
            });
    }

    protected override void OnStepLifetimeFinished(CommandRunScope scope)
    {
        if (!HasClaimedTarget)
            return;

        Cleanup();
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        CharacterRigRefs rigRefs =
            CharacterRigTargetResolver.ResolveCharRigFromTargetKey(
                scope,
                _spec.slotKey);

        _root = rigRefs.GetRect(_spec.rootTarget);
        _image = rigRefs.GetImage(_spec.imageTarget);

        _rootCanvasGroup = _root.GetComponent<CanvasGroup>();

        if (_rootCanvasGroup == null)
            _rootCanvasGroup = _root.gameObject.AddComponent<CanvasGroup>();
    }

    private bool TryResolveFrames()
    {
        _frames.Clear();

        if (_spec.frameKeys == null || _spec.frameKeys.Count == 0)
        {
            Debug.LogWarning(
                "[AnimateCharacterEmojiFramesCommandCharR] frameKeys is empty.");
            return false;
        }

        for (int i = 0; i < _spec.frameKeys.Count; i++)
        {
            string frameKey = _spec.frameKeys[i];

            if (_resolver.TryResolveSprite(frameKey, out Sprite sprite) && sprite != null)
            {
                _frames.Add(sprite);
                continue;
            }

            Debug.LogWarning(
                $"[AnimateCharacterEmojiFramesCommandCharR] Failed to resolve frame sprite. " +
                $"frameKey='{frameKey}', slotKey='{_spec.slotKey}'.");
        }

        return _frames.Count > 0;
    }

    private void ClaimTarget()
    {
        _rootCanvasGroup.alpha = 1f;

        _image.preserveAspect = _spec.preserveAspect;

        if (_frames.Count > 0)
            ApplyFrame(0);

        ApplySpriteMirror(_image, _mirrorContext);

        HasClaimedTarget = true;
    }

    private IEnumerator PlayOneCycle()
    {
        for (int i = 0; i < _frames.Count; i++)
        {
            ApplyFrame(i);

            float duration =
                i == _frames.Count - 1
                    ? _spec.frameDuration + _spec.lastFrameHold
                    : _spec.frameDuration;

            yield return WaitUnscaled(duration);
        }

        if (_spec.loopRest > 0f)
            yield return WaitUnscaled(_spec.loopRest);
    }

    private void ApplyFrame(int index)
    {
        if (index < 0 || index >= _frames.Count)
            return;

        _image.sprite = _frames[index];

        if (_spec.setNativeSize)
            _image.SetNativeSize();

        ApplySpriteMirror(_image, _mirrorContext);
    }

    private string ResolveMirrorKey()
    {
        if (_spec.frameKeys != null && _spec.frameKeys.Count > 0)
            return _spec.frameKeys[0];

        return string.Empty;
    }

    private IEnumerator WaitUnscaled(float duration)
    {
        if (duration <= 0f)
            yield break;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void Cleanup()
    {
        if (!_spec.keepVisibleOnCleanup && _rootCanvasGroup != null)
            _rootCanvasGroup.alpha = 0f;

        HasClaimedTarget = false;
    }
}