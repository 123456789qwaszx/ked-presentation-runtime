using System;
using System.Collections;
using UnityEngine;

[Serializable]
[CommandMenuHint(
    "Char Rig", "@Cast Character", Order = -998,
    Sets = new[]
    {
        CommandMenuSets.BuildChar,
    },
    SetOrder = -979)]
public sealed class CastCharacterCommandSpec : CommandSpecBase
{
    [Header("Slot / Role")]
    [Tooltip("캐릭터를 바인딩할 slotKey / roleKey.")]
    public string slotKey;

    [Header("Character Identity")]
    [Tooltip("이 slotKey에 바인딩할 캐릭터 키.")]
    public string characterKey;
}
public sealed class CastCharacterCommand : CommandBase
{
    private readonly CastCharacterCommandSpec _spec;

    public CastCharacterCommand(CastCharacterCommandSpec spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        ApplyBinding(scope);
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        ApplyBinding(scope);
    }

    private void ApplyBinding(CommandRunScope scope)
    {
        string slotKey = _spec.slotKey;
        string characterKey = _spec.characterKey;

        scope.CastRegistry.CastCharRig(slotKey, characterKey);

        ApplyFocusPreviewMarkerRoleKey(scope, slotKey, characterKey);
    }

    private static void ApplyFocusPreviewMarkerRoleKey(
        CommandRunScope scope,
        string slotKey,
        string characterKey)
    {
        CharacterRigRefs rigRefs = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, slotKey);
        RectTransform extensionsRoot = rigRefs.GetRect(CharacterRigTarget.Character_ExtensionsRoot);
        CharacterFocusPreviewMarker[] markers = extensionsRoot.GetComponentsInChildren<CharacterFocusPreviewMarker>();

        if (markers == null || markers.Length == 0)
            return;

        for (int i = 0; i < markers.Length; i++)
        {
            if (markers[i] == null)
                continue;

            markers[i].SetRoleKey(characterKey);
        }
    }
}