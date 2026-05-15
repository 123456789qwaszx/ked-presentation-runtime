using System;
using System.Collections;
using UnityEngine;

public enum CharRigCamFocusMoveMode
{
    Set,
    Add
}

[Serializable]
[CommandMenuHint(
    "Char Rig",
    "Set Cam Focus",
    Order = -870)]
public sealed class SetCharRigCamFocusCommandSpec : CharacterRigCommandSpecBase
{
    [Header("Search")]
    [Tooltip("camFocus를 찾기 시작할 CharacterRig 내부 기준 루트.")]
    public CharacterRigTarget searchRoot = CharacterRigTarget.CharacterPortrait_Scale;

    [Tooltip("역할 prefix 뒤에 붙을 focus 오브젝트 이름. 예: camFocus => Mercurio_camFocus")]
    public string focusObjectName = "camFocus";

    [Tooltip("체크하면 targetKey prefix를 붙인 이름을 먼저 찾습니다. 예: Mercurio_camFocus")]
    public bool useRolePrefix = true;

    [Tooltip("prefix가 붙은 이름을 못 찾으면 focusObjectName 그대로도 다시 찾습니다.")]
    public bool fallbackToRawName = true;

    [Header("Move")]
    [Tooltip("Set이면 anchoredPosition으로 설정, Add면 현재 anchoredPosition에 더합니다.")]
    public CharRigCamFocusMoveMode mode = CharRigCamFocusMoveMode.Set;

    [Tooltip("camFocus RectTransform의 anchoredPosition 또는 추가 offset.")]
    public Vector2 position = Vector2.zero;

    [Header("Options")]
    [Tooltip("focusObjectName을 찾지 못했을 때 예외를 던질지.")]
    public bool strict = true;
}

public sealed class SetCharRigCamFocusCommand : CommandBase
{
    private readonly SetCharRigCamFocusCommandSpec _spec;

    private RectTransform _focusRect;
    private bool _resolveAttempted;

    public override bool WaitForCompletion => true;
    protected override SkipPolicy SkipPolicy => SkipPolicy.CompleteImmediately;

    public SetCharRigCamFocusCommand(SetCharRigCamFocusCommandSpec spec)
    {
        _spec = spec;
    }

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

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        OnSkip(scope);
    }

    private void Apply()
    {
        if (_focusRect == null)
            return;

        if (_spec.mode == CharRigCamFocusMoveMode.Add)
            _focusRect.anchoredPosition += _spec.position;
        else
            _focusRect.anchoredPosition = _spec.position;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        string roleKey = SafeTrim(_spec.targetKey);

        if (string.IsNullOrEmpty(roleKey))
        {
            if (_spec.strict)
                throw new InvalidOperationException("[SetCharRigCamFocusCommand] targetKey is null or empty.");

            return;
        }

        CharacterRigRefs rigRefs =
            CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, roleKey);

        if (rigRefs == null)
        {
            if (_spec.strict)
            {
                throw new InvalidOperationException(
                    $"[SetCharRigCamFocusCommand] CharacterRigRefs not found. targetKey='{roleKey}'.");
            }

            return;
        }

        RectTransform root = rigRefs.GetRect(_spec.searchRoot);
        if (root == null)
        {
            if (_spec.strict)
            {
                throw new InvalidOperationException(
                    $"[SetCharRigCamFocusCommand] Search root not found. " +
                    $"targetKey='{roleKey}', searchRoot='{_spec.searchRoot}'.");
            }

            return;
        }

        _focusRect = FindFocusRect(root, roleKey);

        if (_focusRect == null && _spec.strict)
        {
            string expectedName = BuildPrefixedName(roleKey, _spec.focusObjectName);

            throw new InvalidOperationException(
                $"[SetCharRigCamFocusCommand] cam focus rect not found. " +
                $"targetKey='{roleKey}', searchRoot='{_spec.searchRoot}', " +
                $"expected='{expectedName}', fallback='{_spec.focusObjectName}'.");
        }
    }

    private RectTransform FindFocusRect(Transform root, string roleKey)
    {
        if (root == null)
            return null;

        string rawName = SafeTrim(_spec.focusObjectName);
        if (string.IsNullOrEmpty(rawName))
            return null;

        if (_spec.useRolePrefix)
        {
            string prefixedName = BuildPrefixedName(roleKey, rawName);
            RectTransform found = FindChildRectByName(root, prefixedName);
            if (found != null)
                return found;

            string lowerPrefixedName = BuildPrefixedName(roleKey.ToLowerInvariant(), rawName);
            found = FindChildRectByName(root, lowerPrefixedName);
            if (found != null)
                return found;
        }

        if (_spec.fallbackToRawName)
            return FindChildRectByName(root, rawName);

        return null;
    }

    private static string BuildPrefixedName(string roleKey, string objectName)
    {
        string role = SafeTrim(roleKey);
        string name = SafeTrim(objectName);

        if (string.IsNullOrEmpty(role))
            return name;

        if (string.IsNullOrEmpty(name))
            return role;

        string prefix = role.EndsWith("_", StringComparison.Ordinal)
            ? role
            : role + "_";

        if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return name;

        return prefix + name;
    }

    private static RectTransform FindChildRectByName(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
            return null;

        string targetName = childName.Trim();

        if (string.Equals(root.name, targetName, StringComparison.OrdinalIgnoreCase))
            return root as RectTransform;

        for (int i = 0; i < root.childCount; i++)
        {
            RectTransform found = FindChildRectByName(root.GetChild(i), targetName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static string SafeTrim(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}