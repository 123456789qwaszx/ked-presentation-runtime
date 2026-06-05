using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PresentationResponseRig
{
    private PresentationIntentState _currentState = PresentationIntentState.Default;

    private readonly List<PresentationResponseBinding> _bindings = new();
    private readonly PresentationCameraRootApplier _cameraRootApplier = new();

    public PresentationIntentState CurrentState => _currentState;
    
    public bool RegisterRuntimeBinding(string bindingKey, IResponseTarget target, PresentationResponseProfile presetProfile)
    {
        PresentationResponseCoordinateMapper coordinateMapper = new(target);
        
        PresentationResponseProfile runtimeProfile = new()
        {
            focusSpreadPixelsPerZoom = presetProfile.focusSpreadPixelsPerZoom,
            panResponse = presetProfile.panResponse,
            basePositionInRigSpace = coordinateMapper.CaptureNeutralPivotInRigSpace(target.MeasureRect),
            baseLocalScale = new Vector2(target.ScaleRect.localScale.x, target.ScaleRect.localScale.y)
        };
        
        PresentationResponseBinding binding = new(bindingKey, runtimeProfile, target, coordinateMapper);

        AddOrReplaceBinding(bindingKey, binding);
        binding.Apply(in _currentState);

        return true;
    }
    
    public void ApplyToAllBindings(in PresentationIntentState state)
    {
        _currentState = state;

        _cameraRootApplier.Apply(in state);

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
    
    public void Clear()
    {
        _currentState = PresentationIntentState.Default;
        _bindings.Clear();
        _cameraRootApplier?.Apply(in _currentState);
    }

    
    private void AddOrReplaceBinding(string key, PresentationResponseBinding newBinding)
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
    
    private bool RemoveBinding(string bindingKey)
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
}