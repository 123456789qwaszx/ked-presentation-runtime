using DG.Tweening;
using UnityEngine;

public sealed partial class YarnCommandBridge : InlineEventMarkupHandler.IInlineEmojiHost
{
    public void PlayEmojiCue(string cue)
    {
        string characterKey = _vnRuntimeStateProvider != null
            ? _vnRuntimeStateProvider.CurrentCharacterKey
            : "";
        
        if (string.IsNullOrWhiteSpace(characterKey))
            return;
        
        if (string.IsNullOrWhiteSpace(cue))
        {
            EnqueueEmojiHideSpec(characterKey);
            return;
        }
        
        EnqueueEmojiPopSpec(characterKey, cue);
    }

    // ----------------------------------------------------------------------
    // Public Yarn command handlers
    // ----------------------------------------------------------------------

    // <<emoji igna 19>>
    private void EnqueueEmojiPopSpec(string roleKey, string emojiKey)
    {
        Collect(BuildSetEmojiSpec(
            roleKey,
            emojiKey,
            initialReveal: 0f));

        // Reveal과 scale overshoot를 동시에 시작한다.
        Collect(BuildRevealEmojiSpec(
            roleKey,
            fromReveal: 0f,
            toReveal: 1f,
            duration: 0.8f,
            ease: Ease.OutCubic,
            wait: false));

        // 한 번 부드럽게 커졌다가 돌아오는 기본 Pop.
        Collect(BuildEmojiScaleToSpec(
            roleKey,
            xyScale: 1.18f,
            duration: 0.28f,
            ease: Ease.OutBack,
            wait: false));

        Collect(BuildEmojiScaleToSpec(
            roleKey,
            xyScale: 1.0f,
            duration: 0.52f,
            ease: Ease.OutCubic,
            wait: false));
        
        Collect(new WaitCommandSpec
        {
            duration = 0.5f
        });
        
        Collect(new FadeOutCommandSpecCharR
        {
            slotKey = roleKey,
            targetMask = CharRigRootMask.CharacterEmoji_Root,
            duration = 0.4f
        });
    }

    // <<emoji_drop igna 19>>
    private void EnqueueEmojiDropSpec(string roleKey, string emojiKey)
    {
        Collect(BuildSetEmojiSpec(
            roleKey,
            emojiKey,
            initialReveal: 0f));

        Collect(BuildRevealEmojiSpec(
            roleKey,
            fromReveal: 0f,
            toReveal: 1f,
            duration: 0.8f,
            ease: Ease.OutCubic,
            wait: false));

        // 위에서 툭 내려오며 정착.
        Collect(BuildEmojiSlideInSpec(
            roleKey,
            direction: CharRigDirection.Up,
            distance: 90f,
            duration: 0.8f,
            ease: Ease.OutBack,
            punch: 0f,
            wait: true));
    }

    // <<emoji_shock igna 19>>
    private void EnqueueEmojiShockSpec(string roleKey, string emojiKey)
    {
        Collect(BuildSetEmojiSpec(
            roleKey,
            emojiKey,
            initialReveal: 0f));

        Collect(BuildRevealEmojiSpec(
            roleKey,
            fromReveal: 0f,
            toReveal: 1f,
            duration: 0.7f,
            ease: Ease.OutCubic,
            wait: false));

        // 위치 jolt와 scale overshoot를 동시에 시작한다.
        Collect(BuildEmojiJoltSpec(
            roleKey,
            strength: 20f,
            duration: 0.75f,
            wait: false));

        Collect(BuildEmojiScaleToSpec(
            roleKey,
            xyScale: 1.28f,
            duration: 0.24f,
            ease: Ease.OutBack,
            wait: true));

        Collect(BuildEmojiScaleToSpec(
            roleKey,
            xyScale: 1.0f,
            duration: 0.46f,
            ease: Ease.OutCubic,
            wait: true));
    }

    // <<emoji_hop igna 19>>
    private void EnqueueEmojiHopSpec(string roleKey, string emojiKey)
    {
        Collect(BuildSetEmojiSpec(
            roleKey,
            emojiKey,
            initialReveal: 0f));

        Collect(BuildRevealEmojiSpec(
            roleKey,
            fromReveal: 0f,
            toReveal: 1f,
            duration: 0.8f,
            ease: Ease.OutCubic,
            wait: false));

        // 작은 감정표현이 머리 위에서 통 하고 한 번 뜨는 느낌.
        Collect(BuildEmojiHopSpec(
            roleKey,
            height: 54f,
            duration: 0.8f,
            wait: true));
    }

    // <<emoji_sway igna 19>>
    private void EnqueueEmojiSwaySpec(string roleKey, string emojiKey)
    {
        Collect(BuildSetEmojiSpec(
            roleKey,
            emojiKey,
            initialReveal: 0f));

        Collect(BuildRevealEmojiSpec(
            roleKey,
            fromReveal: 0f,
            toReveal: 1f,
            duration: 0.8f,
            ease: Ease.OutCubic,
            wait: false));

        // 당황/고민/물음표 계열에 어울리는 기울기 흔들림.
        Collect(BuildEmojiSwaySpec(
            roleKey,
            strength: 9f,
            duration: 0.8f,
            cycles: 1,
            wait: true));
    }

