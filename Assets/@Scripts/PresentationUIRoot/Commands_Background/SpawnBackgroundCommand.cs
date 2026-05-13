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
    public PresentationTarget parentTarget = PresentationTarget.Stage00BGContent_Root;

    public bool destroyExistingWithSameKey = true;
    public bool setAsLastSibling = true;

    [Header("Response Binding")]
    public PresentationResponseProfile responseProfile = PresentationResponseProfile.Background;
}

public sealed class SpawnBackgroundCommand : CommandBase
{
    private readonly IBGViewPrefabProvider _prefabProvider;
    private readonly IBGRuntimeRegistry _runtimeRegistry;
    private readonly PresentationResponseRig _responseRig;
    private readonly SpawnBackgroundCommandSpec _spec;

    private RectTransform _parent;
    private RectTransformResponseTarget _prefab;
    private string _bgKey;
    private bool _resolveAttempted;

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
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_parent == null || _prefab == null)
            yield break;

        Spawn(scope);
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_parent == null || _prefab == null)
            return;

        Spawn(scope);
    }

    protected override void OnRollbackSeek(CommandRunScope scope) => OnSkip(scope);

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        _bgKey = _spec.bgKey;
        _parent = scope.Presentation.GetRect(_spec.parentTarget);
        _prefabProvider.TryGetBackgroundViewPrefab(_spec.viewPrefabKey, out _prefab);
    }

    private void Spawn(CommandRunScope scope)
    {
        if (_spec.destroyExistingWithSameKey)
            DestroyExistingForKey(scope, _bgKey);

        RectTransformResponseTarget target = Object.Instantiate(_prefab, _parent, false);
        target.gameObject.name = $"BG_{_bgKey}";
        
        if (_spec.setAsLastSibling)
            target.transform.SetAsLastSibling();

        ResetSpawnedRectTransform(target);
        
        PresentationTarget stageRootTarget = ResolveStageRootTarget(_spec.parentTarget);
        RectTransform presentationRoot = scope.Presentation.GetRect(stageRootTarget);

        _responseRig?.RegisterRuntimeBinding(
            _bgKey,
            target,
            _spec.responseProfile,
            presentationRoot);

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
    
    private static PresentationTarget ResolveStageRootTarget(PresentationTarget parentTarget)
{
    switch (parentTarget)
    {
        case PresentationTarget.Stage00_Root:
        case PresentationTarget.Stage00BackgroundSystem_Root:
        case PresentationTarget.Stage00BGShot_Root:
        case PresentationTarget.Stage00BGContent_Root:
        case PresentationTarget.Stage00BGOverlay_Root:
        case PresentationTarget.Stage00CharacterSystem_Root:
        case PresentationTarget.Stage00CharSlot_Root:
        case PresentationTarget.Stage00CharSlotFocus_Root:
        case PresentationTarget.Stage00CharSlotRig_Root:
        case PresentationTarget.Stage00Foreground_Root:
            return PresentationTarget.Stage00_Root;

        case PresentationTarget.Stage01_Root:
        case PresentationTarget.Stage01BackgroundSystem_Root:
        case PresentationTarget.Stage01BGShot_Root:
        case PresentationTarget.Stage01BGContent_Root:
        case PresentationTarget.Stage01BGOverlay_Root:
        case PresentationTarget.Stage01CharacterSystem_Root:
        case PresentationTarget.Stage01CharSlot_Root:
        case PresentationTarget.Stage01CharSlotFocus_Root:
        case PresentationTarget.Stage01CharSlotRig_Root:
        case PresentationTarget.Stage01Foreground_Root:
            return PresentationTarget.Stage01_Root;

        case PresentationTarget.Stage02_Root:
        case PresentationTarget.Stage02BackgroundSystem_Root:
        case PresentationTarget.Stage02BGShot_Root:
        case PresentationTarget.Stage02BGContent_Root:
        case PresentationTarget.Stage02BGOverlay_Root:
        case PresentationTarget.Stage02CharacterSystem_Root:
        case PresentationTarget.Stage02CharSlot_Root:
        case PresentationTarget.Stage02CharSlotFocus_Root:
        case PresentationTarget.Stage02CharSlotRig_Root:
        case PresentationTarget.Stage02Foreground_Root:
            return PresentationTarget.Stage02_Root;
    }

    return PresentationTarget.Stage00_Root;
}
}