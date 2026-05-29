using System;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    private readonly DialogueRunner _dialogueRunner;
    private readonly DialogueRunner _subPresentationRunner;
    private readonly DialogueAdvanceDispatcher _dialogueAdvanceDispatcher;
    
    private readonly YarnBridgePlaybackDriver _playbackDriver;
    private readonly RectTransform _charRigPrefab;
    
    private readonly Dictionary<string, DialogueAdvanceDispatcher> _advanceDispatchersByKey = new();
    
    public YarnCommandBridge(
        DialogueRunner dialogueRunner,
        DialogueRunner subPresentationRunner,
        DialogueAdvanceDispatcher dialogueAdvanceDispatcher,
        YarnBridgePlaybackDriver playbackDriver, 
        RectTransform charRigPrefab)
    {
        _dialogueRunner = dialogueRunner;
        _subPresentationRunner = subPresentationRunner;
        _dialogueAdvanceDispatcher = dialogueAdvanceDispatcher;
        
        _playbackDriver = playbackDriver;
        _charRigPrefab = charRigPrefab;
        
        BindRunnerCommands(_dialogueRunner);

        if (_subPresentationRunner != null && !ReferenceEquals(_subPresentationRunner, _dialogueRunner))
            BindRunnerCommands(_subPresentationRunner);

        RegisterAdvanceDispatcher("cue", _dialogueAdvanceDispatcher);

        // Main Runner only commands.
        // _dialogueRunner.AddCommandHandler("go", DispatchAdvanceToRunner);
        // _dialogueRunner.AddCommandHandler<string>("sub_start", StartSubPresentationNode);
        _dialogueRunner.AddCommandHandler("go", EnqueueSubPresentationAdvanceSpec);
        _dialogueRunner.AddCommandHandler<string>("sub_start", EnqueueSubPresentationStartSpec);
    }
    
    private void EnqueueSubPresentationAdvanceSpec()
    {
        var spec = new SubPresentationAdvanceCommandSpec
        {
            label = "cue"
        };

        Collect(spec);
    }

    private void EnqueueSubPresentationStartSpec(string nodeName)
    {
        var spec = new SubPresentationStartCommandSpec
        {
            nodeName = nodeName,
            restartIfRunning = true
        };

        Collect(spec);
    }

    private void BindRunnerCommands(DialogueRunner runner)
    {
        if (runner == null)
        {
            Debug.LogWarning("[YarnCommandBridge] Cannot bind commands. DialogueRunner is null.");
            return;
        }

        BindControl(runner);

        BindCharRigSetup(runner);
        BindCharRigBasic(runner);
        BindCharRigActing(runner);
        BindCharRigIdle(runner);
        BindCharRigPreset(runner);

        BindCharRigEmote(runner);

        BindBackgroundRig(runner);
        BindShotResponse(runner);

        BindTransition(runner);
        BindAudio(runner);
    }
    
    private void RegisterAdvanceDispatcher(string key, DialogueAdvanceDispatcher dispatcher)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            Debug.LogWarning("[YarnCommandBridge] Cannot register advance dispatcher. key is null or empty.");
            return;
        }

        if (dispatcher == null)
        {
            Debug.LogWarning($"[YarnCommandBridge] Cannot register advance dispatcher. key='{key}', dispatcher is null.");
            return;
        }

        _advanceDispatchersByKey[key] = dispatcher;
    }
    
    private void DispatchAdvanceToRunner()
    {
        if (!_advanceDispatchersByKey.TryGetValue("cue", out DialogueAdvanceDispatcher dispatcher) || dispatcher == null)
        {
            Debug.LogWarning("[YarnCommandBridge] advance_runner failed. No dispatcher registered for key='cue'.");
            return;
        }

        dispatcher.DispatchSubPresentationAdvance();
    }
    
    private void StartSubPresentationNode(string nodeName)
    {
        _subPresentationRunner.StartDialogue(nodeName);
    }

    private void BindControl(DialogueRunner runner)
    {
        // Marks the next N collected commands as wait=true inside Presentation/Executor.
        // This affects command playback order, but does NOT block Yarn by itself.
        runner.AddCommandHandler<int>("await", AwaitFor);

        // Starts a Yarn-level hold block.
        runner.AddCommandHandler("hold_begin", BeginHold);

        // Blocking Yarn command:
        // closes the hold block and pauses Yarn until the held commands
        // marked with wait=true finish inside Presentation/Executor.
        runner.AddCommandHandler("hold_end", PlayHeldCommands);
        
        runner.AddCommandHandler<float>("pause", EnqueueWaitSpec);
        
        runner.AddCommandHandler<string>("ui_patch", EnqueueUIPatchSpec);
        
        runner.AddCommandHandler<float>("box_hide", EnqueueHideDialogueBoxSpec);
        
        runner.AddCommandHandler<string>("debug_log", LogImmediate);
        runner.AddCommandHandler<string>("debug_state", LogYarnState);
    }
    
    private void LogImmediate(string message)
    {
        Debug.Log($"[YarnCommandBridge] {message}");
    }
    
    private void LogYarnState(string label)
    {
        VariableStorageBehaviour storage = _dialogueRunner.VariableStorage;

        storage.TryGetValue("$favor", out float favor);
        storage.TryGetValue("$laru_patience", out float patience);
        storage.TryGetValue("$willow_debt", out float debt);
        storage.TryGetValue("$requested_fee", out float requestedFee);
        storage.TryGetValue("$paid_fee", out float paidFee);
        storage.TryGetValue("$trust", out float trust);
        storage.TryGetValue("$anger", out float anger);
        storage.TryGetValue("$contract_signed", out bool contractSigned);

        Debug.Log(
            $"[YarnState] {label} | " +
            $"favor={favor}, patience={patience}, debt={debt}, " +
            $"requested={requestedFee}, paid={paidFee}, trust={trust}, " +
            $"anger={anger}, contract={contractSigned}"
        );
    }

    private void BindCharRigSetup(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string>("slot", EnqueueSetupCharRigSpec);
        
        runner.AddCommandHandler<string, string, string, string, bool, string, string>("cast", EnqueueCastCharacterSpec);
        
        runner.AddCommandHandler<string, string, string, string>("pose", EnqueueSetPortraitSpriteSpec);
        runner.AddCommandHandler<string, string, bool, bool>("place", EnqueueSetAnchorSpecs);
        runner.AddCommandHandler<string, string>("size", EnqueueSetOriginSizeSpec);
        
        runner.AddCommandHandler<string, int, int>("place_offset", EnqueueSetAnchorOffsetSpecs);
    }

    private void BindCharRigBasic(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, float>("fade_in", EnqueueFadeInSpec);
        runner.AddCommandHandler<string, float>("fade_out", EnqueueFadeOutSpec);
        
        runner.AddCommandHandler<string, string>("expression", EnqueueSetEmotionPortraitWipeSpec);
        runner.AddCommandHandler<string, string>("expression_crossfade", EnqueueSetPortraitCrossfadeSpec);
        
        runner.AddCommandHandler<string, string>("slide_in", EnqueueSlideInSpec);
        runner.AddCommandHandler<string, string>("slide_out", EnqueueSlideOutSpec);
        
        runner.AddCommandHandler<string, float, float, float>("move_by", EnqueueMoveByCharSpec);
        runner.AddCommandHandler<string, float, float>("scale_to", EnqueueScaleToSpec);
        runner.AddCommandHandler<string, int>("rotate_to", EnqueuePivotRotateToSpec);
    }
    
    private void BindCharRigActing(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string>("dip", EnqueueDipInOutSpec);
        
        runner.AddCommandHandler<string, int, float, float>("hop", EnqueueHopSpec);
        
        runner.AddCommandHandler<string, string, float, float, int>("shake", EnqueueJoltSpecShake);
        runner.AddCommandHandler<string, float, float, float, string>("tremble", EnqueueTrembleSpec);
        
        runner.AddCommandHandler<string>("sway", EnqueueSwaySpec);
    }
    
    private void BindCharRigIdle(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, float, float, float, float>("idle_bounce", EnqueueBounceInPlaceSpec);
        runner.AddCommandHandler<string, float, float, float>("idle_breathe", EnqueueBreathInPlaceSpec);
        runner.AddCommandHandler<string, float, float, float, float, float, string>("idle_flinch", EnqueueTremblePulseSpec);
        runner.AddCommandHandler<string, float, float, float, float>("idle_walk", EnqueueWalkInPlaceSpec);
    }

    private void BindCharRigPreset(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string>("jolt", EnqueueJoltSpec);
        runner.AddCommandHandler<string, string>("nudge", EnqueueJoltSpecTap);
        runner.AddCommandHandler<string, string>("nudge_hard", EnqueueJoltSpecTapHard);
        
        runner.AddCommandHandler<string>("slide_in_sway", EnqueueSlideInSwayCombo);
        runner.AddCommandHandler<string, string>("slide_in_nudge", EnqueueSlideInJoltCombo);
        
        runner.AddCommandHandler<string>("sway_hard", EnqueueSwaySpecPendulum);
        runner.AddCommandHandler<string>("sway_fast", EnqueueSwaySpecFast);
        runner.AddCommandHandler<string>("sway_away", EnqueueSwaySpecAway);
    }
    
    private void BindCharRigEmote(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string>("emote", EnqueueSetCharacterEmojiSpec);
        runner.AddCommandHandler<string, string, string>("emote_slot", EnqueueSetCharacterEmojiSlotSpec);
        
        runner.AddCommandHandler<string>("emote_hide", EnqueueHideCharacterEmojiSpec);
        runner.AddCommandHandler<string, string>("emote_hide_slot", EnqueueHideCharacterEmojiSlotSpec);
    }
    
    private void BindBackgroundRig(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string, string, string, string, float, float, float>("spawn_bg", EnqueueSpawnBackgroundRigSpec);
        
        runner.AddCommandHandler<string, float, float, float>("bg_place", EnqueueSetBackgroundAnchorSpec);
        runner.AddCommandHandler<string, string, string>("bg_sprite", EnqueueSetBackgroundSpriteSpec);
        runner.AddCommandHandler<string, string>("bg_size", EnqueueSetBackgroundOriginSizeSpec);
        
        runner.AddCommandHandler<string, string, float>("bg_fade_in", EnqueueFadeInBackgroundSpec);
        runner.AddCommandHandler<string, string, float>("bg_fade_out", EnqueueFadeOutBackgroundSpec);
        
        runner.AddCommandHandler<string, float, float, float>("bg_move", EnqueueMoveBackgroundSpec);
        runner.AddCommandHandler<string, float, float>("bg_scale", EnqueueScaleBackgroundSpec);
        
        runner.AddCommandHandler<string, string, float, float>("bg_slide_in", EnqueueSlideInBackgroundSpec);
        runner.AddCommandHandler<string, string, float, float>("bg_slide_out", EnqueueSlideOutBackgroundSpec);
        runner.AddCommandHandler<string, string, float, float>("bg_jolt", EnqueueJoltBackgroundSpec);
        
        runner.AddCommandHandler<string, string, float, float>("bg_idle_tremble", EnqueueTrembleBackgroundSpec);
        runner.AddCommandHandler<string, float, float, float>("bg_idle_breath", EnqueueBreathBackgroundSpec);
    }
    
    private void BindShotResponse(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string>("shot_bind_bg_response", EnqueueRegisterBackgroundResponseBindingSpec);
        runner.AddCommandHandler<string, string>("shot_bind_char_response", EnqueueRegisterCharacterResponseBindingSpec);
        
        runner.AddCommandHandler<string, string, string, float, float>("shot_zoom_focus", EnqueueShotZoomFocusSpec);
        runner.AddCommandHandler<float, float, float, float>("shot_to", EnqueueShotToSpec);
        runner.AddCommandHandler("shot_reset", (Action<float>)EnqueueShotResetSpec);
        
        runner.AddCommandHandler<float, float>("shot_zoom", EnqueueShotZoomSpec);
        runner.AddCommandHandler<float, float, float>("shot_track", EnqueueShotTrackSpec);
    }
    
    private void BindTransition(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, float>("tx_slant_in", EnqueueSlantedMaskCutInSpec);
        runner.AddCommandHandler<string, float>("tx_slant_out", EnqueueSlantedMaskCutOutSpec);

        runner.AddCommandHandler<string, float>("tx_strip_cover", EnqueueVerticalStripCoverSpec);
        runner.AddCommandHandler<string, float>("tx_strip_clear", EnqueueVerticalStripClearSpec);

        runner.AddCommandHandler<string, float>("tx_shutter_open", EnqueueSlantedShutterOpenSpec);
        runner.AddCommandHandler<string, float>("tx_shutter_close", EnqueueSlantedShutterCloseSpec);

        runner.AddCommandHandler<string, float>("tx_focus_fade_out", EnqueueFocusBlurFadeOutSpec);
        runner.AddCommandHandler<string, float>("tx_focus_fade_in", EnqueueFocusBlurFadeInSpec);

        runner.AddCommandHandler<string, float>("tx_focus_curtain_close", EnqueueFocusBlurCurtainCloseSpec);
        runner.AddCommandHandler<string, float>("tx_focus_curtain_open", EnqueueFocusBlurCurtainOpenSpec);

        runner.AddCommandHandler<string, float>("tx_daze_fade_close", EnqueueDazeFadeCloseSpec);
        runner.AddCommandHandler<string, float>("tx_daze_fade_open", EnqueueDazeFadeOpenSpec);
    }
    
    private void BindAudio(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, float>("bgm", EnqueuePlayBgmSpec);
        runner.AddCommandHandler<float>("stop_bgm", EnqueueStopBgmSpec);

        runner.AddCommandHandler<string>("sfx", EnqueuePlaySfxSpec);
        runner.AddCommandHandler("stop_all_sfx", EnqueueStopAllSfxSpec);
        
        runner.AddCommandHandler<string>("voice", EnqueuePlayVoiceSpec);
        runner.AddCommandHandler("stop_voice", EnqueueStopVoiceSpec);
    }

    private void Collect(CommandSpecBase spec)
    {
        _playbackDriver.Enqueue(spec);
    }
}