    // <<emoji_tremble igna 19>>
    private void EnqueueEmojiTrembleSpec(string roleKey, string emojiKey)
    {
        Collect(BuildSetEmojiSpec(
            roleKey,
            emojiKey,
            initialReveal: 0f));

        Collect(BuildRevealEmojiSpec(
            roleKey,
            fromReveal: 0f,
            toReveal: 1f,
            duration: 0.75f,
            ease: Ease.OutCubic,
            wait: false));

        // 공포/분노/불안 계열의 달달 떨림.
        Collect(BuildEmojiTrembleSpec(
            roleKey,
            strength: 6f,
            duration: 0.8f,
            frequency: 20f,
            wait: true));
    }

    // <<emoji_set igna 19>>
    private void EnqueueEmojiSetSpec(string roleKey, string emojiKey)
    {
        Collect(BuildSetEmojiSpec(
            roleKey,
            emojiKey,
            initialReveal: 1f));
    }

    // <<emoji_hide igna>>
    private void EnqueueEmojiHideSpec(string roleKey)
    {
        Collect(BuildSetEmojiSpec(
            roleKey,
            "",
            initialReveal: 0f));
    }

    // ----------------------------------------------------------------------
    // Spec builders - EmojiSlot00 only
    // ----------------------------------------------------------------------

    private SetCharacterEmojiCommandSpecCharR BuildSetEmojiSpec(
        string roleKey,
        string emojiKey,
        float initialReveal)
    {
        return new SetCharacterEmojiCommandSpecCharR
        {
            slotKey = roleKey,
            emojiKey = emojiKey,

            rootTarget = CharacterRigTarget.CharacterEmojiSlot00_Root,
            castTarget = CharacterRigTarget.CharacterEmojiSlot00_CastTransform,
            imageTarget = CharacterRigTarget.EmojiSlot00_Image,

            alpha = 1f,
            initialReveal = initialReveal,

            wait = false,
            killTween = true
        };
    }

    private RevealCharacterEmojiCommandSpecCharR BuildRevealEmojiSpec(
        string roleKey,
        float fromReveal,
        float toReveal,
        float duration,
        Ease ease,
        bool wait)
    {
        return new RevealCharacterEmojiCommandSpecCharR
        {
            slotKey = roleKey,
            imageTarget = CharacterRigTarget.EmojiSlot00_Image,

            usePresetReveal = false,

            fromReveal = fromReveal,
            toReveal = toReveal,
            duration = duration,
            ease = ease,

            wait = wait,
            killTween = true
        };
    }

    private ScaleToCommandSpecCharR BuildEmojiScaleToSpec(
        string roleKey,
        float xyScale,
        float duration,
        Ease ease,
        bool wait)
    {
        return new ScaleToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Scale,

            toScale = new Vector2(xyScale, xyScale),
            duration = duration,
            ease = ease,

            wait = wait,
        };
    }

    private SlideInCommandSpecCharR BuildEmojiSlideInSpec(
        string roleKey,
        CharRigDirection direction,
        float distance,
        float duration,
        Ease ease,
        float punch,
        bool wait)
    {
        return new SlideInCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Track_Move,

            direction = direction,
            distance = distance,
            duration = duration,
            ease = ease,
            punch = punch,

            wait = wait,
        };
    }

    private HopCommandSpecCharR BuildEmojiHopSpec(
        string roleKey,
        float height,
        float duration,
        bool wait)
    {
        return new HopCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Track_Y,

            duration = duration,
            ease = Ease.OutCubic,

            hopCount = 1,
            height = height,
            airWidth = 0.85f,
            lastArcHeight = -1f,
            lastAirWidth = -1f,

            wait = wait
        };
    }

    private JoltCommandSpec BuildEmojiJoltSpec(
        string roleKey,
        float strength,
        float duration,
        bool wait)
    {
        return new JoltCommandSpec
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Track_Move,

            strength = strength,
            direction = CharRigDirection.Right,
            duration = duration,

            taps = 2,
            damping = 6f,
            anticipation = 0f,

            wait = wait,
        };
    }

    private SwayCommandSpecCharR BuildEmojiSwaySpec(
        string roleKey,
        float strength,
        float duration,
        int cycles,
        bool wait)
    {
        return new SwayCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Rotation,

            strength = strength,
            duration = duration,
            cycles = cycles,

            damping = 2.1f,
            speed = 1.0f,
            finalOvershoot = 0.2f,

            anticipation = 0f,
            startPositive = true,

            wait = wait
        };
    }

    private TrembleCommandSpecCharR BuildEmojiTrembleSpec(
        string roleKey,
        float strength,
        float duration,
        float frequency,
        bool wait)
    {
        return new TrembleCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Track_Move,

            strength = strength,
            direction = CharRigDirection.Right,
            duration = duration,
            frequency = frequency,

            crossAxisRatio = 0.35f,
            noiseRatio = 0.2f,

            usePulse = false,
            pulseInterval = 1.0f,
            pulseDuration = 0.16f,

            blendIn = 0.04f,
            blendOut = 0.08f,

            wait = wait
        };
    }
}