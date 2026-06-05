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

        public bool IsAlive =>
            target != null && target.MeasureRect != null &&
            target.PositionRect != null &&
            target.ScaleRect != null;
    }

    private readonly List<RuntimeBinding> _bindings = new();
    private readonly PresentationCameraRootApplier _cameraRootApplier = new();
    private readonly PresentationResponseCoordinateMapper _coordinateMapper = new();
    
    private PresentationIntentState _currentState = PresentationIntentState.Default;
    
    public PresentationIntentState CurrentState => _currentState;

    public void RegisterRuntimeBinding(string bindingKey, IResponseTarget target, PresentationResponseProfile presetProfile)
    {
        PresentationResponseProfile runtimeProfile = new() 
        {
            focusSpreadPixelsPerZoom = presetProfile.focusSpreadPixelsPerZoom,
            panResponse = presetProfile.panResponse,
            basePositionInRigSpace = _coordinateMapper.CaptureNeutralPivotInRigSpace(target),
            baseLocalScale = new Vector2(target.ScaleRect.localScale.x, target.ScaleRect.localScale.y)
        };

        RuntimeBinding binding = new()
        {
            key = bindingKey,
            target = target,
            profile = runtimeProfile
        };

        AddOrReplaceBinding(bindingKey, binding);
        
        PresentationTargetResponse responseForTarget = BuildTargetSpaceResponse(in _currentState, binding);
        binding.target.ApplyResponse(in responseForTarget);
    }
    
    public bool RemoveBinding(string bindingKey)
    {
        bool removed = false;

        for (int i = _bindings.Count - 1; i >= 0; i--)
        {
            RuntimeBinding binding = _bindings[i];

            if (binding == null || !binding.IsAlive)
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
            
            PresentationTargetResponse responseForTarget = BuildTargetSpaceResponse(in state, binding);
            binding.target.ApplyResponse(in responseForTarget);
        }
    }

    public void Clear()
    {
        _currentState = PresentationIntentState.Default;
        _bindings.Clear();
        _cameraRootApplier.Apply(in _currentState);
    }
    
    
    private PresentationTargetResponse BuildTargetSpaceResponse(in PresentationIntentState state, RuntimeBinding binding)
    {
        PresentationTargetResponse responseInRigSpace =
            PresentationResponseMath.CalculateTargetTransformResponseFromShotIntent(in state, binding.profile);

        responseInRigSpace.anchoredPosition =
            _coordinateMapper.ConvertPositionFromRigSpaceToTargetParentSpace(responseInRigSpace.anchoredPosition, binding.target);

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
}