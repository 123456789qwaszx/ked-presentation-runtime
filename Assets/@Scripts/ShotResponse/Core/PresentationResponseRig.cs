using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public sealed class PresentationResponseRig : MonoBehaviour
{
    // 권위 있는 논리 상태. shot 커맨드가 즉시 확정한다 (롤백/스킵과 동일하게 결정론적).
    // BuildTargetState의 from으로 쓰이는 값이 바로 이것이다.
    private PresentationIntentState _logicalState = PresentationIntentState.Default;

    // 실제로 화면에 렌더되는 상태. _logicalState를 향해 부드럽게 따라간다.
    private PresentationIntentState _visualState = PresentationIntentState.Default;

    private readonly List<PresentationResponseBinding> _bindings = new();
    private PresentationCameraRootApplier _cameraRootApplier;

    // rig가 소유하는 단일 shot 드라이버. 커맨드 수명과 무관하게 살아남고,
    // 새 shot 커맨드가 들어오면 현재 visual에서 재타깃된다.
    private Tween _shotDriver;

    public PresentationIntentState CurrentState => _logicalState;
    public PresentationIntentState VisualState => _visualState; // 디버그/검사용
    public bool IsShotDriving => _shotDriver != null && _shotDriver.IsActive();

    public void Initialize(PresentationCameraRootApplier cameraRootApplier)
    {
        _cameraRootApplier = cameraRootApplier;
    }

    // 즉시 스냅: logical/visual 동시 확정. duration<=0, skip, rollback seek에서 사용.
    public void SetShotImmediate(in PresentationIntentState state)
    {
        KillDriver();
        _logicalState = state;
        _visualState = state;
        ApplyState(in _visualState);
    }

    // logical을 즉시 target으로 확정하고, visual은 "현재 렌더값"에서 target으로 ease.
    // → 진행 중이던 연출을 끊지 않고 이어받는다 (점프 없음).
    public void DriveShotTo(in PresentationIntentState target, float duration, Ease ease)
    {
        _logicalState = target;   // 결정론적 확정. 여기서만 끝난다.

        // 이미 화면이 목표에 있거나 시간이 없으면 트윈하지 않는다.
        if (duration <= 0f ||
            PresentationShotIntentMath.ApproximatelyEqual(_visualState, target))
        {
            KillDriver();
            _visualState = target;
            ApplyState(in _visualState);
            return;
        }

        PresentationIntentState start = _visualState; // 현재 렌더값에서 출발
        PresentationIntentState end = target;

        KillDriver();
        _shotDriver = DOTween
            .To(
                () => 0f,
                t =>
                {
                    _visualState = PresentationShotIntentMath.Interpolate(start, end, t);
                    ApplyState(in _visualState);
                },
                1f,
                duration)
            .SetEase(ease)
            .SetUpdate(true)
            .SetTarget(this)
            .OnComplete(() =>
            {
                _visualState = end;
                ApplyState(in _visualState);
                _shotDriver = null;
            });
    }

    public bool RegisterCharacterRigBinding(CommandRunScope scope, string targetKey, PresentationResponseProfile presetProfile, RectTransform stageRoot)
    {
        string resolvedSlotKey = ResponseBindingKeys.CharacterRig(scope, targetKey);
        if (!scope.characterRigs.TryGetRig(resolvedSlotKey, out CharacterRigRefs rigRefs))
            return false;

        string bindingKey = ResponseBindingKeys.CharacterRigFromSlotKey(resolvedSlotKey);
        CharacterRigResponseTarget target = new CharacterRigResponseTarget(rigRefs);

        return RegisterRuntimeBinding(bindingKey, target, presetProfile, stageRoot);
    }

    public bool RegisterBackgroundRigBinding(CommandRunScope scope, string bgKey, PresentationResponseProfile presetProfile, RectTransform stageRoot)
    {
        string resolvedBgKey = ResponseBindingKeys.BackgroundRig(scope, bgKey);
        if (!scope.backgroundRigs.TryGetRig(resolvedBgKey, out BackgroundRigRefs rigRefs))
            return false;

        string bindingKey = ResponseBindingKeys.BackgroundRigFromRigKey(resolvedBgKey);
        BackgroundRigResponseTarget target = new BackgroundRigResponseTarget(rigRefs);

        return RegisterRuntimeBinding(bindingKey, target, presetProfile, stageRoot);
    }

    public bool RegisterRuntimeBinding(string bindingKey, IResponseTarget target, PresentationResponseProfile presetProfile, RectTransform stageRoot)
    {
        PresentationResponseProfile runtimeProfile = BakeRuntimeProfile(target, presetProfile, stageRoot);

        PresentationResponseBinding binding = new PresentationResponseBinding(bindingKey, runtimeProfile, target, stageRoot);

        // 새로 등록된 타깃을 "현재 렌더 상태(visual)"로 즉시 동기화.
        // 드라이브 도중 등록돼도 다음 프레임부터 ApplyState가 함께 끌고 간다.
        ReplaceBinding(bindingKey, binding);
        binding.Apply(in _visualState);

        return true;
    }

    public bool RemoveBinding(string bindingKey)
    {
        bool removed = false;

        for (int i = _bindings.Count - 1; i >= 0; i--)
        {
            PresentationResponseBinding binding = _bindings[i];

            if (binding == null)
                continue;

            if (string.Equals(binding.Key, bindingKey, StringComparison.Ordinal))
            {
                _bindings.RemoveAt(i);
                removed = true;
            }
        }

        return removed;
    }

    public void Clear()
    {
        KillDriver();

        _logicalState = PresentationIntentState.Default;
        _visualState = PresentationIntentState.Default;
        _bindings.Clear();

        _cameraRootApplier?.Apply(in _visualState);
    }

    private void OnDestroy() => KillDriver();

    private void KillDriver()
    {
        if (_shotDriver != null)
        {
            if (_shotDriver.IsActive())
                _shotDriver.Kill(false);

            _shotDriver = null;
        }
    }

    private void ApplyState(in PresentationIntentState state)
    {
        _cameraRootApplier?.Apply(in state);

        for (int i = _bindings.Count - 1; i >= 0; i--)
        {
            PresentationResponseBinding binding = _bindings[i];

            if (binding == null || !binding.IsAlive)
            {
                _bindings.RemoveAt(i);
                continue;
            }

            binding.Apply(in state);
        }
    }

    private void ReplaceBinding(string key, PresentationResponseBinding newBinding)
    {
        for (int i = 0; i < _bindings.Count; i++)
        {
            PresentationResponseBinding binding = _bindings[i];

            if (binding == null)
                continue;

            if (string.Equals(binding.Key, key, StringComparison.Ordinal))
            {
                _bindings[i] = newBinding;
                return;
            }
        }

        _bindings.Add(newBinding);
    }

    private PresentationResponseProfile BakeRuntimeProfile(IResponseTarget target, PresentationResponseProfile presetProfile, RectTransform stageRoot)
    {
        // See "Response Binding Bake Rules" region below.
        #region Response Binding Bake Rules
        // MeasureRect is the stable logical position source for this response target.
        //
        // IMPORTANT:
        // basePositionInRigSpace must be baked from a transform that is affected by
        // stage placement / blocking commands, but NOT affected by Presentation Response
        // output transforms such as FramingTransform or FramingScale.
        //
        // CharacterRig example:
        // - place / anchor / slot positioning should affect CharSlot_Anchor or another
        //   Slot axis node above CharSlot_Scale.
        // - MeasureRect is usually CharSlot_Scale.
        // - PositionRect is usually CharSlot_FramingTransform.
        // - ScaleRect is usually CharSlot_FramingScale.
        //
        // If localPivot stays (0, 0, 0) even after placing the character left/right,
        // check the rig hierarchy and command target order.
        // The most likely cause is that the anchor/place command is applied BELOW
        // MeasureRect, for example on Character_CastTransform, so MeasureRect cannot
        // see the placement offset.
        //
        // Correct direction:
        //   Stage placement / blocking axis
        //   -> MeasureRect
        //   -> Framing response axis
        //   -> Character casting / visual axis
        #endregion
        Vector3 worldPivot = target.MeasureRect.TransformPoint(Vector3.zero);
        Vector3 localPivot = stageRoot.InverseTransformPoint(worldPivot);

        PresentationResponseProfile profile = new PresentationResponseProfile
        {
            maxZoomScaleDelta = presetProfile.maxZoomScaleDelta,
            maxZoomSpreadPixels = presetProfile.maxZoomSpreadPixels,
            panResponse = presetProfile.panResponse
        };

        profile.basePositionInRigSpace = new Vector2(localPivot.x, localPivot.y);
        profile.baseLocalScale = new Vector2(target.ScaleRect.localScale.x, target.ScaleRect.localScale.y);

        return profile;
    }
}