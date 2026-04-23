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

    public bool trackRunLifetime = true;
    public bool strict = true;
}

public sealed class SpawnDialogueBoxCommand : CommandBase
{
    private readonly IDialogueBoxViewPrefabProvider _prefabProvider;
    private readonly SpawnDialogueBoxCommandSpec _spec;

    public override bool WaitForCompletion => true;

    public SpawnDialogueBoxCommand(
        IDialogueBoxViewPrefabProvider prefabProvider,
        SpawnDialogueBoxCommandSpec spec)
    {
        _prefabProvider = prefabProvider;
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

        if (parent == null)
        {
            if (_spec.strict)
                throw new InvalidOperationException($"[SpawnDialogueBoxCommand] Parent target not found. target={_spec.parentTarget}");
            return;
        }

        if (_prefabProvider == null)
        {
            if (_spec.strict)
                throw new InvalidOperationException("[SpawnDialogueBoxCommand] DialogueBox prefab provider is null.");
            return;
        }

        if (!_prefabProvider.TryGetDialogueBoxViewPrefab(_spec.viewPrefabKey, out GameObject prefab) || prefab == null)
        {
            if (_spec.strict)
                throw new InvalidOperationException(
                    $"[SpawnDialogueBoxCommand] DialogueBox view prefab not found. viewPrefabKey={_spec.viewPrefabKey}");
            return;
        }

        string refKey = PresentationDialogueBoxRegistryExt.MakeDialogueBoxRefKey(_spec.dialogueKey);

        if (_spec.destroyExistingWithSameKey && scope.Refs.TryGetDialogueBoxView(_spec.dialogueKey, out PresentationDialogueBoxView existing))
            DestroyExisting(existing);

        GameObject go = Object.Instantiate(prefab, parent, false);
        go.name = string.IsNullOrWhiteSpace(_spec.dialogueKey) ? prefab.name : $"Dialogue_{_spec.dialogueKey}";

        if (_spec.setAsLastSibling)
            go.transform.SetAsLastSibling();

        PresentationDialogueBoxView view = go.GetComponent<PresentationDialogueBoxView>();
        if (view == null)
            view = go.AddComponent<PresentationDialogueBoxView>();

        view.EnsureBound(_spec.strict);

        if (view.CanvasGroup != null)
        {
            view.CanvasGroup.alpha = Mathf.Clamp01(_spec.initialAlpha);
            view.CanvasGroup.interactable = false;
            view.CanvasGroup.blocksRaycasts = false;
        }

        scope.Refs[refKey] = view;

        if (_spec.trackRunLifetime)
        {
            scope.TrackRun(
                cancel: () =>
                {
                    if (view == null)
                        return;

#if UNITY_EDITOR
                    if (!Application.isPlaying)
                        Object.DestroyImmediate(view.gameObject);
                    else
#endif
                        Object.Destroy(view.gameObject);
                });
        }
    }

    private void DestroyExisting(PresentationDialogueBoxView existing)
    {
        if (existing == null)
            return;

        RectTransform rect = existing.Root != null ? existing.Root : existing.transform as RectTransform;
        if (rect != null)
            rect.DOKill(true);

        if (existing.CanvasGroup != null)
            existing.CanvasGroup.DOKill(true);

#if UNITY_EDITOR
        if (!Application.isPlaying)
            Object.DestroyImmediate(existing.gameObject);
        else
#endif
            Object.Destroy(existing.gameObject);
    }
}