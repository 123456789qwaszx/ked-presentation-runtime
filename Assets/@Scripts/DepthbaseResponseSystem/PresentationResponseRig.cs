using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PresentationResponseRig : MonoBehaviour
{
    private readonly PresentationCameraRootApplier _cameraRootApplier = new();
    
    private readonly List<PresentationResponseBinding> _bindings = new();
    private PresentationIntentState _currentState = PresentationIntentState.Default;

    public PresentationIntentState CurrentState => _currentState;

    public float EvaluateCameraScale(float zoom)
    {
        if (_cameraRootApplier == null)
            return 1f + Mathf.Clamp(zoom, -10f, 10f) * 0.05f;

        return _cameraRootApplier.EvaluateScale(zoom);
    }

    public void ApplyToAllBindings(in PresentationIntentState state)
    {
        _currentState = state;

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

    public bool RegisterCharacterRigBinding(
        CommandRunScope scope,
        string targetKey,
        PresentationResponseProfile presetProfile,
        RectTransform stageRoot,
        string bindingKey = null)
    {
        CharacterRigRefs rigRefs = CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, targetKey);

        CharacterRigResponseTarget target =
            new CharacterRigResponseTarget(rigRefs);

        string key = bindingKey;

        return RegisterRuntimeBinding(
            key,
            target,
            presetProfile,
            stageRoot);
    }

    public bool RegisterBackgroundRigBinding(
        CommandRunScope scope,
        string bgKey,
        PresentationResponseProfile presetProfile,
        RectTransform stageRoot,
        string bindingKey = null)
    {
        BackgroundRigRefs rigRefs =
            BackgroundRigTargetResolver.ResolveBackgroundRigFromTargetKey(scope, bgKey);

        BackgroundRigResponseTarget target = new BackgroundRigResponseTarget(rigRefs);

        string key = bindingKey;

        return RegisterRuntimeBinding(key, target, presetProfile, stageRoot);
    }

    public bool RegisterRuntimeBinding(
        string key,
        IResponseTarget target,
        PresentationResponseProfile presetProfile,
        RectTransform stageRoot)
    {
        PresentationResponseProfile runtimeProfile =
            BakeRuntimeProfile(target, presetProfile, stageRoot);

        PresentationResponseBinding binding = new PresentationResponseBinding(key, runtimeProfile, target, stageRoot);

        ReplaceBinding(key, binding);

        // Runtime 중 새로 등록된 target도 현재 shot state에 즉시 맞춘다.
        binding.Apply(in _currentState);

        return true;
    }

    public bool RemoveBinding(string key)
    {
        bool removed = false;

        for (int i = _bindings.Count - 1; i >= 0; i--)
        {
            PresentationResponseBinding binding = _bindings[i];

            if (binding == null)
                continue;

            if (string.Equals(binding.Key, key, StringComparison.Ordinal))
            {
                _bindings.RemoveAt(i);
                removed = true;
            }
        }

        return removed;
    }

    public bool RemoveCharacterRigBinding(string targetKey)
    {
        return RemoveBinding(targetKey);
    }

    public bool RemoveBackgroundRigBinding(string bgKey)
    {
        return RemoveBinding(bgKey);
    }

    public void Clear()
    {
        _currentState = PresentationIntentState.Default;
        _bindings.Clear();
    }
    

    private void ReplaceBinding(
        string key,
        PresentationResponseBinding newBinding)
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

    private static PresentationResponseProfile BakeRuntimeProfile(
        IResponseTarget target,
        PresentationResponseProfile presetProfile,
        RectTransform stageRoot)
    {
        PresentationResponseProfile profile = new PresentationResponseProfile
        {
            maxZoomScaleDelta = presetProfile.maxZoomScaleDelta,
            maxZoomSpreadPixels = presetProfile.maxZoomSpreadPixels,
            panResponse = presetProfile.panResponse
        };

        Vector3 worldPivot = target.MeasureRect.TransformPoint(Vector3.zero);
        Vector3 localPivot = stageRoot.InverseTransformPoint(worldPivot);

        profile.basePositionInRigSpace = new Vector2(
            localPivot.x,
            localPivot.y);

        RectTransform scaleRect = target.ScaleRect != null
            ? target.ScaleRect
            : target.PositionRect;

        profile.baseScale = scaleRect != null
            ? new Vector2(scaleRect.localScale.x, scaleRect.localScale.y)
            : Vector2.one;

        return profile;
    }
}