using System;
using System.Collections;
using System.Collections.Generic;
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

    public static bool TryGetBackgroundView(this Dictionary<string, object> refs, string bgKey, out PresentationBackgroundView view)
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
    [Tooltip("런타임에서 배경 view를 저장/참조할 키")]
    public string bgKey = "current";

    [Header("View Prefab")]
    [Tooltip("BGHost에서 조회할 배경 view 프리팹 키")]
    public string viewPrefabKey = "default";

    [Header("Spawn")]
    [Tooltip("동일 bgKey가 이미 있으면 파괴 후 새로 생성")]
    public bool destroyExistingWithSameKey = true;

    [Tooltip("생성 후 마지막 sibling으로 올림")]
    public bool setAsLastSibling = true;

    [Range(0f, 1f)]
    [Tooltip("생성 직후 CanvasGroup 초기 alpha")]
    public float initialAlpha = 0f;

    [Tooltip("true면 scope.RunLifetime에 등록해서 런 종료 시 정리")]
    public bool trackRunLifetime = true;

    [Tooltip("필수 계약이 없으면 예외를 던질지")]
    public bool strict = true;
}

public sealed class SpawnBackgroundCommand : CommandBase
{
    private readonly IBGViewPrefabProvider _prefabProvider;
    private readonly SpawnBackgroundCommandSpec _spec;

    public override bool WaitForCompletion => true;

    public SpawnBackgroundCommand(
        IBGViewPrefabProvider prefabProvider,
        SpawnBackgroundCommandSpec spec)
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

        if (_spec.destroyExistingWithSameKey && scope.Refs.TryGetBackgroundView(_spec.bgKey, out PresentationBackgroundView existing))
            DestroyExisting(existing);

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

        scope.Refs[refKey] = view;

        // if (_spec.trackRunLifetime)
        // {
        //     scope.TrackRun(
        //         cancel: () =>
        //         {
        //             if (view == null)
        //                 return;
        //             Object.Destroy(view.gameObject);
        //             Debug.Log("Destroying view");
        //         });
        // }
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