using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// runtime binding registry + shot state apply 중심.
/// Stage_Root는 소유하지 않고, Apply 시점에 PresentationViewRefs를 받는다.
/// </summary>
public sealed class PresentationResponseRig : MonoBehaviour
{
    [Header("Shot Settings")]
    [SerializeField] private Vector2 _defaultFramingPoint = Vector2.zero;
    [SerializeField] private Vector2 _manualPanPixelsPerUnit = new Vector2(64f, 36f);

    [Header("Runtime Bindings")]
    [SerializeField] private List<PresentationResponseBinding> _bindings = new();

    [Header("Focus Providers")]
    [SerializeField] private List<NamedFocusProvider> _focusProviders = new();

    private PresentationIntentState _currentState = PresentationIntentState.Default;

    public PresentationIntentState CurrentState => _currentState;

    public void ApplyImmediate(PresentationIntentState state, PresentationViewRefs presentation)
    {
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
            return false;

        if (target == null || target.Rect == null)
            return false;

        PresentationResponseProfile runtimeProfile = CreateRuntimeProfile(target, preset, presentation);

        RemoveBinding(key);

        _bindings.Add(new PresentationResponseBinding(
            key,
            runtimeProfile,
            target));

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
                _bindings.RemoveAt(i);
        }
    }

    public Vector2 ComposePanForFocus(Vector2 focusPoint)
    {
        return ComposePanForFocus(focusPoint, _defaultFramingPoint);
    }

    public Vector2 ComposePanForFocus(Vector2 focusPoint, Vector2 desiredFramingPoint)
    {
        return desiredFramingPoint - focusPoint;
    }

    public Vector2 GetManualPanPixels(Vector2 authoringPanUnits)
    {
        return new Vector2(
            authoringPanUnits.x * _manualPanPixelsPerUnit.x,
            authoringPanUnits.y * _manualPanPixelsPerUnit.y);
    }

    public bool TryGetFocusPoint(string key, PresentationViewRefs presentation, out Vector2 focusPoint)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            focusPoint = Vector2.zero;
            return false;
        }

        for (int i = 0; i < _focusProviders.Count; i++)
        {
            NamedFocusProvider named = _focusProviders[i];
            if (named == null || named.provider == null)
                continue;

            if (!string.Equals(named.key, key, StringComparison.OrdinalIgnoreCase))
                continue;

            if (named.provider is not IPresentationFocusProvider provider)
            {
                Debug.LogWarning($"[PresentationResponseRig] Focus provider '{named.key}' does not implement IPresentationFocusProvider.");
                focusPoint = Vector2.zero;
                return false;
            }

            Vector3 world = provider.GetFocusWorldPoint();
            focusPoint = PresentationSpaceUtil.WorldToSpacePoint(
                presentation != null ? presentation.Stage_Root : null,
                world);

            return true;
        }

        focusPoint = Vector2.zero;
        return false;
    }

    public bool TryGetGroupFocusPoint(IReadOnlyList<string> keys, PresentationViewRefs presentation, out Vector2 focusPoint)
    {
        Vector2 sum = Vector2.zero;
        int count = 0;

        if (keys != null)
        {
            for (int i = 0; i < keys.Count; i++)
            {
                if (TryGetFocusPoint(keys[i], presentation, out Vector2 point))
                {
                    sum += point;
                    count++;
                }
            }
        }

        if (count > 0)
        {
            focusPoint = sum / count;
            return true;
        }

        focusPoint = Vector2.zero;
        return false;
    }

    private void ApplyToAllBindings(in PresentationIntentState state, PresentationViewRefs presentation)
    {
        for (int i = 0; i < _bindings.Count; i++)
            _bindings[i]?.Apply(in state, presentation);
    }

    private static PresentationResponseProfile CreateRuntimeProfile(
        IRectTransformPresentationResponseTarget target,
        PresentationResponseProfile preset,
        PresentationViewRefs presentation)
    {
        RectTransform stageRoot = presentation != null ? presentation.Stage_Root : null;

        PresentationResponseProfile profile = new PresentationResponseProfile
        {
            maxZoomScaleDelta = preset.maxZoomScaleDelta,
            maxZoomSpreadPixels = preset.maxZoomSpreadPixels,
            panResponse = preset.panResponse
        };

        Vector3 worldPivot = target.Rect.TransformPoint(Vector3.zero);

        profile.basePositionInRigSpace = PresentationSpaceUtil.WorldToSpacePoint(stageRoot, worldPivot);
        profile.baseScale = new Vector2(target.Rect.localScale.x, target.Rect.localScale.y);
        profile.baseAlpha = target.CanvasGroup != null ? target.CanvasGroup.alpha : 1f;

        return profile;
    }

    [Serializable]
    public sealed class NamedFocusProvider
    {
        public string key;
        public MonoBehaviour provider;
    }
}