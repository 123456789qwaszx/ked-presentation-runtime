using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using Object = UnityEngine.Object;

[Serializable]
[CommandMenuHint("Presentation Dialogue", "Spawn Dialogue Box", Order = -700)]
public sealed class SpawnDialogueBoxCommandSpec : CommandSpecBase
{
    [Header("Identity")]
    [Tooltip("런타임에서 dialogue box view를 저장/참조할 키")]
    public string dialogueKey = "main";

    [Header("View Prefab")]
    [Tooltip("DialogueBoxHost에서 조회할 dialogue view 프리팹 키")]
    public string viewPrefabKey = "default";

    [Header("Parent")]
    [Tooltip("기본은 DialogueBox_Root. 필요하면 NarrationBox_Root 등으로 변경 가능.")]
    public PresentationTarget parentTarget = PresentationTarget.DialogueBox_Root;

    [Header("Spawn")]
    public bool destroyExistingWithSameKey = true;
    public bool setAsLastSibling = true;

    [Range(0f, 1f)]
    public float initialAlpha = 1f;

    public bool strict = true;
}

public sealed class SpawnDialogueBoxCommand : CommandBase
{
    private readonly IDialogueBoxHost _host;
    private readonly SpawnDialogueBoxCommandSpec _spec;

    public override bool WaitForCompletion => true;
    protected override SkipPolicy SkipPolicy => SkipPolicy.Ignore;

    public SpawnDialogueBoxCommand(
        IDialogueBoxHost host,
        SpawnDialogueBoxCommandSpec spec)
    {
        _host = host;
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        Spawn(scope);
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        Spawn(scope);
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        if (scope.IsRollbackSeeking)
            return;
        
        Spawn(scope);
    }

    private void Spawn(CommandRunScope scope)
    {
        RectTransform parent = scope.Presentation.GetRect(_spec.parentTarget);
        GameObject prefab = GetPrefab();

        string refKey = PresentationDialogueBoxRegistryExt.MakeDialogueBoxRefKey(_spec.dialogueKey);

        if (_spec.destroyExistingWithSameKey && _host.TryGetView(_spec.dialogueKey, out IPresentationDialogueBoxView existing))
            DestroyExisting(existing);

        IPresentationDialogueBoxView view = CreateView(prefab, parent);

        view.ClearText();
        
        view.CanvasGroup.alpha = Mathf.Clamp01(_spec.initialAlpha);
        view.CanvasGroup.interactable = false;
        view.CanvasGroup.blocksRaycasts = false;
        
        Register(scope, refKey, view);
    }

    private GameObject GetPrefab()
    {
        _host.TryGetDialogueBoxViewPrefab(_spec.viewPrefabKey, out GameObject prefab);

        if (prefab == null && _spec.strict)
        {
            throw new InvalidOperationException(
                $"[SpawnDialogueBoxCommand] DialogueBox prefab not found. viewPrefabKey={_spec.viewPrefabKey}");
        }

        return prefab;
    }

    private IPresentationDialogueBoxView CreateView(GameObject prefab, RectTransform parent)
    {
        GameObject go = Object.Instantiate(prefab, parent, false);
        
        go.transform.localPosition = Vector3.zero;

        go.name = string.IsNullOrWhiteSpace(_spec.dialogueKey)
            ? prefab.name
            : $"Dialogue_{_spec.dialogueKey}";

        if (_spec.setAsLastSibling)
            go.transform.SetAsLastSibling();

        IPresentationDialogueBoxView view = FindDialogueBoxView(go);

        if (_spec.strict)
            view.Validate();

        return view;
    }

    private static IPresentationDialogueBoxView FindDialogueBoxView(GameObject go)
    {
        MonoBehaviour[] behaviours = go.GetComponentsInChildren<MonoBehaviour>(true);

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IPresentationDialogueBoxView view)
                return view;
        }

        throw new InvalidOperationException(
            $"[SpawnDialogueBoxCommand] Prefab must have IPresentationDialogueBoxView. prefab={go.name}");
    }

    private void Register(
        CommandRunScope scope,
        string refKey,
        IPresentationDialogueBoxView view)
    {
        scope.Refs[refKey] = view;
        _host.Register(_spec.dialogueKey, view);
    }

    private void DestroyExisting(IPresentationDialogueBoxView existing)
    {
        _host.Unregister(_spec.dialogueKey, existing);

        existing.Root.DOKill(true);
        existing.CanvasGroup.DOKill(true);

        if (existing is MonoBehaviour behaviour)
            Object.Destroy(behaviour.gameObject);
    }
}