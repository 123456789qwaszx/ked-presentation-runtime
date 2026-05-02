using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class PresentationBackgroundView : MonoBehaviour
{
    public RectTransform Root { get; private set; }
    public CanvasGroup CanvasGroup { get; private set; }
    public Image Image { get; private set; }

    public void EnsureBound(bool strict = true)
    {
        Root = transform as RectTransform;
        if (Root == null)
        {
            if (strict)
                throw new InvalidOperationException($"[PresentationBackgroundView] Root must be RectTransform. go={name}");
            return;
        }

        if (!TryGetComponent(out CanvasGroup canvasGroup))
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        CanvasGroup = canvasGroup;

        Image = GetComponentInChildren<Image>(true);
        if (Image == null && strict)
            throw new InvalidOperationException($"[PresentationBackgroundView] Missing Image under '{name}'.");
    }
}

public static class PresentationBackgroundRegistryExt
{
    public static string MakeBackgroundRefKey(string bgKey)
    {
        bgKey = SafeTrim(bgKey);
        return $"bg:{bgKey}";
    }

    public static bool TryGetBackgroundView(this System.Collections.Generic.Dictionary<string, object> refs, string bgKey, out PresentationBackgroundView view)
    {
        string key = MakeBackgroundRefKey(bgKey);

        if (refs != null && refs.TryGetValue(key, out object obj) && obj is PresentationBackgroundView typed)
        {
            view = typed;
            return true;
        }

        view = null;
        return false;
    }

    private static string SafeTrim(string s)
    {
        return string.IsNullOrEmpty(s) ? string.Empty : s.Trim();
    }
}

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

    [Range(0f, 1f)]
    public float initialAlpha = 0f;

    public bool trackRunLifetime = true;
    public bool strict = true;
}

public sealed class SpawnBackgroundCommand : CommandBase
{
    private readonly IBGViewPrefabProvider _prefabProvider;
    private readonly IBGRuntimeRegistry _runtimeRegistry;
    private readonly SpawnBackgroundCommandSpec _spec;

    public override bool WaitForCompletion => true;

    public SpawnBackgroundCommand(
        IBGViewPrefabProvider prefabProvider,
        SpawnBackgroundCommandSpec spec,
        IBGRuntimeRegistry runtimeRegistry = null)
    {
        _prefabProvider = prefabProvider;
        _spec = spec;
        _runtimeRegistry = runtimeRegistry;
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
        RectTransform parent = scope.Presentation.GetRect(PresentationTarget.BGContent_Root);
        if (parent == null)
        {
            if (_spec.strict)
                throw new InvalidOperationException("[SpawnBackgroundCommand] BGContent_Root not found.");
            return;
        }

        if (_prefabProvider == null)
        {
            if (_spec.strict)
                throw new InvalidOperationException("[SpawnBackgroundCommand] Background prefab provider is null.");
            return;
        }

        if (!_prefabProvider.TryGetBackgroundViewPrefab(_spec.viewPrefabKey, out GameObject prefab) || prefab == null)
        {
            if (_spec.strict)
                throw new InvalidOperationException(
                    $"[SpawnBackgroundCommand] Background view prefab not found. viewPrefabKey={_spec.viewPrefabKey}");
            return;
        }

        string refKey = PresentationBackgroundRegistryExt.MakeBackgroundRefKey(_spec.bgKey);
        PresentationResponseRig rig = scope.ResponseRig;

        if (_spec.destroyExistingWithSameKey)
        {
            rig?.RemoveBinding(refKey);
            _runtimeRegistry?.DestroyRuntimeBackground(_spec.bgKey);

            if (scope.Refs.TryGetBackgroundView(_spec.bgKey, out PresentationBackgroundView existing))
                DestroyExisting(existing);
        }

        GameObject go = Object.Instantiate(prefab, parent, false);
        go.name = string.IsNullOrWhiteSpace(_spec.bgKey) ? prefab.name : $"BG_{_spec.bgKey}";

        if (_spec.setAsLastSibling)
            go.transform.SetAsLastSibling();

        ResetSpawnedRectTransform(go);

        PresentationBackgroundView view = go.GetComponent<PresentationBackgroundView>();
        if (view == null)
            view = go.AddComponent<PresentationBackgroundView>();

        view.EnsureBound(_spec.strict);

        if (view.CanvasGroup != null)
        {
            view.CanvasGroup.alpha = Mathf.Clamp01(_spec.initialAlpha);
            view.CanvasGroup.interactable = false;
            view.CanvasGroup.blocksRaycasts = false;
        }

        RectTransformResponseTarget responseTarget = view.GetComponent<RectTransformResponseTarget>();

        if (responseTarget == null)
            responseTarget = view.gameObject.AddComponent<RectTransformResponseTarget>();

        rig?.RegisterRuntimeBinding(
            refKey,
            responseTarget,
            PresentationResponseProfile.Background,
            scope.Presentation);

        scope.Refs[refKey] = view;
        _runtimeRegistry?.RegisterRuntimeBackground(_spec.bgKey, view);
    }

    private void DestroyExisting(PresentationBackgroundView existing)
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