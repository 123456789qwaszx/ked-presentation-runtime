using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PresentationResponseRig : MonoBehaviour
{
    [Header("Runtime Bindings")]
    [SerializeField] private List<PresentationResponseBinding> _bindings = new();

    private PresentationIntentState _currentState = PresentationIntentState.Default;

    public PresentationIntentState CurrentState => _currentState;

    public void ApplyImmediate(PresentationIntentState state, PresentationViewRefs presentation)
    {
        _currentState = state;
        ApplyToAllBindings(in state);
    }

    private void ApplyToAllBindings(in PresentationIntentState state)
    {
        for (int i = _bindings.Count - 1; i >= 0; i--)
        {
            PresentationResponseBinding binding = _bindings[i];

            if (binding == null)
            {
                Debug.LogWarning($"[PresentationResponseRig] Binding null. index={i}");
                _bindings.RemoveAt(i);
                continue;
            }

            if (!binding.IsAlive)
            {
                Debug.LogWarning($"[PresentationResponseRig] Dead binding removed. key={binding.Key}");
                _bindings.RemoveAt(i);
                continue;
            }

            binding.Apply(in state);
        }
    }
    
    public void ResetCurrentState()
    {
        _currentState = PresentationIntentState.Default;
    }

    public void SetCurrentStateOnly(PresentationIntentState state)
    {
        _currentState = state;
    }

    public bool RegisterRuntimeBinding(
        string key,
        IRectTransformPresentationResponseTarget target,
        PresentationResponseProfile preset,
        PresentationViewRefs presentation)
    {
        if (string.IsNullOrWhiteSpace(key))
            Debug.LogWarning("[PresentationResponseRig] RegisterRuntimeBinding failed. key is null or empty.");
        
        if (target == null)
            Debug.LogWarning($"[PresentationResponseRig] RegisterRuntimeBinding failed. target is null. key={key}");
        
        if (target.Rect == null)
            Debug.LogWarning($"[PresentationResponseRig] RegisterRuntimeBinding failed. target.Rect is null. key={key}, target={target}");

        RectTransform stageRoot = presentation.GetRect(PresentationTarget.Stage_Root);
        PresentationResponseProfile runtimeProfile = CreateRuntimeProfile(target, preset, stageRoot);
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
        if (string.IsNullOrWhiteSpace(key))
            return;

        for (int i = _bindings.Count - 1; i >= 0; i--)
        {
            PresentationResponseBinding binding = _bindings[i];
            if (binding == null)
                continue;

            if (string.Equals(binding.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"[PresentationResponseRig] RemoveBinding key={key}");
                _bindings.RemoveAt(i);
            }
        }
    }

    private static PresentationResponseProfile CreateRuntimeProfile(
        IRectTransformPresentationResponseTarget target,
        PresentationResponseProfile preset,
        RectTransform stageRoot)
    {
        PresentationResponseProfile profile = new PresentationResponseProfile
        {
            maxZoomScaleDelta = preset.maxZoomScaleDelta,
            maxZoomSpreadPixels = preset.maxZoomSpreadPixels,
            panResponse = preset.panResponse
        };

        Vector3 worldPivot = target.Rect.TransformPoint(Vector3.zero);

        profile.basePositionInRigSpace = WorldToSpacePoint(stageRoot, worldPivot);
        profile.baseScale =
            new Vector2(target.Rect.localScale.x, target.Rect.localScale.y);
        profile.baseAlpha =
            target.CanvasGroup != null ? target.CanvasGroup.alpha : 1f;

        return profile;
    }

    public static Vector2 WorldToSpacePoint(
        RectTransform stageRoot,
        Vector3 worldPoint)
    {
        if (stageRoot == null)
            return new Vector2(worldPoint.x, worldPoint.y);

        Vector3 local = stageRoot.InverseTransformPoint(worldPoint);
        return new Vector2(local.x, local.y);
    }
}