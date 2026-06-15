using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
[CommandMenuHint("Char Rig Emoji", "Set Character Emoji Content", Order = -700)]
public sealed class SetCharacterEmojiCommandSpecCharR : CharacterRigCommandSpecBase
{
    [Header("Emoji Identity")]
    public string emojiKey;

    [Header("Rig Targets")]
    public CharacterRigTarget rootTarget = CharacterRigTarget.CharacterEmojiSlot00_Root;
    public CharacterRigTarget imageTarget = CharacterRigTarget.EmojiSlot00_Image;

    [Header("Visual")]
    public CharacterEmojiVisualPresetSO visualPreset;
    public bool useResolvedVisualPreset = true;
    public bool overrideVisualPreset = false;

    [Header("Reveal Initial State")]
    [Tooltip("배치 직후 머터리얼 _Reveal 값입니다. 일반 표시=1, reveal/pop 합성 준비=0.")]
    [Range(0f, 1f)]
    public float initialReveal = 1f;
}

// Responsibility:
// - emojiKey를 sprite/runtime material로 해석한다.
// - Image에 sprite를 넣고 material initial state를 준비한다.
// - 위치/scale/rotation/alpha/fade/reveal motion은 다른 command가 담당한다.
public sealed class SetCharacterEmojiCommandCharR : CommandBase
{
    private readonly SetCharacterEmojiCommandSpecCharR _spec;
    private readonly CharacterEmojiResolver _resolver;

    private Image _image;
    private CharacterEmojiMaterialRuntime _materialRuntime;

    private Sprite _resolvedSprite;
    private CharacterEmojiVisualPresetSO _resolvedVisualPreset;

    private bool _hasResolvedEmoji;
    private bool _resolveAttempted;
    private bool HasClaimedTarget { get; set; }

    public override bool WaitForCompletion => _spec.wait;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public SetCharacterEmojiCommandCharR(
        SetCharacterEmojiCommandSpecCharR spec,
        CharacterEmojiResolver resolver)
    {
        _spec = spec;
        _resolver = resolver;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!_hasResolvedEmoji)
            yield break;

        ClaimTarget();
        CommitFinalState();
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (!_hasResolvedEmoji)
            return;

        if (!HasClaimedTarget)
            ClaimTarget();

        CommitFinalState();
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        CharacterRigRefs rigRefs = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, _spec.slotKey);
        if (rigRefs == null)
            return;

        _image = rigRefs.GetImage(_spec.imageTarget);
        _materialRuntime = rigRefs.GetEmojiMaterialRuntime(_spec.imageTarget);

        ResolveEmoji();
    }

    private void ResolveEmoji()
    {
        _hasResolvedEmoji = false;
        _resolvedSprite = null;
        _resolvedVisualPreset = null;

        if (_resolver != null &&
            _resolver.TryResolve(
                _spec.emojiKey,
                out Sprite sprite,
                out CharacterEmojiPlacement _,
                out CharacterEmojiVisualPresetSO visualPreset))
        {
            _hasResolvedEmoji = true;
            _resolvedSprite = sprite;
            _resolvedVisualPreset = visualPreset;
            return;
        }

        Debug.LogWarning(
            $"[SetCharacterEmojiCommandCharR] Failed to resolve emoji sprite. " +
            $"emojiKey='{_spec.emojiKey}', targetKey='{_spec.slotKey}'.");
    }

    private void ClaimTarget()
    {
        _materialRuntime?.KillTween(true);
        _image?.DOKill(true);

        HasClaimedTarget = true;
    }

    private void CommitFinalState()
    {
        _image.sprite = _resolvedSprite;
        
        ApplyEmojiMaterialInitialState();

        HasClaimedTarget = false;
    }

    private void ApplyEmojiMaterialInitialState()
    {
        CharacterEmojiVisualPresetSO preset = ResolveVisualPreset();

        if (preset == null || preset.baseMaterial == null || _materialRuntime == null)
            return;

        if (!_materialRuntime.EnsureMaterial(preset.baseMaterial))
            return;

        _materialRuntime.ApplyPresetStatic(preset, _spec.initialReveal);
    }

    private CharacterEmojiVisualPresetSO ResolveVisualPreset()
    {
        if (_spec.overrideVisualPreset && _spec.visualPreset != null)
            return _spec.visualPreset;

        if (_spec.useResolvedVisualPreset && _resolvedVisualPreset != null)
            return _resolvedVisualPreset;

        if (_spec.visualPreset != null)
            return _spec.visualPreset;

        return null;
    }
}