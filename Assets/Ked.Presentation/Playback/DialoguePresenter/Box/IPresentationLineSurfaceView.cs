using System.Threading;
using Yarn.Unity;

// 줄을 올리는 표면(대사창 · 메신저)의 공통 — 줄 전환이 쓰는 보이기 · 감추기 · 페이드.
public interface IPresentationLineSurfaceView
{
    void SetVisibleImmediate(bool visible);
    void PrepareHidden();

    // 페이드. 토큰이 취소되면 그 자리에서 멈추고 최종값(보임·숨김)을 쓰지 않는다.
    // 줄 전환이 그 줄의 시각 토큰(LinePresentationRun.VisualToken)을 넘긴다. 줄이 아직 유효한지는 부르는 쪽이 판단한다.
    YarnTask FadeInAsync(float duration, CancellationToken token);
    YarnTask FadeOutAsync(float duration, CancellationToken token);
}
