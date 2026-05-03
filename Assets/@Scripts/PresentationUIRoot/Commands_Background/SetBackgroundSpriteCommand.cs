using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
[CommandMenuHint("Presentation Background", "Set Background Sprite", Order = -890)]
public sealed class SetBackgroundSpriteCommandSpec : CommandSpecBase
{
    [Header("Identity")]
    [Tooltip("대상 배경 view를 찾을 bgKey")]
    public string bgKey = "current";

    [Header("Sprite")]
    [Tooltip("직접 지정할 스프라이트")]
    public Sprite sprite;

    [Header("Options")]
    [Tooltip("체크하면 preserveAspect를 설정합니다.")]
    public bool setPreserveAspect = true;

    public bool preserveAspect = true;

    [Tooltip("체크하면 SetNativeSize()를 호출합니다.")]
    public bool setNativeSize = false;

    [Tooltip("필수 계약이 없으면 예외를 던질지")]
    public bool strict = true;
}

public sealed class SetBackgroundSpriteCommand : CommandBase
{
    private readonly SetBackgroundSpriteCommandSpec _spec;

    private PresentationBackgroundView _view;
    private bool _resolveAttempted;

    public override bool WaitForCompletion => true;

    public SetBackgroundSpriteCommand(SetBackgroundSpriteCommandSpec spec)
    {
        _spec = spec;
    }

    protected override IEnumerator ExecuteInner(CommandRunScope scope)
    {
        Apply(scope);
        yield break;
    }

    protected override void OnSkip(CommandRunScope scope)
    {
        Apply(scope);
    }

    protected override void OnRollbackSeek(CommandRunScope scope)
    {
        Apply(scope);
    }

    private void Apply(CommandRunScope scope)
    {
        if (!_resolveAttempted)
            ResolveRefs(scope);

        if (_view == null || _view.Image == null)
            return;

        if (_spec.sprite == null)
        {
            if (_spec.strict)
                throw new InvalidOperationException($"[SetBackgroundSpriteCommand] sprite is null. bgKey={_spec.bgKey}");
            return;
        }

        _view.Image.sprite = _spec.sprite;

        if (_spec.setPreserveAspect)
            _view.Image.preserveAspect = _spec.preserveAspect;

        if (_spec.setNativeSize)
            _view.Image.SetNativeSize();
    }

    private void ResolveRefs(CommandRunScope scope)
    {
        _resolveAttempted = true;

        if (scope == null || scope.Refs == null)
        {
            if (_spec.strict)
                throw new InvalidOperationException($"[SetBackgroundSpriteCommand] Refs is null. bgKey={_spec.bgKey}");
            return;
        }

        if (!scope.Refs.TryGetValue(_spec.bgKey, out object obj) ||
            obj is not PresentationBackgroundView view)
        {
            if (_spec.strict)
                throw new InvalidOperationException($"[SetBackgroundSpriteCommand] Background view not found. bgKey={_spec.bgKey}");
            return;
        }

        _view = view;
        _view.EnsureBound(_spec.strict);

        if (_view.Image == null && _spec.strict)
            throw new InvalidOperationException($"[SetBackgroundSpriteCommand] Background Image missing. bgKey={_spec.bgKey}");
    }
}