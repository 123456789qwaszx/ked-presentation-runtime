using UnityEngine;

// Baker(메커니즘) 계약.
// Command(StageDepthDefocusCommand)는 이 인터페이스 1개에만 의존한다.
//   - 가시성/세기(alpha)와 edge hide 전이는 Command가 소유한다.
//   - 캡처·블러·스냅샷·매 프레임 추적 재bake와 RawImage(texture/uvRect/enabled)는 Baker가 소유한다.
// "이 layer가 지금 defocus 상태"라는 steady-state는 Command 수명이 아니라 Baker가 든다
// (BeginLayer~EndLayer 사이). 그래서 visible tween이 끝나도 추적 재bake가 지속된다.
public interface IStageDepthLayerBlurRuntime
{
    // Command가 overlay handle을 resolve한다. (provider 단일 접근점은 Baker가 캡슐화.)
    bool TryResolveTarget(
        PresentationStageKey stage,
        PresentationDepthLayerKey layer,
        out PresentationDepthDefocusTarget target);

    // 추적 시작 + 즉시 force-bake로 텍스처를 선준비한다(빈 fade-in 방지).
    // 이미 추적 중이면 파라미터만 갱신하고 다시 굽는다(idempotent).
    void BeginLayer(
        PresentationStageKey stage,
        PresentationDepthLayerKey layer,
        in PresentationDepthDefocusTarget target,
        CommandRunScope scope,
        in StageDepthBlurParams blurParams);

    // 추적 종료 + RawImage 끔. alpha fade는 Command가 먼저 끝낸 뒤 호출하는 계약이다.
    void EndLayer(
        PresentationStageKey stage,
        PresentationDepthLayerKey layer);
}

// Baker가 굽는 데 필요한 값만 담는다. alpha/edgeHide는 Command 소유이므로 여기 없다.
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