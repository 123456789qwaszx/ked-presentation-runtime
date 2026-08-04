using UnityEngine;

// Stage depth blur baking/runtime contract.
// Command owns presentation transitions such as alpha and edge hide.
// Runtime owns capture, blur, texture binding, uvRect, and tracking rebakes.
// Defocus steady-state is held here, not by the command lifetime.
// (Defocus 유지 상태는 Command 수명이 아니라 Runtime에 남는다.)
public interface IStageDepthLayerBlurRuntime
{
    // Resolves the overlay target for a stage/layer pair.
    void ResolveTarget(
        PresentationStageKey stage,
        PresentationDepthLayerKey layer,
        out PresentationDepthDefocusTarget target);

    // Starts tracking and performs an immediate bake to avoid empty fade-in.
    // Re-entering the same layer updates params and bakes again.
    void BeginLayer(
        PresentationStageKey stage,
        PresentationDepthLayerKey layer,
        in PresentationDepthDefocusTarget target,
        CommandRunScope scope,
        in StageDepthBlurParams blurParams);

    // Stops tracking and disables the baked overlay.
    // Call after command-owned fade-out has completed.
    void EndLayer(
        PresentationStageKey stage,
        PresentationDepthLayerKey layer);
}

// Blur bake parameters only.
// Alpha and edge-hide values are owned by the command.
public readonly struct StageDepthBlurParams
{
    public readonly float BlurRadius;
    public readonly int Iterations;
    public readonly UIStageBlurDownsample Downsample;
    public readonly float CoveragePaddingPixels;

    public StageDepthBlurParams(
        float blurRadius,
        int iterations,
        UIStageBlurDownsample downsample,
        float coveragePaddingPixels)
    {
        BlurRadius = Mathf.Max(0f, blurRadius);
        Iterations = Mathf.Clamp(iterations, 1, 6);
        Downsample = downsample;
        CoveragePaddingPixels = Mathf.Max(0f, coveragePaddingPixels);
    }
}