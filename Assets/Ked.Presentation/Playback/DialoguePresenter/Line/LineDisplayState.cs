using UnityEngine;

// 지금 줄의 표시 상태 — 다 보였나 · #auto 넘김 대기 · 무대 스텝 중. 시크는 SeekState가 가진다.
public sealed class LineDisplayState
{
    public bool IsLineFullyShown { get; private set; } = true;
    
    // 지금 줄이 #auto이고 아직 넘기지 않았다(LineAdvanceTag). 다 보이면 재생 입력의 프레임이 넘기고 내린다.
    // 줄에 들어갈 때 · 무너질 때 내린다 — 다음 줄로 새지 않는다.
    public bool IsLineAutoAdvancePending { get; private set; }

    // #auto=길이 — 줄이 들어온 때부터 이만큼 머문 뒤에 넘긴다(docs/PLAN_line-beat.md R9). 0이면 다 보이면 바로.
    // 시계는 pause와 같은 실시간(WaitCommand — SetUpdate(true))이다.
    public float LineAutoAdvanceHoldSeconds { get; private set; }

    private double _lineEnteredAt;

    public bool IsLineAutoHoldElapsed => Time.unscaledTimeAsDouble - _lineEnteredAt >= LineAutoAdvanceHoldSeconds;

    // 무대 스텝(<<step>> · 노드 끝)이 도는 중이다. 줄이 아니다 — 넘길 줄도 서두를 글도 없다.
    // 앞 줄의 "다 보임"이 남아 있으면 자동 진행이 한 번 더 넘기므로 들어갈 때 내린다.
    public bool IsStageStepActive { get; private set; }

    public void MarkLineEntered()
    {
        IsLineFullyShown = false;
        IsLineAutoAdvancePending = false;
        LineAutoAdvanceHoldSeconds = 0f;
        _lineEnteredAt = Time.unscaledTimeAsDouble;
        IsStageStepActive = false;
    }

    public void MarkStageStepEntered()
    {
        IsLineFullyShown = false;
        IsLineAutoAdvancePending = false;
        IsStageStepActive = true;
    }

    public void MarkStageStepExited()
    {
        IsStageStepActive = false;
    }
    
    // 줄마다 한 번, 넘김 정책(LineAdvancePolicy)이 정한 대로 — 머무름은 #auto=길이.
    // 전경 중의 클릭(DialogueAdvanceDispatcher)도 부른다 — 머무름 없이(0), 전경이 끝나는 대로 넘긴다.
    public void SetLineAutoAdvance(bool autoAdvance, float holdSeconds = 0f)
    {
        IsLineAutoAdvancePending = autoAdvance;
        LineAutoAdvanceHoldSeconds = autoAdvance ? Mathf.Max(0f, holdSeconds) : 0f;
    }

    // 넘김 요청이 받아들여졌다 — 같은 줄에서 두 번 넘기지 않는다.
    public void ConsumeLineAutoAdvance()
    {
        IsLineAutoAdvancePending = false;
    }

    // 라인이 안정된 경계까지 갔다(= Ready).
    public void MarkLineDisplayCompleted(string reason)
    {
        IsLineFullyShown = true;
    }

    // 라인이 정상 경계 전에 무너짐.(= Released). 롤백 · 정지 · 선점으로 잘린 경우.
    public void MarkLineTornDown(string reason)
    {
        IsLineFullyShown = false;
        IsLineAutoAdvancePending = false;
        LineAutoAdvanceHoldSeconds = 0f;
    }
}