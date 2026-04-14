using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Char Rig",
    "#ApplyTrackOffset By Character (default = ResetToZero)",
    Order = -889,
    Sets = new[]
    {
        CommandMenuSets.ResetChar,
    },
    SetOrder = -939)]
public sealed class ApplyTrackOffsetByCharacterCommandSpecCharR : CommandSpecBase
{
    [Header("Character Target")]
    public string characterKey;

    [Header("Target")]
    [Tooltip("offset를 실제로 적용할 대상.")]
    public CharacterRigTarget target = CharacterRigTarget.Character_Track;

    [Header("Offset")]
    [Tooltip("현재 anchoredPosition 기준으로 더해질 오프셋(픽셀 단위).")]
    public Vector2 offset = Vector2.zero;

    [Header("Reset Target Before Apply")]
    [Tooltip("체크하면 target의 위치를 먼저 (0,0)으로 맞춘 뒤 offset을 적용합니다. 상위 Anchor의 위치는 유지됩니다.")]
    public bool applyFromZero = true;

    [Header("Track Layer Reset")]
    [Tooltip("체크하면 Character_Track / Move / X / Y 를 전부 (0,0)으로 초기화합니다.")]
    public bool resetAllTrackLayers = true;

    [Tooltip("Char_Track 을 (0,0)으로 초기화.")]
    public bool resetCharTrack = false;

    [Tooltip("Char_Track_Move 를 (0,0)으로 초기화.")]
    public bool resetCharTrackMove = false;

    [Tooltip("Char_Track_X 를 (0,0)으로 초기화.")]
    public bool resetCharTrackX = false;

    [Tooltip("Char_Track_Y 를 (0,0)으로 초기화.")]
    public bool resetCharTrackY = false;

    [Header("Validation")]
    public bool strict = true;
}

public sealed class ApplyTrackOffsetByCharacterCommandCharR : CommandBase
{
    private readonly ApplyTrackOffsetByCharacterCommandSpecCharR _spec;

    private CharacterRigRefs _rigRefs;
    private RectTransform _rect;
    private bool _resolveAttempted;

    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public ApplyTrackOffsetByCharacterCommandCharR(ApplyTrackOffsetByCharacterCommandSpecCharR spec) => _spec = spec;

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        Apply();
        yield break;
    }
    
    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        Apply();
    }

    protected override void OnRollbackSeek(CommandRunScope scope) => OnSkip(scope);

    private void Apply()
    {
        if (_rigRefs == null || _rect == null)
            return;

        if (_spec.resetAllTrackLayers || _spec.resetCharTrack)
            ResetRect(_rigRefs.Character_Track);

        if (_spec.resetAllTrackLayers || _spec.resetCharTrackMove)
            ResetRect(_rigRefs.Character_Track_Move);

        if (_spec.resetAllTrackLayers || _spec.resetCharTrackX)
            ResetRect(_rigRefs.Character_Track_X);

        if (_spec.resetAllTrackLayers || _spec.resetCharTrackY)
            ResetRect(_rigRefs.Character_Track_Y);

        _rect.DOKill(true); // Finish previous motion so this command starts from a committed state.

        if (_spec.applyFromZero)
            _rect.anchoredPosition = Vector2.zero;

        _rect.anchoredPosition += _spec.offset;
    }

    private static void ResetRect(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.DOKill(true); // Finish previous motion so this command starts from a committed state.
        rect.anchoredPosition = Vector2.zero;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        string characterKey = SafeTrim(_spec.characterKey);
        if (string.IsNullOrEmpty(characterKey))
        {
            if (_spec.strict)
                Debug.LogError("[ApplyTrackOffsetByCharacterCommandCharR] characterKey is null or empty.");
            return;
        }

        if (!scope.CastRegistry.TryGetRole(characterKey, out string roleKey) ||
            string.IsNullOrWhiteSpace(roleKey))
        {
            if (_spec.strict)
                Debug.LogWarning(
                    $"[ApplyTrackOffsetByCharacterCommandCharR] No cast role found for character='{characterKey}'.");
            return;
        }

        if (!scope.Refs.TryGetCharRigRefs(roleKey, out CharacterRigRefs rigRefs) || rigRefs == null)
        {
            if (_spec.strict)
                Debug.LogWarning(
                    $"[ApplyTrackOffsetByCharacterCommandCharR] Rig refs not found. character='{characterKey}', roleKey='{roleKey}'.");
            return;
        }

        _rigRefs = rigRefs;
        _rect = rigRefs.GetRect(_spec.target);

        if (_rect == null && _spec.strict)
        {
            Debug.LogWarning(
                $"[ApplyTrackOffsetByCharacterCommandCharR] Target rect not found. character='{characterKey}', roleKey='{roleKey}', target='{_spec.target}'.");
        }
    }

    private static string SafeTrim(string s)
    {
        return string.IsNullOrEmpty(s) ? string.Empty : s.Trim();
    }
}