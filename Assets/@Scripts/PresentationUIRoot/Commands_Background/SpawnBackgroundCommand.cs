using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using Object = UnityEngine.Object;

[Serializable]
[CommandMenuHint("Presentation Background", "Spawn Background", Order = -900)]
public sealed class SpawnBackgroundCommandSpec : CommandSpecBase
{
    [Header("Identity")]
    public string bgKey = "current";

    [Header("View Prefab")]
    public string viewPrefabKey = "default";

    [Header("Spawn")]
    public bool destroyExistingWithSameKey = true;
    public bool setAsLastSibling = true;

    [Header("Validation")]
    public bool strict = true;
}

public sealed class SpawnBackgroundCommand : CommandBase
{
    private readonly IBGViewPrefabProvider _prefabProvider;
    private readonly IBGRuntimeRegistry _runtimeRegistry;
    private readonly PresentationResponseRig _responseRig;
    private readonly SpawnBackgroundCommandSpec _spec;

    private PresentationViewRefs _presentation;
    private RectTransform _parent;
    private RectTransformResponseTarget _prefab;
    private string _bgKey;
    private bool _resolveAttempted;

    public override bool WaitForCompletion => true;

    public SpawnBackgroundCommand(
        IBGViewPrefabProvider prefabProvider,
        SpawnBackgroundCommandSpec spec,
        IBGRuntimeRegistry runtimeRegistry = null,
        PresentationResponseRig responseRig = null)
    {
        _prefabProvider = prefabProvider;
        _spec = spec;
        _runtimeRegistry = runtimeRegistry;
        _responseRig = responseRig;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_presentation == null || _parent == null || _prefab == null || string.IsNullOrEmpty(_bgKey))
            yield break;

        Spawn(scope);
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_presentation == null || _parent == null || _prefab == null || string.IsNullOrEmpty(_bgKey))
            return;

        Spawn(scope);
    }

    protected override void OnRollbackSeek(CommandRunScope scope) => OnSkip(scope);

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        _bgKey = SafeTrim(_spec.bgKey);
        if (string.IsNullOrEmpty(_bgKey))
        {
            if (_spec.strict)
                Debug.LogWarning("[SpawnBackgroundCommand] bgKey is null or empty.");
            return;
        }

        if (scope == null)
        {
            if (_spec.strict)
                Debug.LogWarning("[SpawnBackgroundCommand] CommandRunScope is null.");
            return;
        }

        if (scope.Presentation == null)
        {
            if (_spec.strict)
                Debug.LogWarning("[SpawnBackgroundCommand] PresentationViewRefs is null.");
            return;
        }

        _presentation = scope.Presentation;

        _parent = _presentation.GetRect(PresentationTarget.BGContent_Root);
        if (_parent == null)
        {
            if (_spec.strict)
                Debug.LogWarning("[SpawnBackgroundCommand] BGContent_Root not found.");
            return;
        }

        if (_prefabProvider == null)
        {
            if (_spec.strict)
                Debug.LogWarning("[SpawnBackgroundCommand] Background prefab provider is null.");
            return;
        }

        string viewPrefabKey = SafeTrim(_spec.viewPrefabKey);
        if (string.IsNullOrEmpty(viewPrefabKey))
        {
            if (_spec.strict)
                Debug.LogWarning("[SpawnBackgroundCommand] viewPrefabKey is null or empty.");
            return;
        }

        if (!_prefabProvider.TryGetBackgroundViewPrefab(viewPrefabKey, out _prefab) || _prefab == null)
        {
            if (_spec.strict)
                Debug.LogWarning(
                    $"[SpawnBackgroundCommand] Background view prefab not found. viewPrefabKey='{viewPrefabKey}'.");
            return;
        }
    }

    private void Spawn(CommandRunScope scope)
    {
        if (_spec.destroyExistingWithSameKey)
            DestroyExistingForKey(scope, _bgKey);

        RectTransformResponseTarget target = Object.Instantiate(_prefab, _parent, false);

        target.name = string.IsNullOrWhiteSpace(_bgKey)
            ? _prefab.name
            : $"BG_{_bgKey}";

        if (_spec.setAsLastSibling)
            target.transform.SetAsLastSibling();

        ResetSpawnedRectTransform(target);

        _responseRig?.RegisterRuntimeBinding(
            _bgKey,
            target,
            PresentationResponseProfile.Background,
            scope.Presentation.GetRect(PresentationTarget.Stage_Root));

        if (scope != null && scope.Refs != null)
            scope.Refs[_bgKey] = target;

        _runtimeRegistry?.RegisterRuntimeBackground(_bgKey, target);
    }

    private void DestroyExistingForKey(CommandRunScope scope, string bgKey)
    {
        _responseRig?.RemoveBinding(bgKey);
        _runtimeRegistry?.DestroyRuntimeBackground(bgKey);

        if (scope != null &&
            scope.Refs != null &&
            scope.Refs.TryGetValue(bgKey, out object obj) &&
            obj is RectTransformResponseTarget existing)
        {
            DestroyExisting(existing);
        }

        scope?.Refs?.Remove(bgKey);
    }

    private static void DestroyExisting(RectTransformResponseTarget existing)
    {
        if (existing == null)
            return;

        RectTransform rect = existing.transform as RectTransform;
        if (rect != null)
            rect.DOKill(true);

        CanvasGroup canvasGroup = existing.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
            canvasGroup.DOKill(true);

        Object.Destroy(existing.gameObject);
    }

    private static void ResetSpawnedRectTransform(RectTransformResponseTarget target)
    {
        if (target == null)
            return;

        RectTransform rect = target.transform as RectTransform;
        if (rect == null)
        {
            target.transform.localPosition = Vector3.zero;
            target.transform.localRotation = Quaternion.identity;
            target.transform.localScale = Vector3.one;
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

    private static string SafeTrim(string s)
    {
        return string.IsNullOrEmpty(s) ? string.Empty : s.Trim();
    }
}