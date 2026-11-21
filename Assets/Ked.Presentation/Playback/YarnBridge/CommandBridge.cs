using System.Collections.Generic;
using System.Text;
using Ked.Presentation.Core;
using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    private readonly YarnPlaybackDriver _playbackDriver;
    private readonly StageCommandParser _parser;
    private readonly CommandRoleGate _roleGate;
    private readonly DialogueBoxPresentationController _dialogueBoxPresentation;

    // Forward가 쓰는 스크래치 — 커맨드 하나가 스펙 여럿을 낳는다(show·cast·bg_spawn).
    private readonly List<CommandSpecBase> _parsed = new();

    public YarnCommandBridge(
        DialogueRunner runner,
        YarnPlaybackDriver playbackDriver,
        StageCommandParser parser,
        PresentationStage stage,
        DialogueBoxPresentationController dialogueBoxPresentation)
    {
        _playbackDriver = playbackDriver;
        _parser = parser;
        _roleGate = new CommandRoleGate(stage);
        _dialogueBoxPresentation = dialogueBoxPresentation;

        BindRunnerCommands(runner);
    }

    private void BindRunnerCommands(DialogueRunner runner)
    {
        RegisterCharFocusPlacementCommands(runner);
        RegisterCharRigPlacementCommands(runner);
        BindBackground(runner);
        
        BindControl(runner);

        BindCharRigSetup(runner);
        BindEmoji(runner);
        BindCharRigPresentation(runner);
        BindCharRigStaging(runner);
        BindCharRigComposition(runner);

        BindShotStaging(runner);

        BindTransition(runner);
        BindAudio(runner);
        
        BindScreenEffects(runner);
        BindStageDepthDefocus(runner);
        BindDepthFocus(runner);
    }

    private void BindControl(DialogueRunner runner)
    {
        runner.AddCommandHandler<string>(
            "pause", EnqueueWaitSpec);

        runner.AddCommandHandler<string, string>(
            "actor", EnqueuePresentationActorAliasSpec);

        runner.AddCommandHandler<string>(
            "surface_layout", SetSurfaceLayout);

        runner.AddCommandHandler(
            "surface_reset", ResetSurfaceLayout);

        // <<step>>(옛 표기) — 실행하지 않는다. 여기까지 쌓인 명령이 다음 대사 줄의 앞 앵커 하나다(docs/PLAN_line-anchor.md A6).
        runner.AddCommandHandler(
            "step", CloseStageStep);

        // <<at 표지 명령 …>> — 다음 대사 줄의 표지 [beat=표지/] 자리에서 실행할 명령(docs/PLAN_line-beat.md).
        runner.AddCommandHandler<string, string[]>(
            LineBeatSchedule.AtCommandName, CollectAt);
    }
    
    private void BindEmoji(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string, string>(
            "emoji", EnqueueSetEmojiSpec);

        runner.AddCommandHandler<string, string>(
            "emoji_off", EnqueueClearEmojiSpec);
    }

    private void BindCharRigSetup(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string, string>(
            "slot", EnqueueSetupCharRigSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "cast", EnqueueCastCharacterSpec);
        runner.AddCommandHandler<string, string>(
            "pose", EnqueueSetPortraitPoseSpec);
        runner.AddCommandHandler<string, string>(
            "face", EnqueueSetPortraitFaceSpec);
        runner.AddCommandHandler<string, string, string>(
            "face_swap", EnqueueSetEmotionPortraitWipeSpec);

        runner.AddCommandHandler<string, string>(
            "mirror", EnqueueMirrorSetSpec);
    }
    
    private void BindCharRigStaging(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string, string, string, string>(
            "move_by", EnqueueSetAnchorOffsetSpecs);
        runner.AddCommandHandler<string, string, string, string>(
            "rotate_by", EnqueueRotateBySpec);

        runner.AddCommandHandler<string, string, string, string>(
            "scale_by", EnqueueSizeBySpec);

        runner.AddCommandHandler<string, string>(
            "move_reset", EnqueueSetPlaceResetSpecs);
        runner.AddCommandHandler<string, string, string>(
            "rotate_reset", EnqueueRotateResetSpec);

        runner.AddCommandHandler<string, string, string>(
            "scale_reset", EnqueueSizeResetSpec);

        // 모션 — motions.json의 클립(모션 툴·Animation 창에서 내보낸 것). 길이는 클립이 안다.
        // 세 번째 인자 relative|absolute(생략 relative)가 키 값을 어떻게 읽을지 정한다 — 클립은 mode를 모른다(motions/2).
        runner.AddCommandHandler<string, string, string>(
            "motion", EnqueueMotionSpec);
        
        runner.AddCommandHandler<string>(
            "sibling_front", EnqueueCharacterSiblingFrontSpec);

        runner.AddCommandHandler<string>(
            "sibling_back", EnqueueCharacterSiblingBackSpec);
    }
    
    private void BindShotStaging(DialogueRunner runner)
    {
        // 묶음 — 줌 + 팬
        runner.AddCommandHandler<string, string, string, string, string, string>(
            "shot_focus_to", EnqueueShotFocusToSpec);
        runner.AddCommandHandler<string, string, string, string, string>(
            "shot_to", EnqueueShotToSpec);

        // 축마다 하나 — 렌즈(줌 · 팬) · 헤드(롤) · 몸(달리 · 트랙)
        runner.AddCommandHandler<string, string, string>(
            "shot_zoom", EnqueueShotZoomSpec);
        runner.AddCommandHandler<string, string, string, string>(
            "shot_pan", EnqueueShotPanSpec);
        runner.AddCommandHandler<string, string, string>(
            "shot_roll", EnqueueShotRollSpec);
        runner.AddCommandHandler<string, string, string>(
            "shot_dolly", EnqueueShotDollySpec);
        runner.AddCommandHandler<string, string, string, string>(
            "shot_track", EnqueueShotTrackSpec);

        // 되돌리기 — 렌즈만 / 통째로
        runner.AddCommandHandler<string, string>(
            "shot_reset", EnqueueShotResetSpec);
        runner.AddCommandHandler<string, string>(
            "shot_reset_all", EnqueueShotResetAllSpec);

        // 연기 — 화면 흔들림
        runner.AddCommandHandler<string, string>(
            "shot_shake", EnqueueShotShakeSpec);
    }
    
    private void RegisterCharRigPlacementCommands(DialogueRunner runner)
    {
        // Show
        runner.AddCommandHandler<string, string, string>(
            "show", EnqueueShowAtSpec);
        
        // Nudge
        runner.AddCommandHandler<string, string, string, string>(
            "left", EnqueueNudgeLeftSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "right", EnqueueNudgeRightSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "up", EnqueueNudgeUpSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "down", EnqueueNudgeDownSpec);
    }
    
    private void RegisterCharFocusPlacementCommands(DialogueRunner runner)
    {
        // Char focus placement
        runner.AddCommandHandler<string, string, string, string, string>(
            "place", EnqueuePlaceCharacterFocusSpec);
        
        runner.AddCommandHandler<string, string, string, string>(
            "place_left", EnqueueFocusToLeftSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "place_center", EnqueueFocusToCenterSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "place_right", EnqueueFocusToRightSpec);

        // Char focus depth — <<size 슬롯 깊이 [프레이밍] [길이] [이징]>>
        // <<size c1 close bust 12fr>>
        // <<size c1 front>>
        runner.AddCommandHandler<string, string, string, string, string>(
            "size", EnqueueDepthAtPresetSpec);

        runner.AddCommandHandler<string, string, string>(
            "size_reset", EnqueueDepthResetSpec);
    }
    
    private void BindCharRigPresentation(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string>(
            "fade_in", EnqueueFadeInSpec);

        runner.AddCommandHandler<string, string>(
            "fade_out", EnqueueFadeOutSpec);

        runner.AddCommandHandler<string, string, string>(
            "fade", EnqueueFadeToSpec);

        runner.AddCommandHandler<string, string, string, string, string, string>(
            "slide_in", EnqueueSlideInSpec);

        runner.AddCommandHandler<string, string, string, string, string, string>(
            "slide_out", EnqueueSlideOutSpec);
        
        runner.AddCommandHandler<string, string, string, string>(
            "char_move_to", EnqueueMoveByUnitCharSpec);
        
        runner.AddCommandHandler<string, string, string, string>(
            "char_scale_to", EnqueueScaleSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "char_rotate_to", EnqueuePortraitRotateToSpec);
    }
    
    private void BindCharRigComposition(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string, string, string>(
            "char_visual", EnqueueCharVisualPresetSpec);
        
        runner.AddCommandHandler<string, string, string>(
            "char_visual_focus", EnqueueCharFocusSpec);

        runner.AddCommandHandler<string, string, string>(
            "char_visual_defocus", EnqueueCharDefocusSpec);


        runner.AddCommandHandler<string, string, string>(
            "char_visual_dim", EnqueueCharDimSpec);

        runner.AddCommandHandler<string, string, string>(
            "char_visual_silhouette", EnqueueCharSilhouetteSpec);

        runner.AddCommandHandler<string, string, string>(
            "char_visual_inner_rim", EnqueueCharInnerRimSpec);

        runner.AddCommandHandler<string, string, string>(
            "char_visual_outer_rim", EnqueueCharOuterRimSpec);
        
        runner.AddCommandHandler<string, string>(
            "char_visual_clear", EnqueueCharRigVisualClearSpec);
    }
    
    // 배경 = 캐스팅하지 않은 캐릭터 슬롯. 뜻은 무대의 StageCommandParser.Background에 있다.
    // bg_place·bg_size·bg_slotNN은 지운 어휘라 등록하지 않는다 — 남은 대본은 Yarn이 "모르는 커맨드"로 소리 낸다.
    private void BindBackground(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string, string, string>(
            "bg_spawn", EnqueueSpawnBackgroundSpec);

        runner.AddCommandHandler<string, string>(
            "bg_sprite", EnqueueSetBackgroundSpriteSpec);

        runner.AddCommandHandler<string, string>(
            "bg_fade_in", EnqueueFadeInBackgroundDslSpec);

        runner.AddCommandHandler<string, string>(
            "bg_fade_out", EnqueueFadeOutBackgroundDslSpec);

        runner.AddCommandHandler<string, string, string, string>(
            "bg_move", EnqueueMoveBackgroundDslSpec);

        runner.AddCommandHandler<string, string, string>(
            "bg_scale", EnqueueScaleBackgroundDslSpec);
    }
    
    // <<stage_mask 프리셋 [무대] [시간]>> — 무대의 마스크 모양을 움직인다. 프리셋은 무대의 StageMaskMotionPresetDB 키.
    private void BindTransition(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string, string>(
            "stage_mask", EnqueueStageMaskMotionPresetSpec);

        runner.AddCommandHandler(
            "stage_mask_reset",
            EnqueueStageMaskClearSpec);

        // <<cutin_open 무대 판 x y [배율] [모션]>> · <<cutin_close 무대 [모션]>> — docs/PLAN_cutin.md
        runner.AddCommandHandler<string, string, string, string, string, string>(
            "cutin_open", EnqueueCutInOpenSpec);

        runner.AddCommandHandler<string, string>(
            "cutin_close", EnqueueCutInCloseSpec);

        // <<popup 무대 그림 [x y] [배율]>> · <<popup_close 무대>> — docs/PLAN_popup.md
        runner.AddCommandHandler<string, string, string, string, string>(
            "popup", EnqueuePopupOpenSpec);

        runner.AddCommandHandler<string>(
            "popup_close", EnqueuePopupCloseSpec);

        // <<portrait 캐릭터 [포즈] [표정]>> · <<portrait_close>> — docs/PLAN_portrait-capture.md
        runner.AddCommandHandler<string, string, string>(
            "portrait", EnqueuePortraitOpenSpec);

        runner.AddCommandHandler(
            "portrait_close", EnqueuePortraitCloseSpec);
    }
    
    private void BindAudio(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string>(
            "bgm", EnqueuePlayBgmSpec);
        runner.AddCommandHandler<string>(
            "stop_bgm", EnqueueStopBgmSpec);

        runner.AddCommandHandler<string>(
            "sfx", EnqueuePlaySfxSpec);
        runner.AddCommandHandler(
            "stop_all_sfx", EnqueueStopAllSfxSpec);
        
        runner.AddCommandHandler<string>(
            "voice", EnqueuePlayVoiceSpec);
        runner.AddCommandHandler(
            "stop_voice", EnqueueStopVoiceSpec);
    }
    
    private void BindScreenEffects(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string, string>(
            "screen_cover", EnqueueScreenCoverSpec);
        runner.AddCommandHandler<string, string>(
            "screen_cover_clear", EnqueueScreenCoverClearSpec);

        runner.AddCommandHandler<string, string>(
            "screen_flash", EnqueueScreenFlashPresetSpec);
        runner.AddCommandHandler(
            "screen_flash_clear", EnqueueScreenFlashClearSpec);

        runner.AddCommandHandler<string, string, string>(
            "screen_vignette", EnqueueScreenVignettePresetSpec);
        runner.AddCommandHandler<string>(
            "screen_vignette_clear", EnqueueScreenVignetteClearSpec);

        runner.AddCommandHandler<string, string, string>(
            "screen_noise", EnqueueScreenNoisePresetSpec);
        runner.AddCommandHandler<string>(
            "screen_noise_clear", EnqueueScreenNoiseClearSpec);
    }
    
    private void BindDepthFocus(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string, string>(
            "focus_on", EnqueueDepthFocusOnSpec);

        runner.AddCommandHandler<string>(
            "focus_clear", EnqueueDepthFocusClearSpec);
    }

    private void BindStageDepthDefocus(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string, string>(
            "stage_blur", EnqueueLayerBlurSpec);

        runner.AddCommandHandler(
            "stage_blur_reset", EnqueueStageLayerBlurClearSpec);

        // 어둡게 — 층마다 선 어둡기 판(docs/PLAN_screen-cover.md §3.2). 층 · 세기 · 길이 · 이징 · 무대.
        runner.AddCommandHandler<string, string, string, string, string>(
            "stage_dim", EnqueueStageLayerDimSpec);
        runner.AddCommandHandler<string, string>(
            "stage_dim_reset", EnqueueStageLayerDimResetSpec);
    }

    /// <summary>
    /// Yarn이 나눠 준 토큰을 StageCommand로 묶어 무대 파서에 넘긴다. 뜻(인자 자리·생략값·단위)은
    /// 파서가 정한다 — 여기는 텍스트를 옮기기만 한다. 생략된 인자는 null로 가고, 파서가 생략값을 채운다.
    /// </summary>
    private void Forward(string commandName, params string[] args)
    {
        _parsed.Clear();

        if (!_parser.TryParse(new StageCommand(commandName, args), _parsed))
        {
            Debug.LogError($"[YarnCommandBridge] 무대 파서가 모르는 커맨드 '{commandName}' — 등록표와 파서 표가 어긋났다.");
            return;
        }

        // 줄이 도착하기 전에 그 줄의 명령이 모두 풀린다 — 앞 앵커(<<step>>이 닫은 구간)가 세울 슬롯도 선언으로 본다.
        if (!_roleGate.TryAccept(_playbackDriver.PendingAll, _parsed, out string reason))
        {
            Debug.LogError($"[YarnCommandBridge] {AuthoredText(commandName, args)} — {reason}. 실행하지 않는다.");
            return;
        }

        for (int i = 0; i < _parsed.Count; i++)
            _playbackDriver.Enqueue(_parsed[i]);
    }

    // <<at 표지 명령 …>> — 모으기만 한다. 명령 글은 닿았을 때 푼다(TryParseInline) — 대상 슬롯은 그때의 무대로 판정한다.
    private void CollectAt(string beat, params string[] command)
    {
        string[] args = new string[(command?.Length ?? 0) + 1];
        args[0] = beat;

        if (command != null)
            command.CopyTo(args, 1);

        if (!LineBeatSchedule.TryReadAt(args, "at", out AtCommand at, out string error))
        {
            Debug.LogError($"[줄의 자리] {AuthoredText(LineBeatSchedule.AtCommandName, args)} — {error}. 실행하지 않는다.");
            return;
        }

        _playbackDriver.EnqueueAt(at);
    }

    // 줄의 앵커에 놓인 명령(<<at>>)을 스펙으로 풀어 output 뒤에 붙인다. authored는 로그용 — 대본에 쓴 꼴.
    // output에 이미 든 것은 같은 앵커의 앞 명령이다 — 대상 판정이 그 선언(actor · slot · cast)을 본다.
    // 전경 명령(wait — pause · screen_cover)은 앞 앵커에서만 받는다(allowWait) — 대사창이 열린 뒤에는
    // 타자기도 넘김도 멈추지 않는다(docs/PLAN_line-beat.md R8 — docs/PLAN_line-anchor.md A7은 N3에서 연다).
    public bool TryParseInline(in StageCommand command, string authored, List<CommandSpecBase> output, bool allowWait = false)
    {
        _parsed.Clear();

        if (!_parser.TryParse(command, _parsed))
        {
            Debug.LogError($"[줄의 자리] 무대 파서가 모르는 커맨드 '{command.Name}': {authored}. 실행하지 않는다.");
            return false;
        }

        for (int i = 0; i < _parsed.Count && !allowWait; i++)
        {
            if (_parsed[i].wait)
            {
                Debug.LogError($"[줄의 자리] 기다리는 명령(pause · screen_cover)은 start · 글 표지 · end에 쓸 수 없다 — 줄의 앞 앵커에 쓴다: {authored}. 실행하지 않는다.");
                return false;
            }
        }

        if (!_roleGate.TryAccept(output, _parsed, out string reason))
        {
            Debug.LogError($"[줄의 자리] {authored} — {reason}. 실행하지 않는다.");
            return false;
        }

        output.AddRange(_parsed);
        return true;
    }

    private static string AuthoredText(string commandName, string[] args)
    {
        var sb = new StringBuilder("<<").Append(commandName);

        for (int i = 0; i < args.Length && args[i] != null; i++)
            sb.Append(' ').Append(args[i]);

        return sb.Append(">>").ToString();
    }
}
