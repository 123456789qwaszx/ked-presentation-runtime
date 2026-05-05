using System;
using System.Collections;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Char Rig", "@Set Character Rig", Order = -999,
    Sets = new[]
    {
        CommandMenuSets.BuildChar,
    },
    SetOrder = -980)]
public sealed class SetupCharRigCommandSpec : CommandSpecBase
{
    [Header("Role / Slot")]
    [Tooltip("생성할 CharacterRig의 roleKey/slotKey. 예: slot1, me, right")]
    public string roleKey;

    [Header("Rig")]
    [Tooltip("있으면 이 프리팹을 인스턴스해서 Rig를 구성합니다. 없으면 자동 생성합니다.")]
    public GameObject rigPrefab;

    [Tooltip("Rig를 붙일 Slot")]
    public CharRigSlot parentSlot = CharRigSlot.CharacterSlotRight;

    [Tooltip("자동 생성 시 루트 오브젝트 이름.")]
    public string rigRootName = "CharacterRig";

    [Header("Role Prefix")]
    [Tooltip("켜면 roleKey로부터 자동으로 prefix를 생성합니다. 예: roleKey='seina' -> 'seina_'")]
    public bool autoRolePrefixFromRoleKey = true;

    [Tooltip("켜면 최종 prefix를 실제 Rig 이름에 적용합니다.")]
    public bool addRolePrefix = true;

    [Tooltip("Parent Slot에 동일한 이름의 Rig가 이미 있으면 파괴 후 새로 생성합니다.")]
    public bool destroyExistingRigWithSameName = true;

    [Tooltip("필수 노드가 없으면 예외를 던질지.")]
    public bool strict = true;

    public string ResolvedRolePrefix
    {
        get
        {
            if (!addRolePrefix)
                return "";

            if (!autoRolePrefixFromRoleKey)
                return "";

            if (string.IsNullOrEmpty(roleKey))
                return "";

            return roleKey.EndsWith("_", StringComparison.Ordinal)
                ? roleKey
                : $"{roleKey}_";
        }
    }

    public string ResolvedRigName => $"{ResolvedRolePrefix}{rigRootName}";
}

public sealed class SetupCharRigCommand : CommandBase
{
    private readonly CharacterRigAccess _rigAccess;
    private readonly SetupCharRigCommandSpec _spec;

    public override bool WaitForCompletion => true;

    public SetupCharRigCommand(CharacterRigAccess rigAccess, SetupCharRigCommandSpec spec)
    {
        _rigAccess = rigAccess;
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        Apply(scope);
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        Apply(scope);
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        OnSkip(scope);
    }

    private void Apply(CommandRunScope scope)
    {
        string roleKey = SafeTrim(_spec.roleKey);

        if (string.IsNullOrEmpty(roleKey))
            throw new InvalidOperationException("[SetupCharRigCommand] roleKey is empty.");

        if (_spec.destroyExistingRigWithSameName)
            TryDestroyExistingRig();

        CharacterRigRefs rigRefs = _rigAccess.BindAndBuildRefs(_spec);
        scope.Refs[roleKey] = rigRefs;
    }

    private void TryDestroyExistingRig()
    {
        RectTransform parent = _rigAccess.ResolveParentSlot(_spec.parentSlot, _spec.strict);
        if (parent == null)
            return;

        string rigName = _spec.ResolvedRigName;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child.name != rigName)
                continue;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            else
#endif
                UnityEngine.Object.Destroy(child.gameObject);
        }
    }

    private static string SafeTrim(string s)
    {
        return string.IsNullOrEmpty(s) ? "" : s.Trim();
    }
}