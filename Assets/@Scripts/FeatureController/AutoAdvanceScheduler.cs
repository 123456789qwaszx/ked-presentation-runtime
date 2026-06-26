using System;
using System.Threading;
using TMPro;
using Yarn.Markup;
using Yarn.Unity;

public sealed class AutoAdvanceScheduler : ActionMarkupHandler
{
    private VnPlaybackRuntimeState _playbackSettings;
    private DialogueAdvanceDispatcher _dialogueAdvanceDispatcher;
    private Func<double> _getNow;

    private double _nextAutoAdvanceAt = double.PositiveInfinity;

    public void Initialize(
        VnPlaybackRuntimeState vnPlaybackSettings,
        DialogueAdvanceDispatcher dialogueAdvanceDispatcher,
        Func<double> getNow)
    {
        _playbackSettings = vnPlaybackSettings;
        _dialogueAdvanceDispatcher = dialogueAdvanceDispatcher;
        _getNow = getNow;
        Disarm();
    }

    public override void OnPrepareForLine(MarkupParseResult line, TMP_Text text) { }

    public override void OnLineDisplayBegin(MarkupParseResult line, TMP_Text text)
    {
        Disarm();
    }

    public override YarnTask OnCharacterWillAppear(
        int currentCharacterIndex,
        MarkupParseResult line,
        CancellationToken cancellationToken)
    {
        return YarnTask.CompletedTask;
    }

    public override void OnLineDisplayComplete()
    {
        ArmAutoAdvance();
    }

    public override void OnLineWillDismiss() { }

    public void Tick()
    {
        if (_dialogueAdvanceDispatcher == null || _getNow == null)
            return;

        if (_getNow() < _nextAutoAdvanceAt)
            return;

        // dispatch 실행 전 예약.
        // 하지만 dispatch가 no-op이면(표시 중인 라인 뷰가 없어 hurry-up이 무시되는 경우)
        // 사전에 예약된 것을 활용 해 Auto 복구.
        ArmAutoAdvance();
        _dialogueAdvanceDispatcher.DispatchAutoAdvance();
    }

    public void ResetAutoAdvanceTimer()
    {
        ArmAutoAdvance();
    }

    private void ArmAutoAdvance()
    {
        if (_playbackSettings == null || _getNow == null)
        {
            Disarm();
            return;
        }

        _nextAutoAdvanceAt = _getNow() + _playbackSettings.AutoAdvanceDelay;
    }

    private void Disarm()
    {
        _nextAutoAdvanceAt = double.PositiveInfinity;
    }
}