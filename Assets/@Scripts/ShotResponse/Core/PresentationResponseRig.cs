using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PresentationResponseRig
{
    private sealed class RuntimeBinding
    {
        public string key;
        public IResponseTarget target;
        public PresentationResponseProfile profile;
        public PresentationResponseMeasure baseMeasure;

        public bool IsAlive =>
            target != null &&
            target.MeasureRect != null &&
            target.PositionRect != null &&
            target.ScaleRect != null &&
            profile != null;
    }

    private readonly List<RuntimeBinding> _bindings = new();
    private readonly PresentationCameraRootApplier _cameraRootApplier = new();
    private readonly PresentationResponseCoordinateMapper _coordinateMapper = new();
    private readonly StageDepthLayerBinder _stageDepthLayerBinder = new();

    private PresentationIntentState _currentState = PresentationIntentState.Default;

    public PresentationIntentState CurrentState => _currentState;

    public void RegisterRuntimeBinding(
        string bindingKey,
        IResponseTarget target,
        PresentationResponseProfile presetProfile)
    {
        if (string.IsNullOrWhiteSpace(bindingKey))
            return;

        if (!IsValidTarget(target))
            return;

        if (presetProfile == null)
            return;

        RuntimeBinding binding = new()
        {
            key = bindingKey,
            target = target,
            profile = presetProfile,

            // 반드시 등록 시점에만 캡처.
            baseMeasure = _coordinateMapper.CaptureBaseMeasure(target),
        };

        AddOrReplaceBinding(bindingKey, binding);
    }

    // StageDepthLayer처럼 고정 인프라 binding은 baseMeasure를 보존해야함.
    // 따라서 이미 살아있는 binding이 있으면 재등록하지 않고 profile만 교체한다.
    public bool TryUpdateBindingProfile(
        string bindingKey,
        PresentationResponseProfile profile)
    {
        if (string.IsNullOrWhiteSpace(bindingKey))
            return false;

        if (profile == null)
            return false;

        for (int i = _bindings.Count - 1; i >= 0; i--)
        {
            RuntimeBinding binding = _bindings[i];

            if (binding == null)
            {
                _bindings.RemoveAt(i);
                continue;
            }

            if (!string.Equals(binding.key, bindingKey, StringComparison.Ordinal))
                continue;

            if (!binding.IsAlive)
            {
                _bindings.RemoveAt(i);
                return false;
            }

            binding.profile = profile;
            return true;
        }

        return false;
    }

    public bool HasLiveBinding(string bindingKey)
    {
        if (string.IsNullOrWhiteSpace(bindingKey))
            return false;

        for (int i = _bindings.Count - 1; i >= 0; i--)
        {
            RuntimeBinding binding = _bindings[i];

            if (binding == null)
            {
                _bindings.RemoveAt(i);
                continue;
            }

            if (!string.Equals(binding.key, bindingKey, StringComparison.Ordinal))
                continue;

            if (binding.IsAlive)
                return true;

            _bindings.RemoveAt(i);
            return false;
        }

        return false;
    }

    public bool RemoveBinding(string bindingKey)
    {
        bool removed = false;

        if (string.IsNullOrWhiteSpace(bindingKey))
            return false;

        for (int i = _bindings.Count - 1; i >= 0; i--)
        {
            RuntimeBinding binding = _bindings[i];

            if (binding == null)
                continue;

            if (string.Equals(binding.key, bindingKey, StringComparison.Ordinal))
            {
                _bindings.RemoveAt(i);
                removed = true;
            }
        }

        return removed;
    }

    public void ApplyToAllBindings(in PresentationIntentState state)
    {
        _currentState = state;

        // StageDepth는 무대 고정 인프라임에도 매 프레임 호출 중.
        _stageDepthLayerBinder.EnsureBindings(this);

        _cameraRootApplier.Apply(in state);

        for (int i = _bindings.Count - 1; i >= 0; i--)
        {
            RuntimeBinding binding = _bindings[i];

            if (binding == null || !binding.IsAlive)
            {
                _bindings.RemoveAt(i);
                continue;
            }

            PresentationTargetResponse responseForTarget =
                BuildTargetSpaceResponse(in state, binding);

            binding.target.ApplyResponse(in responseForTarget);
        }
    }

    public void Clear()
    {
        _currentState = PresentationIntentState.Default;

        // 살아있는 binding이 있다면 default response를 한 번 적용해서 중립 상태로 되돌린다.
        ApplyToAllBindings(_currentState);

        _bindings.Clear();

        // binding이 모두 제거된 뒤에도 camera root는 반드시 default로 보정.
        _cameraRootApplier.Apply(in _currentState);
    }

    private PresentationTargetResponse BuildTargetSpaceResponse(
        in PresentationIntentState state,
        RuntimeBinding binding)
    {
        // bind 시점에 저장해둔 baseMeasure만 기준으로 계산.
        PresentationTargetResponse responseInRigSpace =
            PresentationResponseMath.CalculateTargetTransformResponseFromShotIntent(
                in state,
                binding.profile,
                in binding.baseMeasure);

        // 계산 결과는 rig space상의 "목표점" 형태로 나오지만,
        // 실제 PositionRect에는 절대 위치를 꽂지 않음.
        // basePosition에서 얼마나 벗어났는지 offset만 추출해서 target parent space로 변환.
        Vector2 offsetInRigSpace =
            responseInRigSpace.anchoredPosition -
            binding.baseMeasure.basePositionInRigSpace;

        Vector2 offsetInParentSpace =
            _coordinateMapper.ConvertOffsetFromRigSpaceToTargetParentSpace(
                offsetInRigSpace,
                binding.target);

        responseInRigSpace.anchoredPosition =
            binding.baseMeasure.baseAnchoredPosition + offsetInParentSpace;

        return responseInRigSpace;
    }

    private void AddOrReplaceBinding(string key, RuntimeBinding newBinding)
    {
        for (int i = 0; i < _bindings.Count; i++)
        {
            RuntimeBinding binding = _bindings[i];

            if (binding == null)
                continue;

            if (string.Equals(binding.key, key, StringComparison.Ordinal))
            {
                _bindings[i] = newBinding;
                return;
            }
        }

        _bindings.Add(newBinding);
    }

    private static bool IsValidTarget(IResponseTarget target)
    {
        return target != null &&
               target.MeasureRect != null &&
               target.PositionRect != null &&
               target.ScaleRect != null;
    }
}