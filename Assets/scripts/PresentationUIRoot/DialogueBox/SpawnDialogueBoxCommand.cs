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

    public bool clearTextOnSpawn = true;
    public bool trackRunLifetime = true;
    public bool strict = true;
}

public sealed class SpawnDialogueBoxCommand : CommandBase
{
    private readonly IDialogueBoxHost _host;
    private readonly SpawnDialogueBoxCommandSpec _spec;

    public override bool WaitForCompletion => true;

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
        Spawn(scope);
    }

    private void Spawn(CommandRunScope scope)
    {
        RectTransform parent = scope.Presentation.GetRect(_spec.parentTarget);
        GameObject prefab = GetPrefab();

        string refKey = PresentationDialogueBoxRegistryExt.MakeDialogueBoxRefKey(_spec.dialogueKey);

        if (_spec.destroyExistingWithSameKey && _host.TryGetView(_spec.dialogueKey, out PresentationDialogueBoxView existing))
            DestroyExisting(scope, refKey, existing);

        PresentationDialogueBoxView view = CreateView(prefab, parent);

        if (_spec.clearTextOnSpawn)
            view.ClearText();

        ApplyInitialState(view);
        Register(scope, refKey, view);

        if (_spec.trackRunLifetime)
            TrackLifetime(scope, refKey, view);
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

    private PresentationDialogueBoxView CreateView(GameObject prefab, RectTransform parent)
    {
        GameObject go = Object.Instantiate(prefab, parent, false);

        go.name = string.IsNullOrWhiteSpace(_spec.dialogueKey)
            ? prefab.name
            : $"Dialogue_{_spec.dialogueKey}";

        if (_spec.setAsLastSibling)
            go.transform.SetAsLastSibling();

        PresentationDialogueBoxView view = go.GetComponent<PresentationDialogueBoxView>();

        if (view == null)
        {
            throw new InvalidOperationException(
                $"[SpawnDialogueBoxCommand] Prefab must have PresentationDialogueBoxView. prefab={prefab.name}");
        }

        if (_spec.strict)
            view.Validate();

        return view;
    }

    private void ApplyInitialState(PresentationDialogueBoxView view)
    {
        view.CanvasGroup.alpha = Mathf.Clamp01(_spec.initialAlpha);
        view.CanvasGroup.interactable = false;
        view.CanvasGroup.blocksRaycasts = false;
    }

    private void Register(
        CommandRunScope scope,
        string refKey,
        PresentationDialogueBoxView view)
    {
        scope.Refs[refKey] = view;
        _host.Register(_spec.dialogueKey, view);
    }

    private void TrackLifetime(
        CommandRunScope scope,
        string refKey,
        PresentationDialogueBoxView view)
    {
        scope.TrackRun(
            cancel: () =>
            {
                Unregister(scope, refKey, view);
                Object.Destroy(view.gameObject);
            });
    }

    private void DestroyExisting(
        CommandRunScope scope,
        string refKey,
        PresentationDialogueBoxView existing)
    {
        Unregister(scope, refKey, existing);

        existing.Root.DOKill(true);
        existing.CanvasGroup.DOKill(true);

        Object.Destroy(existing.gameObject);
    }

    private void Unregister(
        CommandRunScope scope,
        string refKey,
        PresentationDialogueBoxView view)
    {
        _host.Unregister(_spec.dialogueKey, view);

        if (scope.Refs.TryGetValue(refKey, out object current) && ReferenceEquals(current, view))
            scope.Refs.Remove(refKey);
    }
}