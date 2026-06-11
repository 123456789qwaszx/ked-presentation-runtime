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

    private PresentationIntentState _currentState = PresentationIntentState.Default;

    public PresentationIntentState CurrentState => _currentState;

    public void RegisterRuntimeBinding(
        string bindingKey,
        IResponseTarget target,
        PresentationResponseProfile presetProfile)
    {
        if (string.IsNullOrEmpty(bindingKey))
            return;

        if (target == null || presetProfile == null)
            return;

        if (!IsValidTarget(target))
            return;

        RuntimeBinding binding = new()
        {
            key = bindingKey,
            target = target,
            profile = presetProfile,

            // 중요:
            // response 기준값은 매 Apply마다 다시 측정하면 안 된다.
            // bind 시점의 neutral/base 상태를 고정 저장해야 shot_reset이 안정적으로 돌아온다.
            baseMeasure = _coordinateMapper.CaptureBaseMeasure(target),
        };

        AddOrReplaceBinding(bindingKey, binding);
    }

    public bool RemoveBinding(string bindingKey)
    {
        bool removed = false;

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
        _bindings.Clear();

        _cameraRootApplier.Apply(in _currentState);
    }

    private PresentationTargetResponse BuildTargetSpaceResponse(
        in PresentationIntentState state,
        RuntimeBinding binding)
    {
        // 중요:
        // 여기서 현재 Transform을 다시 측정하지 않는다.
        // 등록 시점에 저장해둔 baseMeasure를 기준으로만 response를 계산한다.
        PresentationTargetResponse responseInRigSpace =
            PresentationResponseMath.CalculateTargetTransformResponseFromShotIntent(
                in state,
                binding.profile,
                in binding.baseMeasure);

        // 핵심:
        // Math가 돌려준 anchoredPosition = basePositionInRigSpace + offset (절대 rig-space 위치).
        // 이 절대 위치를 다시 꽂으면(re-pin) 안 된다.
        // 그러면 profile이 0이어도 base를 MeasureRect에서 떠서 world 경유로 PositionRect에
        // 다시 꽂게 되고, 카메라 zoom 중 그 변환이 흔들려 Y가 떨린다.
        //
        // 대신 offset(= focusSpread + pan, profile=0이면 0)만 떼어내 벡터로 변환하고,
        // 중립 anchoredPosition에 "더한다". offset=0이면 anchoredPosition은
        // baseAnchoredPosition 그대로 → binding 안 한 것과 완전히 동일하게 카메라만 따라간다.
        Vector2 offsetInRigSpace =
            responseInRigSpace.anchoredPosition - binding.baseMeasure.basePositionInRigSpace;

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