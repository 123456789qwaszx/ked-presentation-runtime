using System;
using System.Collections.Generic;

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

        responseInRigSpace.anchoredPosition =
            _coordinateMapper.ConvertPositionFromRigSpaceToTargetParentSpace(
                responseInRigSpace.anchoredPosition,
                binding.target);

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