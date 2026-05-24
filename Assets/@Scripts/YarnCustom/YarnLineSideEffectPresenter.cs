using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

public sealed class YarnLineSideEffectPresenter : DialoguePresenterBase
{
    private PresentationSessionContext _context;
    private LinePresentationAdvanceState _linePresentationAdvanceState;
    private AudioSystem _audioSystem;
    private YarnBridgePlaybackDriver _yarnBridgePlaybackDriver;

    public void Initialize(
        DialogueRunner dialogueRunner,
        PresentationSessionContext context,
        LinePresentationAdvanceState linePresentationAdvanceState,
        YarnBridgePlaybackDriver yarnBridgePlaybackDriver = null,
        AudioSystem audioSystem = null)
    {
        _context = context;
        _linePresentationAdvanceState = linePresentationAdvanceState;
        _yarnBridgePlaybackDriver = yarnBridgePlaybackDriver;
        _audioSystem = audioSystem;

        if (dialogueRunner == null)
        {
            Debug.LogError($"{nameof(YarnLineSideEffectPresenter)}: dialogueRunner is null.");
            return;
        }

        RegisterBeforeVisualLinePresenter(dialogueRunner);
    }

    public override YarnTask OnDialogueStartedAsync()
    {
        return YarnTask.CompletedTask;
    }

    public override YarnTask OnDialogueCompleteAsync()
    {
        StopVoice();
        return YarnTask.CompletedTask;
    }

    public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        //Debug.Log($"[YarnLineSideEffectPresenter] RollbackSeeking={_linePresentationAdvanceState != null && _linePresentationAdvanceState.IsRollbackSeeking}");
        
        PlayVoice(line);
        
        _yarnBridgePlaybackDriver?.ResetImmediateWaitForNewLine();
        _yarnBridgePlaybackDriver?.PlayCollected();

        return YarnTask.CompletedTask;
    }

    private void PlayVoice(LocalizedLine line)
    {
        StopVoice();

        if (line == null)
            return;

        AudioClip clip = line.Asset as AudioClip;
        if (clip == null)
            return;

        _audioSystem?.Voice?.Play(clip);
    }

    private void StopVoice()
    {
        _audioSystem?.Voice?.Stop();
    }

    private bool ShouldConsumeLineSilently()
    {
        if (_context == null)
            return false;

        return _linePresentationAdvanceState.IsSeeking ||
               _context.IsSpeedUpMode;
    }

    private void RegisterBeforeVisualLinePresenter(DialogueRunner dialogueRunner)
    {
        List<DialoguePresenterBase> presenters = new List<DialoguePresenterBase>(dialogueRunner.DialoguePresenters);
        presenters.Remove(this);

        int insertIndex = FindVisualLinePresenterIndex(presenters);

        if (insertIndex < 0)
            insertIndex = 0;

        presenters.Insert(insertIndex, this);
        dialogueRunner.DialoguePresenters = presenters;
    }

    private static int FindVisualLinePresenterIndex(List<DialoguePresenterBase> presenters)
    {
        int customLinePresenterIndex = presenters.FindIndex(x => x is CustomLinePresenter);
        if (customLinePresenterIndex >= 0)
            return customLinePresenterIndex;

        int yarnLinePresenterIndex = presenters.FindIndex(x => x is LinePresenter);
        if (yarnLinePresenterIndex >= 0)
            return yarnLinePresenterIndex;

        return -1;
    }
}