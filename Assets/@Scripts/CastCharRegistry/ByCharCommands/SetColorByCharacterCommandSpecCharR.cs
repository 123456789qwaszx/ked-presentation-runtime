using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
[CommandMenuHint(
    "Char Rig",
    "Set Color By Character (Z)",
    Order = 871
)]
public sealed class SetColorByCharacterCommandSpecCharR : CommandSpecBase
{
    [Header("Character Target")]
    public string characterKey;

    [Header("Target")]
    public CharacterRigTarget target = CharacterRigTarget.CharacterPortrait_Image;

    [Header("Color")]
    public Color color = Color.white;

    [Tooltip("체크하면 현재 알파(A)는 그대로 두고 색상(RGB)만 변경합니다.")]
    public bool keepAlpha = true;

    [Header("Validation")]
    public bool strict = true;
}

public sealed class SetColorByCharacterCommandCharR : CommandBase
{
    private readonly SetColorByCharacterCommandSpecCharR _spec;

    private Image _image;
    private bool _resolveAttempted;

    public SetColorByCharacterCommandCharR(SetColorByCharacterCommandSpecCharR spec) => _spec = spec;

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        Apply();
        yield break;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        string characterKey = SafeTrim(_spec.characterKey);
        if (string.IsNullOrEmpty(characterKey))
        {
            if (_spec.strict)
                Debug.LogError("[SetColorByCharacterCommandCharR] characterKey is null or empty.");
            return;
        }

        if (!scope.CastRegistry.TryGetRole(characterKey, out string roleKey) ||
            string.IsNullOrWhiteSpace(roleKey))
        {
            if (_spec.strict)
                Debug.LogWarning(
                    $"[SetColorByCharacterCommandCharR] No cast role found for character='{characterKey}'.");
            return;
        }

        if (!scope.Refs.TryGetCharRigRefs(roleKey, out CharacterRigRefs rig) || rig == null)
        {
            if (_spec.strict)
                Debug.LogWarning(
                    $"[SetColorByCharacterCommandCharR] Rig refs not found. character='{characterKey}', roleKey='{roleKey}'.");
            return;
        }

        _image = rig.GetGraphic(_spec.target) as Image;
        if (_image == null && _spec.strict)
        {
            Debug.LogWarning(
                $"[SetColorByCharacterCommandCharR] Target image not found. character='{characterKey}', roleKey='{roleKey}', target='{_spec.target}'.");
        }
    }
    
    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_image == null)
            return;

        Apply();
    }

    protected override void OnRollbackSeek(CommandRunScope scope) => OnSkip(scope);

    private void Apply()
    {
        if (_image == null)
            return;

        Color color = _spec.color;

        if (_spec.keepAlpha)
            color.a = _image.color.a;

        _image.color = color;
    }

    private static string SafeTrim(string s)
    {
        return string.IsNullOrEmpty(s) ? string.Empty : s.Trim();
    }
}