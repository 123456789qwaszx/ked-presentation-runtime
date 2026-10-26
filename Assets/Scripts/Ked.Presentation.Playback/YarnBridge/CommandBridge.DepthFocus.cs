public sealed partial class YarnCommandBridge
{
    // focus_on @1            — @1에 초점, 나머지는 깊이 차이만큼 흐려진다
    // focus_on @1 0.6        — 최대 흐림을 0.6으로 제한
    // focus_on @1 0.6 0.8s   — 0.8초에 걸쳐
    private void EnqueueDepthFocusOnSpec(
        string slotKey,
        float maxBlur = 1f,
        string durationToken = "0.4s") 
        => Collect(new DepthFocusCommandSpecCharR
        {
            slotKey = slotKey,
            maxBlur = maxBlur,
            duration = YarnDurationParser.Parse(durationToken)
        });

    private void EnqueueDepthFocusClearSpec(string durationToken = "0.4s")
        => Collect(new DepthFocusCommandSpecCharR
        {
            clear = true,
            duration = YarnDurationParser.Parse(durationToken)
        });
}