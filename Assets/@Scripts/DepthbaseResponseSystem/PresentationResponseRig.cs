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
        CharacterRigRefs rigRefs =
            CharacterRigTargetResolver.ResolveCharRigFromTargetKey(scope, targetKey);

        CanvasGroup canvasGroup = GetOrAddCanvasGroup(rigRefs.RigRoot);

        CharacterRigResponseTarget target =
            new CharacterRigResponseTarget(rigRefs, canvasGroup);

        string key = string.IsNullOrWhiteSpace(bindingKey)
            ? BuildCharacterBindingKey(targetKey)
            : bindingKey;

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

        CanvasGroup canvasGroup = GetOrAddCanvasGroup(rigRefs.RigRoot);

        BackgroundRigResponseTarget target =
            new BackgroundRigResponseTarget(rigRefs, canvasGroup);

        string key = string.IsNullOrWhiteSpace(bindingKey)
            ? BuildBackgroundBindingKey(bgKey)
            : bindingKey;

        return RegisterRuntimeBinding(
            key,
            target,
            presetProfile,
            stageRoot);
    }

    public bool RegisterRuntimeBinding(
        string key,
        IResponseTarget target,
        PresentationResponseProfile presetProfile,
        RectTransform stageRoot)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            Debug.LogWarning("[PresentationResponseRig] RegisterRuntimeBinding failed. key is null or empty.", this);
            return false;
        }

        if (target == null)
        {
            Debug.LogWarning($"[PresentationResponseRig] RegisterRuntimeBinding failed. target is null. key='{key}'.", this);
            return false;
        }

        if (target.MeasureRect == null)
        {
            Debug.LogWarning($"[PresentationResponseRig] RegisterRuntimeBinding failed. target.MeasureRect is null. key='{key}'.", this);
            return false;
        }

        if (target.PositionRect == null)
        {
            Debug.LogWarning($"[PresentationResponseRig] RegisterRuntimeBinding failed. target.PositionRect is null. key='{key}'.", this);
            return false;
        }

        if (presetProfile == null)
        {
            Debug.LogWarning($"[PresentationResponseRig] RegisterRuntimeBinding failed. presetProfile is null. key='{key}'.", this);
            return false;
        }

        if (stageRoot == null)
        {
            Debug.LogWarning($"[PresentationResponseRig] RegisterRuntimeBinding failed. stageRoot is null. key='{key}'.", this);
            return false;
        }

        PresentationResponseProfile runtimeProfile =
            BakeRuntimeProfile(target, presetProfile, stageRoot);

        PresentationResponseBinding binding =
            new PresentationResponseBinding(
                key,
                runtimeProfile,
                target,
                stageRoot);

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
        return RemoveBinding(BuildCharacterBindingKey(targetKey));
    }

    public bool RemoveBackgroundRigBinding(string bgKey)
    {
        return RemoveBinding(BuildBackgroundBindingKey(bgKey));
    }

    public void ClearRuntimeState()
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

        profile.baseAlpha = target.CanvasGroup != null
            ? target.CanvasGroup.alpha
            : 1f;

        return profile;
    }

    private static CanvasGroup GetOrAddCanvasGroup(RectTransform root)
    {
        if (root == null)
            return null;

        CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = root.gameObject.AddComponent<CanvasGroup>();

        return canvasGroup;
    }

    private static string BuildCharacterBindingKey(string targetKey)
    {
        return $"char:{targetKey}";
    }

    private static string BuildBackgroundBindingKey(string bgKey)
    {
        return $"bg:{bgKey}";
    }
}