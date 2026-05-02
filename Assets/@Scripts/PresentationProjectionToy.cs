using System;
using UnityEngine;

public sealed class PresentationProjectionToy : MonoBehaviour
{
    [Serializable]
    public sealed class Target
    {
        public string label;
        public RectTransform rect;

        [Header("Physical World")]
        public Vector2 worldPosition;
        public float depthZ = 10f;

        [Header("Captured Reference")]
        public Vector3 referenceLocalScale = Vector3.one;

        [Header("Debug")]
        public Vector2 lastProjectedPosition;
        public float lastScaleFactor = 1f;
    }

    [Header("Viewport")]
    [Tooltip("Usually the RectTransform that represents your stage or canvas area. If empty, this component's RectTransform is used.")]
    public RectTransform viewport;

    [Tooltip("Virtual sensor height in millimeters. 24mm is a common full-frame-ish reference height.")]
    public float sensorHeightMm = 24f;

    [Header("Reference Camera")]
    [Tooltip("The lens value used when you captured the current RectTransform positions as reference.")]
    public float referenceFocalLengthMm = 35f;

    [Tooltip("Reference camera XY position in world units.")]
    public Vector2 referenceCameraPosition;

    [Tooltip("Reference camera Z position. Usually 0.")]
    public float referenceCameraZ = 0f;

    [Header("Current Camera")]
    [Tooltip("Higher value means stronger zoom-in. Try 18, 24, 35, 50, 85.")]
    [Range(12f, 120f)]
    public float focalLengthMm = 35f;

    [Tooltip("Camera movement on the virtual X/Y plane. This produces parallax.")]
    public Vector2 cameraPosition;

    [Tooltip("Camera movement along the view direction. Increasing this moves camera closer to targets.")]
    public float cameraZ = 0f;

    [Tooltip("Screen-space optical center. Usually zero if the stage pivot is centered.")]
    public Vector2 principalPointPixels = Vector2.zero;

    [Header("Targets")]
    public Target[] targets;

    [Header("Safety")]
    public float minEffectiveDepth = 0.1f;

    [Header("Live Preview")]
    public bool applyInEditMode = true;

    private void Reset()
    {
        viewport = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        ApplyProjection();
    }

    private void OnValidate()
    {
        if (!applyInEditMode)
            return;

        ApplyProjection();
    }

    [ContextMenu("1. Capture Current Layout As Reference")]
    public void CaptureCurrentLayoutAsReference()
    {
        RectTransform view = ResolveViewport();

        if (view == null || targets == null)
            return;

        float pixelsPerMm = GetPixelsPerMm(view);
        float projectionScale = referenceFocalLengthMm * pixelsPerMm;

        for (int i = 0; i < targets.Length; i++)
        {
            Target target = targets[i];

            if (target == null || target.rect == null)
                continue;

            float effectiveDepth = Mathf.Max(
                minEffectiveDepth,
                target.depthZ - referenceCameraZ);

            Vector2 screenOffset =
                target.rect.anchoredPosition - principalPointPixels;

            target.worldPosition =
                referenceCameraPosition + screenOffset * effectiveDepth / projectionScale;

            target.referenceLocalScale = target.rect.localScale;
        }

        focalLengthMm = referenceFocalLengthMm;
        cameraPosition = referenceCameraPosition;
        cameraZ = referenceCameraZ;

        ApplyProjection();
    }

    [ContextMenu("2. Apply Projection")]
    public void ApplyProjection()
    {
        RectTransform view = ResolveViewport();

        if (view == null || targets == null)
            return;

        float pixelsPerMm = GetPixelsPerMm(view);

        ProjectionCamera camera = new ProjectionCamera
        {
            focalLengthMm = focalLengthMm,
            cameraPosition = cameraPosition,
            cameraZ = cameraZ,
            principalPointPixels = principalPointPixels,
            pixelsPerMm = pixelsPerMm,
            minEffectiveDepth = minEffectiveDepth
        };

        ProjectionCamera referenceCamera = new ProjectionCamera
        {
            focalLengthMm = referenceFocalLengthMm,
            cameraPosition = referenceCameraPosition,
            cameraZ = referenceCameraZ,
            principalPointPixels = principalPointPixels,
            pixelsPerMm = pixelsPerMm,
            minEffectiveDepth = minEffectiveDepth
        };

        for (int i = 0; i < targets.Length; i++)
        {
            Target target = targets[i];

            if (target == null || target.rect == null)
                continue;

            ProjectionResult result =
                PhysicalProjectionSolver.Project(target, camera, referenceCamera);

            target.rect.anchoredPosition = result.anchoredPosition;
            target.rect.localScale = target.referenceLocalScale * result.scaleFactor;

            target.lastProjectedPosition = result.anchoredPosition;
            target.lastScaleFactor = result.scaleFactor;
        }
    }

    [ContextMenu("3. Reset Camera To Reference")]
    public void ResetCameraToReference()
    {
        focalLengthMm = referenceFocalLengthMm;
        cameraPosition = referenceCameraPosition;
        cameraZ = referenceCameraZ;

        ApplyProjection();
    }

    private RectTransform ResolveViewport()
    {
        if (viewport != null)
            return viewport;

        return GetComponent<RectTransform>();
    }

    private float GetPixelsPerMm(RectTransform view)
    {
        float heightPixels = Mathf.Max(1f, view.rect.height);
        float safeSensorHeight = Mathf.Max(1f, sensorHeightMm);

        return heightPixels / safeSensorHeight;
    }
}

public struct ProjectionCamera
{
    public float focalLengthMm;
    public Vector2 cameraPosition;
    public float cameraZ;
    public Vector2 principalPointPixels;
    public float pixelsPerMm;
    public float minEffectiveDepth;
}

public struct ProjectionResult
{
    public Vector2 anchoredPosition;
    public float scaleFactor;
}

public static class PhysicalProjectionSolver
{
    public static ProjectionResult Project(
        PresentationProjectionToy.Target target,
        ProjectionCamera camera,
        ProjectionCamera referenceCamera)
    {
        float effectiveDepth =
            Mathf.Max(
                camera.minEffectiveDepth,
                target.depthZ - camera.cameraZ);

        float referenceDepth =
            Mathf.Max(
                referenceCamera.minEffectiveDepth,
                target.depthZ - referenceCamera.cameraZ);

        Vector2 relativeWorldPosition =
            target.worldPosition - camera.cameraPosition;

        float currentProjectionScale =
            camera.focalLengthMm * camera.pixelsPerMm;

        Vector2 projectedPosition =
            camera.principalPointPixels +
            relativeWorldPosition * currentProjectionScale / effectiveDepth;

        float currentAngularScale =
            camera.focalLengthMm / effectiveDepth;

        float referenceAngularScale =
            referenceCamera.focalLengthMm / referenceDepth;

        float scaleFactor =
            currentAngularScale / referenceAngularScale;

        ProjectionResult result = new ProjectionResult
        {
            anchoredPosition = projectedPosition,
            scaleFactor = scaleFactor
        };

        return result;
    }
}