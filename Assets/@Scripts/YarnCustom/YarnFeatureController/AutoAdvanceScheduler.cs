using System;
using System.Threading;
using TMPro;
using UnityEngine;
using Yarn.Markup;
using Yarn.Unity;

public sealed class AutoAdvanceScheduler : ActionMarkupHandler
{
    private VnPlaybackSettings _playbackSettings;
    private DialogueAdvanceDispatcher _dialogueAdvanceDispatcher;
    private Func<double> _getNow;

    private double _nextAutoAdvanceAt = double.PositiveInfinity;

    public void Initialize(
        VnPlaybackSettings vnPlaybackSettings,
        DialogueAdvanceDispatcher dialogueAdvanceDispatcher,
        Func<double> getNow)
    {
        _playbackSettings = vnPlaybackSettings;
        _dialogueAdvanceDispatcher = dialogueAdvanceDispatcher;
        _getNow = getNow;
        _nextAutoAdvanceAt = double.PositiveInfinity;
    }

    public override void OnPrepareForLine(MarkupParseResult line, TMP_Text text)
    {
    }

    public override void OnLineDisplayBegin(MarkupParseResult line, TMP_Text text)
    {
        NotifyLineStart();
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
        NotifyLineFinishDisplaying();
    }

    public override void OnLineWillDismiss()
    {
    }

    public void NotifyLineStart()
    {
        _nextAutoAdvanceAt = double.PositiveInfinity;
    }

    public void NotifyLineStart(YarnLineMeta meta)
    {
        NotifyLineStart();
    }

    public void NotifyLineFinishDisplaying()
    {
        if (_playbackSettings == null || _getNow == null)
        {
            _nextAutoAdvanceAt = double.PositiveInfinity;
            return;
        }

        _nextAutoAdvanceAt = _getNow() + _playbackSettings.autoModeDelaySeconds;
    }

    public void NotifyLineFinishDisplaying(YarnLineMeta meta)
    {
        NotifyLineFinishDisplaying();
    }

    public void Tick()
    {
        if (_dialogueAdvanceDispatcher == null || _getNow == null)
            return;

        double t = _getNow();

        if (t < _nextAutoAdvanceAt)
            return;

        _nextAutoAdvanceAt = double.PositiveInfinity;
        _dialogueAdvanceDispatcher.DispatchAdvance();
    }

    public void ResetAutoAdvanceTimer()
    {
        if (_playbackSettings == null || _getNow == null)
        {
            _nextAutoAdvanceAt = double.PositiveInfinity;
            return;
        }

        _nextAutoAdvanceAt = _getNow() + _playbackSettings.autoModeDelaySeconds;
    }

    public void NotifyChoicesPresented()
    {
        _nextAutoAdvanceAt = double.PositiveInfinity;
    }

    public void NotifyBacklogOpened()
    {
        _nextAutoAdvanceAt = double.PositiveInfinity;
    }
}