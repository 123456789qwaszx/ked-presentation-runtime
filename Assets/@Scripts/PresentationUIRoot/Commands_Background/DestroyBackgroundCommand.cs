using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using Object = UnityEngine.Object;

[Serializable]
[CommandMenuHint("Presentation Background", "Destroy Background", Order = -880)]
public sealed class DestroyBackgroundCommandSpec : CommandSpecBase
{
    [Header("Identity")]
    [Tooltip("파괴할 대상 배경 view를 찾을 bgKey")]
    public string bgKey = "current";

    [Header("Options")]
    [Tooltip("체크하면 기존 트윈을 끝내고 committed state에서 파괴합니다.")]
    public bool killTween = true;

    [Tooltip("scope.Refs에서 해당 bgKey 엔트리를 제거합니다.")]
    public bool removeRefEntry = true;

    [Tooltip("필수 계약이 없으면 예외를 던질지")]
    public bool strict = true;
}

public sealed class DestroyBackgroundCommand : CommandBase
{
    private readonly DestroyBackgroundCommandSpec _spec;

    private PresentationBackgroundView _view;
    private bool _resolveAttempted;

    public override bool WaitForCompletion => true;

    public DestroyBackgroundCommand(DestroyBackgroundCommandSpec spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        DestroyView(scope);
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        DestroyView(scope);
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        DestroyView(scope);
    }

    private void DestroyView(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_view == null)
            return;

        RectTransform rect = _view.Root != null ? _view.Root : _view.transform as RectTransform;

        if (rect != null && _spec.killTween)
            rect.DOKill(true);

        if (_view.CanvasGroup != null)
            _view.CanvasGroup.DOKill(_spec.killTween);

        if (_spec.removeRefEntry && scope != null && scope.Refs != null)
            scope.Refs.Remove(_spec.bgKey);

#if UNITY_EDITOR
        if (!Application.isPlaying)
            Object.DestroyImmediate(_view.gameObject);
        else
#endif
            Object.Destroy(_view.gameObject);

        _view = null;
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        if (scope == null || scope.Refs == null)
        {
            if (_spec.strict)
                throw new InvalidOperationException($"[DestroyBackgroundCommand] Refs is null. bgKey={_spec.bgKey}");
            return;
        }

        if (!scope.Refs.TryGetValue(_spec.bgKey, out object obj) ||
            obj is not PresentationBackgroundView view)
        {
            if (_spec.strict)
                throw new InvalidOperationException($"[DestroyBackgroundCommand] Background view not found. bgKey={_spec.bgKey}");
            return;
        }

        _view = view;

        if (_view == null && _spec.strict)
            throw new InvalidOperationException($"[DestroyBackgroundCommand] Background view is null. bgKey={_spec.bgKey}");
    }
}