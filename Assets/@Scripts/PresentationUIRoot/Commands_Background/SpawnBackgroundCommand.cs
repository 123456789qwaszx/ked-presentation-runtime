using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using Object = UnityEngine.Object;

[Serializable]
[CommandMenuHint("Presentation Background", "Spawn Background", Order = -900)]
public sealed class SpawnBackgroundCommandSpec : CommandSpecBase
{
    [Header("Identity")] public string bgKey = "current";

    [Header("View Prefab")] public string viewPrefabKey = "default";

    [Header("Spawn")] public bool destroyExistingWithSameKey = true;
    public bool setAsLastSibling = true;

    [Range(0f, 1f)] public float initialAlpha = 0f;

    public bool strict = true;
}

public sealed class SpawnBackgroundCommand : CommandBase
{
    private readonly IBGViewPrefabProvider _prefabProvider;
    private readonly IBGRuntimeRegistry _runtimeRegistry;
    private readonly PresentationResponseRig _responseRig;
    private readonly SpawnBackgroundCommandSpec _spec;

    public override bool WaitForCompletion => true;

    public SpawnBackgroundCommand(
        IBGViewPrefabProvider prefabProvider,
        SpawnBackgroundCommandSpec spec,
        IBGRuntimeRegistry runtimeRegistry,
        PresentationResponseRig responseRig)
    {
        _prefabProvider = prefabProvider;
        _spec = spec;
        _runtimeRegistry = runtimeRegistry;
        _responseRig = responseRig;
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

    protected override void OnRollbackSeek(CommandRunScope scope) => OnSkip(scope);

    private void Spawn(CommandRunScope scope)
    {
        RectTransform parent = scope.Presentation.GetRect(PresentationTarget.BGContent_Root);
        
        if (!_prefabProvider.TryGetBackgroundViewPrefab(_spec.viewPrefabKey, out GameObject prefab) || prefab == null)
        {
            Debug.Log($"[SpawnBackgroundCommand] Background view prefab not found. viewPrefabKey={_spec.viewPrefabKey}");
            return;
        }

        string bgKey = _spec.bgKey;

        if (_spec.destroyExistingWithSameKey)
        {
            _responseRig.RemoveBinding(bgKey);
            _runtimeRegistry.DestroyRuntimeBackground(bgKey);

            if (scope.Refs.TryGetValue(bgKey, out object obj) && obj is PresentationBackgroundView existing)
            {
                DestroyExisting(existing);
            }

            scope.Refs.Remove(bgKey);
        }

        GameObject go = Object.Instantiate(prefab, parent, false);

        if (_spec.setAsLastSibling)
            go.transform.SetAsLastSibling();

        ResetSpawnedRectTransform(go);

        PresentationBackgroundView view = go.GetComponent<PresentationBackgroundView>();
        if (view == null)
        {
            view = go.AddComponent<PresentationBackgroundView>();
            Debug.Log("[SpawnBackgroundCommand] Background view not found. AutoAdd PresentationBackgroundView");
        }

        view.EnsureBound(_spec.strict);

        if (view.CanvasGroup != null)
        {
            view.CanvasGroup.alpha = Mathf.Clamp01(_spec.initialAlpha);
            view.CanvasGroup.interactable = false;
            view.CanvasGroup.blocksRaycasts = false;
        }

        RectTransformResponseTarget responseTarget = view.GetComponent<RectTransformResponseTarget>();

        if (responseTarget == null)
        {
            responseTarget = view.gameObject.AddComponent<RectTransformResponseTarget>();
            Debug.Log("[SpawnBackgroundCommand] BackgroundResponseTarget not found. AutoAdd RectTransformResponseTarget");
        }

        _responseRig?.RegisterRuntimeBinding(
            bgKey,
            responseTarget,
            PresentationResponseProfile.Background,
            scope.Presentation);

        if (scope.Refs != null)
            scope.Refs[bgKey] = view;

        _runtimeRegistry?.RegisterRuntimeBackground(bgKey, view);
    }

    private void DestroyExisting(PresentationBackgroundView existing)
    {
        if (existing == null)
            return;

        RectTransform rect = existing.Root != null
            ? existing.Root
            : existing.transform as RectTransform;

        if (rect != null)
            rect.DOKill(true);

        if (existing.CanvasGroup != null)
            existing.CanvasGroup.DOKill(true);

        Object.Destroy(existing.gameObject);
    }

    private static void ResetSpawnedRectTransform(GameObject go)
    {
        if (go == null)
            return;

        RectTransform rect = go.transform as RectTransform;
        if (rect == null)
        {
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            return;
        }

        rect.localPosition = Vector3.zero;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }
}