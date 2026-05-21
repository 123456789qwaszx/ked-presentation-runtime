using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PresentationResponseRig : MonoBehaviour
{
    private PresentationIntentState _currentState = PresentationIntentState.Default;

    private readonly List<PresentationResponseBinding> _bindings = new();

    private PresentationCameraRootApplier _cameraRootApplier;
    public PresentationIntentState CurrentState => _currentState;
    
    public void BindCameraRoots(
        RectTransform stagePanRoot,
        RectTransform stageZoomRoot)
    {
        _cameraRootApplier = new PresentationCameraRootApplier(
            stagePanRoot,
            stageZoomRoot);
    }
    
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
                Debug.LogWarning($"[PresentationResponseRig] Binding null. index={i}");
                _bindings.RemoveAt(i);
                continue;
            }

            binding.Apply(in state);
        }
    }

    public bool RegisterRuntimeBinding(
        string key,
        PresentationResponseBinding.IResponseTarget target,
        PresentationResponseProfile presetProfile,
        RectTransform presentationUIRoot)
    {
        if (string.IsNullOrWhiteSpace(key))
            Debug.LogWarning("[PresentationResponseRig] RegisterRuntimeBinding failed. key is null or empty.");

        if (target?.Rect == null)
            Debug.LogWarning($"[PresentationResponseRig] RegisterRuntimeBinding failed. target?.Rect is null. key={key}, target={target}");

        RectTransform stageRoot = presentationUIRoot;
        PresentationResponseProfile runtimeProfile = CreateRuntimeProfile(target, presetProfile, stageRoot);
        PresentationResponseBinding binding = new PresentationResponseBinding(key, runtimeProfile, target, stageRoot);

        for (int i = 0; i < _bindings.Count; i++)
        {
            if (_bindings[i] != null && string.Equals(_bindings[i].Key, key, StringComparison.OrdinalIgnoreCase))
            {
                _bindings[i] = binding;
                return true;
            }
        }

        _bindings.Add(binding);
        return true;
    }

    public void RemoveBinding(string key)
    {
        for (int i = _bindings.Count - 1; i >= 0; i--)
        {
            PresentationResponseBinding binding = _bindings[i];
            if (binding == null)
                continue;

            if (string.Equals(binding.Key, key, StringComparison.OrdinalIgnoreCase))
                _bindings.RemoveAt(i);
        }
    }
    
    public void ClearRuntimeState()
    {
        _currentState = PresentationIntentState.Default;
        _bindings.Clear();
    }

    
    private static PresentationResponseProfile CreateRuntimeProfile(
        PresentationResponseBinding.IResponseTarget target,
        PresentationResponseProfile presetProfile,
        RectTransform stageRoot)
    {
        PresentationResponseProfile profile = new PresentationResponseProfile
        {
            maxZoomScaleDelta = presetProfile.maxZoomScaleDelta,
            maxZoomSpreadPixels = presetProfile.maxZoomSpreadPixels,
            panResponse = presetProfile.panResponse
        };

        Vector3 worldPivot = target.Rect.TransformPoint(Vector3.zero);
        Vector3 localPivot = stageRoot.InverseTransformPoint(worldPivot);
        Vector2 basePositionInRigSpace = new Vector2(localPivot.x, localPivot.y);

        profile.basePositionInRigSpace = basePositionInRigSpace;
        profile.baseScale = new Vector2(target.Rect.localScale.x, target.Rect.localScale.y);
        profile.baseAlpha = target.CanvasGroup.alpha;

        return profile;
    }
    
    private static PresentationResponseProfile BakeRuntimeProfile(
        IResponseTarget target,
        PresentationResponseProfile presetProfile,
        RectTransform stageRoot)
    {
        PresentationResponseProfile profile = presetProfile;

        Vector3 worldPivot = target.MeasureRect.TransformPoint(Vector3.zero);
        Vector3 localPivot = stageRoot.InverseTransformPoint(worldPivot);

        profile.basePositionInRigSpace = new Vector2(localPivot.x, localPivot.y);

        RectTransform scaleRect = target.ScaleRect != null
            ? target.ScaleRect
            : target.PositionRect;

        profile.baseScale = scaleRect != null
            ? new Vector2(scaleRect.localScale.x, scaleRect.localScale.y)
            : Vector2.one;

        profile.baseAlpha = target.CanvasGroup != null
            ? target.CanvasGroup.alpha
            : 1f;

        return profile;
    }
}