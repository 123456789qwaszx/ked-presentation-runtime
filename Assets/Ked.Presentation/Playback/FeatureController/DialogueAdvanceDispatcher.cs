using Yarn.Unity;

public sealed class DialogueAdvanceDispatcher
{
    private AdvanceGate _gate;
    private DialogueRunner _dialogueRunner;
    private LineDisplayState _lineDisplayState;
    private PlaybackRuntimeState _playbackState;

    public bool IsDialogueRunning =>
        _dialogueRunner != null &&
        _dialogueRunner.IsDialogueRunning;
    
    /// <summary>
    /// 다음 라인을 요청하기 직전, 등가성 하네스의 관측점으로 사용.
    /// </summary>
    public event System.Action BeforeNextLineDispatched;

    public void Initialize(
        AdvanceGate gate,
        DialogueRunner dialogueRunner,
        LineDisplayState lineDisplayState,
        PlaybackRuntimeState playbackState)
    {
        _gate = gate;
        _dialogueRunner = dialogueRunner;
        _lineDisplayState = lineDisplayState;
        _playbackState = playbackState;
    }

    public void DispatchAdvance() => DispatchAdvance(AdvanceRequestKind.User);
    public void DispatchAutoAdvance() => DispatchAdvance(AdvanceRequestKind.Auto);
    public void DispatchRapidSkipAdvance() => DispatchAdvance(AdvanceRequestKind.RapidSkip);

    // #auto 줄 — 다 보인 줄을 넘긴다(서두르기 없음). 다음 줄을 요청했으면 true.
    // 관문이 막으면(연출 명령 · 시크 · 쿨다운) false — 다음 프레임에 다시. Yarn이 멈췄으면 넘길 것이 없다 — true.
    public bool TryDispatchLineAutoAdvance()
    {
        if (!_dialogueRunner.IsDialogueRunning)
            return true;

        if (!_lineDisplayState.IsLineFullyShown)
            return false;

        if (!_gate.TryAccept(AdvanceRequestKind.Auto))
            return false;

        RequestNextLine(AdvanceRequestKind.Auto);
        return true;
    }

    private void DispatchAdvance(AdvanceRequestKind kind)
    {
        if (kind == AdvanceRequestKind.User && _gate.IsForegroundBusy)
        {
            HurryWhileBusy();
            return;
        }

        // 무대 스텝은 줄이 아니다 — 넘길 줄도 서두를 글도 없다. 스스로 끝난다.
        if (_lineDisplayState.IsStageStepActive)
            return;

        if (!_gate.TryAccept(kind))
            return;

        if (!_dialogueRunner.IsDialogueRunning)
            return;

        if (!_lineDisplayState.IsLineFullyShown)
        {
            _dialogueRunner.RequestHurryUpLine();
            _gate.AddCooldownSeconds(_gate.GetCooldownAfterHurryUp(kind));
        }
        else
        {
            RequestNextLine(kind);
        }
    }

    // 전경(pause)이 도는 동안의 사용자 입력 — 클릭하면 언제나 진행된다(PLAN_stage-step-inline-markup.md §7.1).
    //   글이 아직 나오는 줄      글을 서두른다. 스텝은 건드리지 않는다.
    //   무대 스텝               스텝을 서두른다 — 전경이 끝나 스텝이 스스로 끝난다.
    //   글이 다 나온 줄          스텝을 서두르고, 전경이 끝나는 대로 다음 줄로 넘긴다(#auto와 같은 길).
    // 스텝 서두르기 = 압축 래치: 돌던 pause가 그 자리에서 끝나고 남은 명령은 끝값으로 간다.
    // 이미 돌던 트윈은 다음 스텝의 경계가 가속해 끝낸다.
    private void HurryWhileBusy()
    {
        bool stageStep = _lineDisplayState.IsStageStepActive;

        if (!stageStep && !_dialogueRunner.IsDialogueRunning)
            return;

        if (!_gate.TryAcceptWhileBusy())
            return;

        _gate.AddCooldownSeconds(_gate.GetCooldownAfterHurryUp(AdvanceRequestKind.User));

        if (!stageStep && !_lineDisplayState.IsLineFullyShown)
        {
            _dialogueRunner.RequestHurryUpLine();
            return;
        }

        _playbackState.HurryStep();

        if (!stageStep)
            _lineDisplayState.SetLineAutoAdvance(true);
    }

    private void RequestNextLine(AdvanceRequestKind kind)
    {
        BeforeNextLineDispatched?.Invoke();

        _dialogueRunner.RequestNextLine();
        _gate.AddCooldownSeconds(_gate.GetCooldownAfterNextLine(kind));
    }
}