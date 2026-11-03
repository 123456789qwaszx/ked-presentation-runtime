public enum LineAdvanceMode
{
    None = 0,
    WaitForInput,     // 입력이나 자동 진행
    AutoWhenShown,    // #auto - 다 보이면 입력 없이 넘김
}

// 롤백으로 도착한 줄은 #auto를 무시.
// - 무시하지 않으면,
// 도착하자마자 다음 줄로 넘어가
// 그 줄 앞으로는 되감을 수 없는 경우가 생길 수 있음.
public static class LineAdvancePolicy
{
    public static LineAdvanceMode Resolve(
        SeekKind entrySeekKind,
        SeekLineDecision seekDecision,
        string[] metadata)
    {
        if (seekDecision == SeekLineDecision.SkipVisualAndDispatchSeekNext)
            return LineAdvanceMode.None;

        if (seekDecision == SeekLineDecision.TargetLineReached
            && entrySeekKind == SeekKind.Rollback)
            return LineAdvanceMode.WaitForInput;

        return LineAdvanceTag.IsAuto(metadata)
            ? LineAdvanceMode.AutoWhenShown
            : LineAdvanceMode.WaitForInput;
    }
}
