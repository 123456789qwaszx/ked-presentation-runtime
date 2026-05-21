using DG.Tweening;
using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    public void RegisterPresentationCommands()
    {
        _dialogueRunner.AddCommandHandler("presentation_reset", EnqueueSetupPresentationViewSpec);
        
        _dialogueRunner.AddCommandHandler<string, string>("p_bind_bg_response", EnqueueRegisterBackgroundResponseBindingSpec);
        _dialogueRunner.AddCommandHandler<string, string>("p_bind_char_response", EnqueueRegisterCharacterResponseBindingSpec);
        
        // PresentationTarget direct transform
        _dialogueRunner.AddCommandHandler<string, float>("p_slant_cut_in", EnqueueSlantedMaskCutInSpec);
        _dialogueRunner.AddCommandHandler<string, float>("p_slant_cut_out", EnqueueSlantedMaskCutOutSpec);

        _dialogueRunner.AddCommandHandler<string, float>("p_strip_cover", EnqueueVerticalStripCoverSpec);
        _dialogueRunner.AddCommandHandler<string, float>("p_strip_clear", EnqueueVerticalStripClearSpec);

        _dialogueRunner.AddCommandHandler<string, float>("p_shutter_close", EnqueueSlantedShutterCloseSpec);
        _dialogueRunner.AddCommandHandler<string, float>("p_shutter_open", EnqueueSlantedShutterOpenSpec);

        _dialogueRunner.AddCommandHandler<string, float>("p_focus_fade_out", EnqueueFocusBlurFadeOutSpec);
        _dialogueRunner.AddCommandHandler<string, float>("p_focus_fade_in", EnqueueFocusBlurFadeInSpec);

        _dialogueRunner.AddCommandHandler<string, float>("p_focus_curtain_close", EnqueueFocusBlurCurtainCloseSpec);
        _dialogueRunner.AddCommandHandler<string, float>("p_focus_curtain_open", EnqueueFocusBlurCurtainOpenSpec);

        _dialogueRunner.AddCommandHandler<string, float>("p_daze_fade_close", EnqueueDazeFadeCloseSpec);
        _dialogueRunner.AddCommandHandler<string, float>("p_daze_fade_open", EnqueueDazeFadeOpenSpec);


        _dialogueRunner.AddCommandHandler("box_hide", EnqueueHideDialogueBoxSpec);
    }
    private void EnqueueRegisterBackgroundResponseBindingSpec(
        string rigKey,
        string stageKey = "0")
    {
        var spec = new RegisterBackgroundResponseBindingCommandSpec
        {
            rigKey = rigKey,
            stage = PresentationResponseStageParser.Parse(stageKey),
            bindingKey = "",
            responseProfile = PresentationResponseProfile.Background,
            wait = true
        };

        Collect(spec);
    }

    private void EnqueueRegisterCharacterResponseBindingSpec(
        string targetKey,
        string stageKey = "0")
    {
        var spec = new RegisterCharacterResponseBindingCommandSpec
        {
            targetKey = targetKey,
            stage = PresentationResponseStageParser.Parse(stageKey),
            bindingKey = "",
            responseProfile = PresentationResponseProfile.CharacterSlot,
            wait = true
        };

        Collect(spec);
    }

    private void EnqueueDazeFadeCloseSpec(
        string targetName,
        float duration = 0.85f)
    {

        var spec = new FocusBlurCurtainCommandSpec
        {
            mode = FocusBlurCurtainMode.Close,

            // 화면 중앙이 오래 남도록 gap을 크게 둔다.
            openGapHeight = 680f,
            finalGapHeight = 0f,

            // 셔터 느낌을 줄이기 위해 사선은 약하게.
            slantPixels = 36f,

            // 위아래 경계가 딱 잘리는 느낌보다, 부드럽게 번지는 느낌.
            edgeFeatherHeight = 240f,
            edgeFeatherAlpha = 0.42f,

            // 중앙 흐림 영역을 넓게 잡아서 "멍해지는" 느낌을 만든다.
            centerBlurHeight = 520f,
            centerStartAlpha = 0.08f,
            centerEndAlpha = 0.72f,
            centerBlurSlices = 28,

            color = Color.black,

            // 천천히 멍해지는 전환.
            duration = duration,
            ease = Ease.InOutSine,

            wait = true,
            killTween = true,
            disableWhenOpen = true,
            blockRaycastWhenClosed = false,
        };

        Collect(spec);
    }

    private void EnqueueDazeFadeOpenSpec(
        string targetName,
        float duration = 0.65f)
    {

        var spec = new FocusBlurCurtainCommandSpec
        {

            mode = FocusBlurCurtainMode.Open,

            openGapHeight = 680f,
            finalGapHeight = 0f,
            slantPixels = 36f,

            edgeFeatherHeight = 240f,
            edgeFeatherAlpha = 0.42f,

            centerBlurHeight = 520f,
            centerStartAlpha = 0.08f,
            centerEndAlpha = 0.72f,
            centerBlurSlices = 28,

            color = Color.black,

            duration = duration,
            ease = Ease.InOutSine,

            wait = true,
            killTween = true,
            disableWhenOpen = true,
            blockRaycastWhenClosed = false,
        };

        Collect(spec);
    }

    private void EnqueueFocusBlurCurtainCloseSpec(
        string targetName,
        float duration = 0.55f)
    {
        var spec = new FocusBlurCurtainCommandSpec
        {
            mode = FocusBlurCurtainMode.Close,

            openGapHeight = 520f,
            finalGapHeight = 0f,
            slantPixels = 90f,

            edgeFeatherHeight = 140f,
            edgeFeatherAlpha = 0.55f,

            centerBlurHeight = 320f,
            centerStartAlpha = 0.12f,
            centerEndAlpha = 0.82f,
            centerBlurSlices = 18,

            color = Color.black,

            duration = duration,
            ease = Ease.InOutCubic,

            wait = true,
            killTween = true,
            disableWhenOpen = true,
            blockRaycastWhenClosed = false,
        };

        Collect(spec);
    }

    private void EnqueueFocusBlurCurtainOpenSpec(
        string targetName,
        float duration = 0.42f)
    {

        var spec = new FocusBlurCurtainCommandSpec
        {
            mode = FocusBlurCurtainMode.Open,

            openGapHeight = 520f,
            finalGapHeight = 0f,
            slantPixels = 90f,

            edgeFeatherHeight = 140f,
            edgeFeatherAlpha = 0.55f,

            centerBlurHeight = 320f,
            centerStartAlpha = 0.12f,
            centerEndAlpha = 0.82f,
            centerBlurSlices = 18,

            color = Color.black,

            duration = duration,
            ease = Ease.InOutCubic,

            wait = true,
            killTween = true,
            disableWhenOpen = true,
            blockRaycastWhenClosed = false,
        };

        Collect(spec);
    }


    private void EnqueueFocusBlurFadeOutSpec(
        string targetName,
        float duration = 0.45f)
    {
        var spec = new FocusBlurFadeCommandSpec
        {
            mode = FocusBlurFadeMode.FadeOut,

            color = Color.black,
            maxAlpha = 1f,
            zoomAmount = 0.035f,

            duration = duration,
            ease = Ease.InOutCubic,

            wait = true,
            killTween = true,
            disableWhenClear = true,
            blockRaycastWhenVisible = false,
        };

        Collect(spec);
    }

    private void EnqueueFocusBlurFadeInSpec(
        string targetName,
        float duration = 0.35f)
    {

        var spec = new FocusBlurFadeCommandSpec
        {

            mode = FocusBlurFadeMode.FadeIn,

            color = Color.black,
            maxAlpha = 1f,
            zoomAmount = 0.035f,

            duration = duration,
            ease = Ease.InOutCubic,

            wait = true,
            killTween = true,
            disableWhenClear = true,
            blockRaycastWhenVisible = false,
        };

        Collect(spec);
    }

    private void EnqueueSlantedShutterCloseSpec(
        string targetName,
        float duration = 0.38f)
    {

        var spec = new SlantedShutterCommandSpec
        {

            mode = SlantedShutterMode.Close,

            slantPixels = 140f,
            openGapHeight = 460f,
            finalGapHeight = 0f,

            centerBandHeight = 280f,
            centerStartAlpha = 0.25f,
            centerEndAlpha = 1f,

            color = Color.black,

            duration = duration,
            ease = Ease.OutCubic,

            wait = true,
            killTween = true,
            disableWhenOpen = true,
            blockRaycastWhileClosed = false,
        };

        Collect(spec);
    }

    private void EnqueueSlantedShutterOpenSpec(
        string targetName,
        float duration = 0.32f)
    {

        var spec = new SlantedShutterCommandSpec
        {
            mode = SlantedShutterMode.Open,

            slantPixels = 140f,
            openGapHeight = 460f,
            finalGapHeight = 0f,

            centerBandHeight = 280f,
            centerStartAlpha = 0.25f,
            centerEndAlpha = 1f,

            color = Color.black,

            duration = duration,
            ease = Ease.InCubic,

            wait = true,
            killTween = true,
            disableWhenOpen = true,
            blockRaycastWhileClosed = false,
        };

        Collect(spec);
    }

    private void EnqueueVerticalStripCoverSpec(
        string targetName,
        float duration = 0f)
    {

        var spec = new VerticalStripWipeCommandSpec
        {

            mode = VerticalStripWipeMode.Cover,
            order = VerticalStripWipeOrder.LeftToRight,

            stripCount = 20,
            stripDelay = 0.02f,
            stripFillDuration = 0.08f,

            color = Color.black,

            duration = duration,
            ease = Ease.Linear,

            wait = true,
            killTween = true,
            disableWhenClear = true,
        };

        Collect(spec);
    }

    private void EnqueueVerticalStripClearSpec(
        string targetName,
        float duration = 0f)
    {

        var spec = new VerticalStripWipeCommandSpec
        {

            mode = VerticalStripWipeMode.Clear,
            order = VerticalStripWipeOrder.RightToLeft,

            stripCount = 20,
            stripDelay = 0.02f,
            stripFillDuration = 0.08f,

            color = Color.black,

            duration = duration,
            ease = Ease.Linear,

            wait = true,
            killTween = true,
            disableWhenClear = true,
        };

        Collect(spec);
    }

    private void EnqueueSetupPresentationViewSpec()
    {
        var spec = new SetupPresentationViewCommandSpec();

        Collect(spec);
    }

    private void EnqueueHideDialogueBoxSpec()
    {
        var spec = new HideDialogueBoxCommandSpec
        {
            hideAll = true,
            targetKind = DialogueBoxKind.Speaker,
            duration = 0.18f,
            wait = false
        };

        Collect(spec);
    }
    
    private void EnqueueSlantedMaskCutInSpec(
        string targetName,
        float duration = 0.65f)
    {

        var spec = new SlantedMaskSlideInCommandSpec
        {
            fromOffset = new Vector2(-2200f, 0f),
            toOffset = new Vector2(-770f, 0f),

            slantToRight = false,
            flipVertical = true,

            duration = duration,
            ease = Ease.OutCubic,

            overshootPixels = 72f,
            overshootStart = 0.72f,

            wait = false,
            killTween = true,
        };

        Collect(spec);
    }

    private void EnqueueSlantedMaskCutOutSpec(
        string targetName,
        float duration = 0.45f)
    {

        var spec = new SlantedMaskSlideOutCommandSpec
        {
            fromOffset = new Vector2(-770f, 0f),
            toOffset = new Vector2(-2200f, 0f),

            slantToRight = false,
            flipVertical = true,

            duration = duration,
            ease = Ease.InCubic,

            pullPixels = 0f,
            pullEnd = 0.28f,

            wait = false,
            killTween = true,
        };

        Collect(spec);
    }

    private bool TryParsePresentationResponseProfile(string raw, out PresentationResponseProfile profile)
    {
        profile = null;

        if (string.IsNullOrWhiteSpace(raw))
            return false;

        string s = raw.Trim().ToLowerInvariant();

        switch (s)
        {
            case "background":
            case "bg":
                profile = PresentationResponseProfile.Background;
                return true;

            case "prop":
                profile = PresentationResponseProfile.Prop;
                return true;

            case "characterslot":
            case "character_slot":
            case "char":
            case "slot":
                profile = PresentationResponseProfile.CharacterSlot;
                return true;

            case "foreground":
            case "fg":
                profile = PresentationResponseProfile.Foreground;
                return true;
        }

        return false;
    }
}