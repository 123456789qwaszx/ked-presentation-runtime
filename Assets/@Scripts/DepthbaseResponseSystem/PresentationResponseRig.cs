using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PresentationResponseRig : MonoBehaviour
{
    [Header("Shot Settings")]
    [SerializeField] private Vector2 _defaultFramingPoint = Vector2.zero;
    [SerializeField] private Vector2 _manualPanPixelsPerUnit = new Vector2(64f, 36f);

    [Header("Runtime Bindings")]
    [SerializeField] private List<PresentationResponseBinding> _bindings = new();

    private PresentationIntentState _currentState = PresentationIntentState.Default;

    public PresentationIntentState CurrentState => _currentState;

    public void ApplyImmediate(
        PresentationIntentState state,
        PresentationViewRefs presentation)
    {
        Debug.Log(
            $"[PresentationResponseRig] ApplyImmediate incomingZoom={state.zoom}, " +
            $"currentZoom={CurrentState.zoom}");

        _currentState = state;
        
        ApplyToAllBindings(in state, presentation);
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
        {
            Debug.LogWarning("[PresentationResponseRig] RegisterRuntimeBinding failed. key is null or empty.");
            return false;
        }

        if (target == null)
        {
            Debug.LogWarning(
                $"[PresentationResponseRig] RegisterRuntimeBinding failed. target is null. key={key}");

            return false;
        }

        if (target.Rect == null)
        {
            Debug.LogWarning(
                $"[PresentationResponseRig] RegisterRuntimeBinding failed. target.Rect is null. key={key}, target={target}");

            return false;
        }

        PresentationResponseProfile runtimeProfile =
            CreateRuntimeProfile(target, preset, presentation);

        RemoveBinding(key);

        _bindings.Add(new PresentationResponseBinding(
            key,
            runtimeProfile,
            target));

        Debug.Log(
            $"[PresentationResponseRig] RegisterRuntimeBinding success. " +
            $"key={key}, target={target.Rect.name}, count={_bindings.Count}");

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

    public Vector2 ComposePanForFocus(Vector2 focusPoint)
    {
        return ComposePanForFocus(focusPoint, _defaultFramingPoint);
    }

    public Vector2 ComposePanForFocus(
        Vector2 focusPoint,
        Vector2 desiredFramingPoint)
    {
        return desiredFramingPoint - focusPoint;
    }

    public Vector2 GetManualPanPixels(Vector2 authoringPanUnits)
    {
        return new Vector2(
            authoringPanUnits.x * _manualPanPixelsPerUnit.x,
            authoringPanUnits.y * _manualPanPixelsPerUnit.y);
    }

    private void ApplyToAllBindings(
        in PresentationIntentState state,
        PresentationViewRefs presentation)
    {
        Debug.Log($"[PresentationResponseRig] ApplyToAllBindings count={_bindings.Count}");

        for (int i = 0; i < _bindings.Count; i++)
        {
            PresentationResponseBinding binding = _bindings[i];

            if (binding == null)
            {
                Debug.LogWarning($"[PresentationResponseRig] Binding null. index={i}");
                continue;
            }

            Debug.Log(
                $"[PresentationResponseRig] Apply binding index={i}, key={binding.Key}");

            binding.Apply(in state, presentation);
        }
    }

    private static PresentationResponseProfile CreateRuntimeProfile(
        IRectTransformPresentationResponseTarget target,
        PresentationResponseProfile preset,
        PresentationViewRefs presentation)
    {
        RectTransform stageRoot = presentation != null
            ? presentation.GetRect(PresentationTarget.Stage_Root)
            : null;

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