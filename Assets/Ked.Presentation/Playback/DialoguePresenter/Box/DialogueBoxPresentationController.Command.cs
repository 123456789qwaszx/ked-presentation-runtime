public partial class DialogueBoxPresentationController
{
    // surface_layout is a persistent presentation state mutation.
    // It is intentionally not applied to the currently committed box here,
    // because front-matter commands for the next line may run while the previous
    // line is still visible. The layout is applied deterministically at ShowLineAsync,
    // before PrimeText.
    public void SetSurfaceLayout(string presetKey)
    {
        _surfaceState.SetLayout(presetKey);
    }

    public void ResetSurfaceLayout()
    {
        _surfaceState.Reset();
    }

    // 메신저 구간도 같은 방식의 상태 변경이다 — 화면은 건드리지 않는다.
    // 다음 줄의 진입이 기록에 적고(YarnLineBoundary), 그 줄의 ShowLineAsync가 메신저 뷰를 고른다.
    // 닫아도 패널은 다음 줄의 전환이 감출 때까지 그대로 보인다.
    public void OpenMessenger(string me)
    {
        _messengerLog.Open(me);
    }

    public void CloseMessenger()
    {
        _messengerLog.Close();
    }
}
