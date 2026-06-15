
using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint("Char Rig Emoji", "Emoji Sway", Order = -692)]
public sealed class EmojiSwayCommandSpecCharR : CharacterRigCommandSpecBase
{
    public string emojiKey;
    public CharacterRigTarget target = CharacterRigTarget.EmojiSlot00_SwayPivot;
    public float strength = 10f;
    public float duration = 1f;
    public int cycles = 2;
    public float damping = 2.2f;
    public float speed = 1f;
    [Range(0f, 1f)] public float finalOvershoot = 0.22f;
    public float anticipation = 0f;
    public bool startPositive = true;
}

public sealed class EmojiSwayCommandCharR : CharacterEmojiCommandBase
{
    private readonly EmojiSwayCommandSpecCharR _spec;
    private readonly CharacterEmojiResolver _resolver;

    private RectTransform _rect;
    private float _baseRotationZ;
    private Tween _tween;
    private bool _resolveAttempted;
    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;

    public EmojiSwayCommandCharR(
        EmojiSwayCommandSpecCharR spec,
        CharacterEmojiResolver resolver)
    {
        _spec = spec;
        _resolver = resolver;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        CharacterEmojiMirrorContext context = ResolveEmojiMirrorContext(
            scope,
            _resolver,
            _spec.slotKey,
            _spec.emojiKey);

        ClaimTarget();

        if (_spec.duration <= 0f || Mathf.Approximately(_spec.strength, 0f))
        {
            CommitFinalState();
            yield break;
        }

        float amplitude = Mathf.Abs(_spec.strength);
        float directionSign = context.MirrorStartPositive(_spec.startPositive) ? 1f : -1f;
        float duration = Mathf.Max(0.0001f, _spec.duration);
        int cycles = Mathf.Max(1, _spec.cycles);
        float damping = Mathf.Max(0f, _spec.damping);
        float speed = Mathf.Max(0.05f, _spec.speed);

        _tween = DOTween
            .To(
                () => 0f,
                t =>
                {
                    float u = Mathf.Clamp01(t / duration);
                    float envelope = Mathf.Pow(1f - u, damping * 0.25f);
                    float wave = Mathf.Sin(2f * Mathf.PI * cycles * speed * u);
                    float angle = _baseRotationZ + directionSign * amplitude * envelope * wave;
                    SetLocalEulerZ(_rect, angle);
                },
                duration,
                duration)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .SetTarget(_rect)
            .OnComplete(CommitFinalState);

        if (_spec.wait)
            yield return _tween.WaitForCompletion();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!HasClaimedTarget)
            ClaimTarget();

        CommitFinalState();
    }

    protected override void OnStepLifetimeFinished(CommandRunScope scope)
    {
        if (!HasClaimedTarget)
            return;

        _tween?.Kill(false);
        CommitFinalState();
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;
        CharacterRigRefs rigRefs = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.slotKey);
        _rect = rigRefs.GetRect(_spec.target);
    }

    private void ClaimTarget()
    {
        _rect.DOKill(true);
        _baseRotationZ = NormalizeAngle(_rect.localEulerAngles.z);
        HasClaimedTarget = true;
    }

    private void CommitFinalState()
    {
        if (_rect != null)
            SetLocalEulerZ(_rect, _baseRotationZ);

        HasClaimedTarget = false;
        _tween = null;
    }

    private static void SetLocalEulerZ(RectTransform rect, float z)
    {
        Vector3 euler = rect.localEulerAngles;
        euler.z = z;
        rect.localEulerAngles = euler;
    }

    private static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        if (angle < -180f) angle += 360f;
        return angle;
    }
}
