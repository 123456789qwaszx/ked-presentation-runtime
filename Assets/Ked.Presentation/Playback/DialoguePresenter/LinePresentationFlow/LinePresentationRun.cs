using System.Threading;

// 줄 하나의 화면 작업(박스 전환·페이드) 수명.
// presenter가 다음 줄의 시각 실행을 열거나, 줄을 중단하거나, 대화가 끝나면 토큰이 취소된다.
// await에서 돌아올 때마다 IsValid를 물어 "아직 이 줄의 작업인가"를 확인한다.
// 무효로 만드는 방법은 VisualToken 취소 하나다 — 대사창 뷰는 토큰만 본다.
public sealed class LinePresentationRun
{
    public CancellationToken VisualToken { get; }

    public bool IsValid => !VisualToken.IsCancellationRequested;

    public LinePresentationRun(CancellationToken visualToken)
    {
        VisualToken = visualToken;
    }
}