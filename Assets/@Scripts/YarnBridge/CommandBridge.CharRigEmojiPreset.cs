using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    private void BindCharRigEmote(DialogueRunner runner)
    {
        runner.AddCommandHandler<string, string>(
            "emoji", EnqueueEmojiPopSpec);

        runner.AddCommandHandler<string, string>(
            "emoji_drop", EnqueueEmojiDropSpec);

        runner.AddCommandHandler<string, string>(
            "emoji_shock", EnqueueEmojiShockSpec);

        runner.AddCommandHandler<string, string>(
            "emoji_hop", EnqueueEmojiHopSpec);

        runner.AddCommandHandler<string, string>(
            "emoji_sway", EnqueueEmojiSwaySpec);

        runner.AddCommandHandler<string, string>(
            "emoji_tremble", EnqueueEmojiTrembleSpec);

        runner.AddCommandHandler<string, string>(
            "emoji_spring", EnqueueEmojiSpringSpec);

        runner.AddCommandHandler<string, string>(
            "emoji_spit", EnqueueEmojiSpitSpec);

        runner.AddCommandHandler<string, string>(
            "emoji_pinwheel", EnqueueEmojiPinwheelSpec);
        
        runner.AddCommandHandler<string, string>(
            "emoji_heartfly", EnqueueEmojiHeartFlySpec);

        runner.AddCommandHandler<string, string>(
            "emoji_chatter", EnqueueEmojiChatterSpec);
        
        runner.AddCommandHandler<string, string>(
            "emoji_ellipsis", EnqueueEmojiEllipsisSpec);
    }

    private void EnqueueEmojiPopSpec(string roleKey, string emojiKey)
    {
        var spec0InitEmojiSpec = new InitCharacterEmojiCommandSpecCharR
        {
            slotKey = roleKey,
            emojiKey = emojiKey,
            rootTarget = CharacterRigTarget.EmojiSlot00_Root,
            castTarget = CharacterRigTarget.EmojiSlot00_VisualOffset,
            imageTarget = CharacterRigTarget.EmojiSlot00_Image,
            initialReveal = 0f,
            resetMotionAxes = true
        };

        var spec1PopRevealSpec = new RevealCharacterEmojiCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Image,
            fromReveal = 0f,
            toReveal = 1f,
            duration = 0.8f,
            ease = Ease.OutCubic
        };

        var spec2PopScaleUpSpec = new ScaleToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Scale,
            toScale = new Vector2(1.18f, 1.18f),
            duration = 0.28f,
            ease = Ease.OutBack,
            wait = true
        };

        var spec3PopScaleBackSpec = new ScaleToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Scale,
            toScale = Vector2.one,
            duration = 0.52f,
            ease = Ease.OutCubic,
            wait = true
        };

        var spec4HoldSpec = new WaitCommandSpec
        {
            duration = 0.5f
        };

        var spec5AutoFadeOutSpec = new FadeOutCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Root,
            duration = 0.4f
        };

        Collect(spec0InitEmojiSpec);
        Collect(spec1PopRevealSpec);
        Collect(spec2PopScaleUpSpec);
        Collect(spec3PopScaleBackSpec);
        Collect(spec4HoldSpec);
        Collect(spec5AutoFadeOutSpec);
    }

    private void EnqueueEmojiDropSpec(string roleKey, string emojiKey)
    {
        var spec0InitEmojiSpec = new InitCharacterEmojiCommandSpecCharR
        {
            slotKey = roleKey,
            emojiKey = emojiKey,
            rootTarget = CharacterRigTarget.EmojiSlot00_Root,
            castTarget = CharacterRigTarget.EmojiSlot00_VisualOffset,
            imageTarget = CharacterRigTarget.EmojiSlot00_Image,
            initialReveal = 0f,
            resetMotionAxes = true
        };

        var spec1RevealEmojiSpec = new RevealCharacterEmojiCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Image,
            fromReveal = 0f,
            toReveal = 1f,
            duration = 0.8f,
            ease = Ease.OutCubic,
            wait = false
        };

        var spec2DropSlideInSpec = new EmojiSlideInCommandSpecCharR
        {
            slotKey = roleKey,
            emojiKey = emojiKey,
            target = CharacterRigTarget.EmojiSlot00_Track_Move,
            direction = CharRigDirection.Up,
            distance = 90f,
            duration = 0.8f,
            ease = Ease.OutBack,
            punch = 0f,
            wait = true
        };

        Collect(spec0InitEmojiSpec);
        Collect(spec1RevealEmojiSpec);
        Collect(spec2DropSlideInSpec);
    }

    private void EnqueueEmojiShockSpec(string roleKey, string emojiKey)
    {
        var spec0InitEmojiSpec = new InitCharacterEmojiCommandSpecCharR
        {
            slotKey = roleKey,
            emojiKey = emojiKey,
            rootTarget = CharacterRigTarget.EmojiSlot00_Root,
            castTarget = CharacterRigTarget.EmojiSlot00_VisualOffset,
            imageTarget = CharacterRigTarget.EmojiSlot00_Image,
            initialReveal = 0f,
            resetMotionAxes = true
        };

        var spec1RevealEmojiSpec = new RevealCharacterEmojiCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Image,
            fromReveal = 0f,
            toReveal = 1f,
            duration = 0.7f,
            ease = Ease.OutCubic,
            wait = false
        };

        var spec2ShockJoltSpec = new EmojiJoltCommandSpec
        {
            slotKey = roleKey,
            emojiKey = emojiKey,
            target = CharacterRigTarget.EmojiSlot00_Track_Move,
            strength = 20f,
            direction = CharRigDirection.Right,
            duration = 0.75f,
            taps = 2,
            damping = 6f,
            anticipation = 0f,
            wait = false
        };

        var spec3ShockScaleUpSpec = new ScaleToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Scale,
            toScale = new Vector2(1.28f, 1.28f),
            duration = 0.24f,
            ease = Ease.OutBack,
            wait = true
        };

        var spec4ShockScaleBackSpec = new ScaleToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Scale,
            toScale = Vector2.one,
            duration = 0.46f,
            ease = Ease.OutCubic,
            wait = true
        };

        Collect(spec0InitEmojiSpec);
        Collect(spec1RevealEmojiSpec);
        Collect(spec2ShockJoltSpec);
        Collect(spec3ShockScaleUpSpec);
        Collect(spec4ShockScaleBackSpec);
        
        EnqueueEmojiIdleHeartBeatSpec(roleKey);
    }

    private void EnqueueEmojiHopSpec(string roleKey, string emojiKey)
    {
        var spec0InitEmojiSpec = new InitCharacterEmojiCommandSpecCharR
        {
            slotKey = roleKey,
            emojiKey = emojiKey,
            rootTarget = CharacterRigTarget.EmojiSlot00_Root,
            castTarget = CharacterRigTarget.EmojiSlot00_VisualOffset,
            imageTarget = CharacterRigTarget.EmojiSlot00_Image,
            initialReveal = 0f,
            resetMotionAxes = true
        };

        var spec1RevealEmojiSpec = new RevealCharacterEmojiCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Image,
            fromReveal = 0f,
            toReveal = 1f,
            duration = 0.8f,
            ease = Ease.OutCubic,
            wait = false
        };

        var spec2HopSpec = new HopCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Track_Move_Y,
            duration = 0.8f,
            ease = Ease.OutCubic,
            hopCount = 1,
            height = 54f,
            airWidth = 0.85f,
            lastArcHeight = -1f,
            lastAirWidth = -1f,
            wait = true
        };

        Collect(spec0InitEmojiSpec);
        Collect(spec1RevealEmojiSpec);
        Collect(spec2HopSpec);
        
        EnqueueEmojiIdleHeartBeatSpec(roleKey);
    }

    private void EnqueueEmojiSwaySpec(string roleKey, string emojiKey)
    {
        var spec0InitEmojiSpec = new InitCharacterEmojiCommandSpecCharR
        {
            slotKey = roleKey,
            emojiKey = emojiKey,
            rootTarget = CharacterRigTarget.EmojiSlot00_Root,
            castTarget = CharacterRigTarget.EmojiSlot00_VisualOffset,
            imageTarget = CharacterRigTarget.EmojiSlot00_Image,
            initialReveal = 0f,
            resetMotionAxes = true
        };

        var spec1RevealEmojiSpec = new RevealCharacterEmojiCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Image,
            fromReveal = 0f,
            toReveal = 1f,
            duration = 0.8f,
            ease = Ease.OutCubic,
            wait = false
        };

        var spec2SwaySpec = new SwayCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_SwayPivot,
            strength = 9f,
            duration = 0.8f,
            cycles = 1,
            damping = 2.1f,
            speed = 1.0f,
            finalOvershoot = 0.2f,
            anticipation = 0f,
            startPositive = true,
            wait = true
        };

        Collect(spec0InitEmojiSpec);
        Collect(spec1RevealEmojiSpec);
        Collect(spec2SwaySpec);
        
        EnqueueEmojiIdleHeartBeatSpec(roleKey);
    }

    private void EnqueueEmojiTrembleSpec(string roleKey, string emojiKey)
    {
        var spec0InitEmojiSpec = new InitCharacterEmojiCommandSpecCharR
        {
            slotKey = roleKey,
            emojiKey = emojiKey,
            rootTarget = CharacterRigTarget.EmojiSlot00_Root,
            castTarget = CharacterRigTarget.EmojiSlot00_VisualOffset,
            imageTarget = CharacterRigTarget.EmojiSlot00_Image,
            initialReveal = 0f,
            resetMotionAxes = true
        };

        var spec1RevealEmojiSpec = new RevealCharacterEmojiCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Image,
            fromReveal = 0f,
            toReveal = 1f,
            duration = 0.75f,
            ease = Ease.OutCubic,
            wait = false
        };

        var spec2TrembleSpec = new EmojiTrembleCommandSpecCharR
        {
            slotKey = roleKey,
            emojiKey = emojiKey,
            target = CharacterRigTarget.EmojiSlot00_Track_Move,
            strength = 6f,
            direction = CharRigDirection.Right,
            duration = 0.8f,
            frequency = 20f,
            crossAxisRatio = 0.35f,
            noiseRatio = 0.2f,
            usePulse = false,
            pulseInterval = 1.0f,
            pulseDuration = 0.16f,
            blendIn = 0.04f,
            blendOut = 0.08f,
            wait = true
        };

        Collect(spec0InitEmojiSpec);
        Collect(spec1RevealEmojiSpec);
        Collect(spec2TrembleSpec);
        
        EnqueueEmojiIdleHeartBeatSpec(roleKey);
    }

    private void EnqueueEmojiSpringSpec(string roleKey, string emojiKey)
    {
        var spec0InitEmojiSpec = new InitCharacterEmojiCommandSpecCharR
        {
            slotKey = roleKey,
            emojiKey = emojiKey,
            rootTarget = CharacterRigTarget.EmojiSlot00_Root,
            castTarget = CharacterRigTarget.EmojiSlot00_VisualOffset,
            imageTarget = CharacterRigTarget.EmojiSlot00_Image,
            initialReveal = 1f,
            resetMotionAxes = true
        };

        // 이전보다 stretch 양을 줄여서 이모지다운 가벼운 존재감으로 조정.
        var spec1SpringStretchScaleSpec = new ScaleToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Scale,
            overrideFromScale = true,
            fromScale = new Vector2(0.001f, 0.001f),
            toScale = new Vector2(0.88f, 1.12f),
            duration = 0.16f,
            ease = Ease.OutCubic,
            wait = false
        };

        // 크기가 확 생길 때 아주 살짝만 위로 뜸.
        // 존재감이 강하지 않도록 이동량을 줄임.
        var spec2SpringLiftSpec = new EmojiMoveByCommandSpecCharR
        {
            slotKey = roleKey,
            emojiKey = emojiKey,
            target = CharacterRigTarget.EmojiSlot00_Effect,
            delta = new Vector2(1f, 6f),
            duration = 0.16f,
            ease = Ease.OutCubic,
            wait = false
        };

        // 아주 작은 비틀림만.
        var spec3SpringKickRotateSpec = new EmojiRotateToCommandSpecCharR
        {
            slotKey = roleKey,
            emojiKey = emojiKey,
            target = CharacterRigTarget.EmojiSlot00_Rotation,
            toEuler = new Vector3(0f, 0f, -4f),
            duration = 0.16f,
            wait = true
        };

        // 눌림은 유지하되 과장하지 않음.
        var spec4SpringSoftSquashScaleSpec = new ScaleToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Scale,
            toScale = new Vector2(1.08f, 0.96f),
            duration = 0.14f,
            ease = Ease.InOutSine,
            wait = false
        };

        // 원점으로 부드럽게 안착.
        // OutBack 대신 OutCubic으로 과한 튐을 줄임.
        var spec5SpringSettlePositionSpec = new EmojiMoveByCommandSpecCharR
        {
            slotKey = roleKey,
            emojiKey = emojiKey,
            target = CharacterRigTarget.EmojiSlot00_Effect,
            useAbsolutePosition = true,
            delta = Vector2.zero,
            duration = 0.24f,
            ease = Ease.OutCubic,
            wait = false
        };

        // sway 잔향도 약하게.
        var spec7SpringSwaySettleSpec = new SwayCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_SwayPivot,
            strength = 1.8f,
            duration = 0.36f,
            cycles = 1,
            damping = 5.2f,
            speed = 1.0f,
            finalOvershoot = 0.06f,
            anticipation = 0f,
            startPositive = true,
            wait = false
        };
        
        // 반대 방향 회전도 아주 작게.
        var spec8SpringCounterRotateSpec = new EmojiRotateToCommandSpecCharR
        {
            slotKey = roleKey,
            emojiKey = emojiKey,
            target = CharacterRigTarget.EmojiSlot00_Rotation,
            toEuler = new Vector3(0f, 0f, 1f),
            duration = 0.22f,
            wait = true
        };

        var spec10SpringScaleSettleSpec = new ScaleToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Scale,
            toScale = Vector2.one,
            duration = 0.26f,
            ease = Ease.OutCubic,
            wait = true
        };

        Collect(spec0InitEmojiSpec);
        Collect(spec1SpringStretchScaleSpec);
        Collect(spec2SpringLiftSpec);
        Collect(spec3SpringKickRotateSpec);
        Collect(spec4SpringSoftSquashScaleSpec);
        Collect(spec5SpringSettlePositionSpec);
        Collect(spec7SpringSwaySettleSpec);
        Collect(spec8SpringCounterRotateSpec);
        Collect(spec10SpringScaleSettleSpec);

        // 완전 좋음.
        EnqueueEmojiIdleHeartBeatSpec(roleKey);
    }

    private void EnqueueEmojiSpitSpec(string roleKey, string emojiKey)
    {
        var spec0InitEmojiSpec = new InitCharacterEmojiCommandSpecCharR
        {
            slotKey = roleKey,
            emojiKey = emojiKey,
            rootTarget = CharacterRigTarget.EmojiSlot00_Root,
            castTarget = CharacterRigTarget.EmojiSlot00_VisualOffset,
            imageTarget = CharacterRigTarget.EmojiSlot00_Image,
            initialReveal = 1f,
            resetMotionAxes = true
        };

        var spec1SpitScaleSpec = new ScaleToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Scale,
            overrideFromScale = true,
            fromScale = new Vector2(0.62f, 0.62f),
            toScale = new Vector2(0.78f, 0.78f),
            duration = 0.22f,
            ease = Ease.OutCubic,
            wait = false
        };

        // 오른쪽으로 더 천천히 튀어나감.
        var spec2SpitMoveRightSpec = new EmojiMoveByCommandSpecCharR
        {
            slotKey = roleKey,
            emojiKey = emojiKey,
            target = CharacterRigTarget.EmojiSlot00_Track_Move_X,
            delta = new Vector2(165f, 0f),
            duration = 0.88f,
            ease = Ease.OutCubic,
            wait = false
        };

        // 최종 도착점은 시작점보다 낮아야 하므로 Track_Y는 아래로 계속 drift.
        // 이 값이 최종 도착 Y 높이를 결정하므로 유지한다.
        var spec3SpitDriftDownSpec = new EmojiMoveByCommandSpecCharR
        {
            slotKey = roleKey,
            emojiKey = emojiKey,
            target = CharacterRigTarget.EmojiSlot00_Track_Move_Y,
            delta = new Vector2(0f, -56f),
            duration = 1.08f,
            ease = Ease.InCubic,
            wait = false
        };

        // 포물선의 위로 솟는 부분만 낮춘다.
        // Track_Y의 최종 -36f는 그대로라서 도착 높이는 유지된다.
        var spec4SpitArcSpec = new HopCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Effect,
            duration = 0.92f,
            ease = Ease.OutCubic,
            hopCount = 1,
            height = 24f,
            airWidth = 0.82f,
            lastArcHeight = -1f,
            lastAirWidth = -1f,
            wait = false
        };

        var spec5SpitRotateSpec = new EmojiRotateToCommandSpecCharR
        {
            slotKey = roleKey,
            emojiKey = emojiKey,
            target = CharacterRigTarget.EmojiSlot00_Rotation,
            toEuler = new Vector3(0f, 0f, -22f),
            duration = 0.98f,
            wait = false
        };

        // 기존보다 더 빨리 사라지기 시작.
        // 이동은 0.98초지만 fade는 0.32 + 0.30 = 0.62초에 완전히 끝난다.
        var spec6SpitFadeDelaySpec = new WaitCommandSpec
        {
            duration = 0.28f
        };

        var spec7SpitFadeOutSpec = new FadeOutCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Root,
            duration = 0.56f,
            wait = true
        };

        Collect(spec0InitEmojiSpec);
        Collect(spec1SpitScaleSpec);
        Collect(spec2SpitMoveRightSpec);
        Collect(spec3SpitDriftDownSpec);
        Collect(spec4SpitArcSpec);
        Collect(spec5SpitRotateSpec);
        Collect(spec6SpitFadeDelaySpec);
        Collect(spec7SpitFadeOutSpec);
    }

    private void EnqueueEmojiPinwheelSpec(string roleKey, string emojiKey)
    {
        var spec0InitEmojiSpec = new InitCharacterEmojiCommandSpecCharR
        {
            slotKey = roleKey,
            emojiKey = emojiKey,
            rootTarget = CharacterRigTarget.EmojiSlot00_Root,
            castTarget = CharacterRigTarget.EmojiSlot00_VisualOffset,
            imageTarget = CharacterRigTarget.EmojiSlot00_Image,

            // Shader reveal은 쓰지 않는다.
            // 꽃이 잘려 나오지 않고, 투명도와 scale로 부드럽게 피어나게 한다.
            initialReveal = 1f,
            resetMotionAxes = true
        };

        // Init이 root alpha를 1로 올리므로, fade-in 시작점을 만든다.
        var spec1PrepareRootAlphaSpec = new FadeOutCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Root,
            duration = 0f,
            wait = true
        };

        // 나오자마자 부드럽게 보이기 시작.
        var spec2PinwheelFadeInSpec = new FadeInCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Root,
            duration = 0.74f,
            wait = false
        };

        // 회전은 처음부터 시작한다.
        // 더 이상 중간 가속/감속을 만들지 않고, 하나의 부드러운 흐름으로 계속 돈다.
        var spec3PinwheelSpinSpec = new EmojiRotateByCommandSpecCharR
        {
            slotKey = roleKey,
            emojiKey = emojiKey,
            target = CharacterRigTarget.EmojiSlot00_Rotation,
            deltaEuler = new Vector3(0f, 0f, 600f),
            duration = 5.4f,
            ease = Ease.Linear,
            wait = false
        };

        // 초반에만 살짝 떠오른 뒤 그 자리에 멈춘다.
        // 풍선처럼 계속 올라가지 않게 duration을 짧게 잡는다.
        var spec4PinwheelRiseSpec = new EmojiMoveByCommandSpecCharR
        {
            slotKey = roleKey,
            emojiKey = emojiKey,
            target = CharacterRigTarget.EmojiSlot00_Effect,
            delta = new Vector2(-8f, 20f),
            duration = 1.05f,
            ease = Ease.OutSine,
            wait = false
        };

        // 꽃이 피기 시작하는 첫 느낌.
        // 너무 작은 상태에서 오래 버티지 않게 빠르게 열어준다.
        var spec5PinwheelBloomOpenSpec = new ScaleToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Scale,
            overrideFromScale = true,
            fromScale = new Vector2(0.18f, 0.18f),
            toScale = new Vector2(1.07f, 1.07f),
            duration = 0.42f,
            ease = Ease.OutCubic,
            wait = true
        };

        // 다시 1로 안정.
        // 튀는 pop이 아니라 꽃이 활짝 핀 뒤 안착하는 느낌.
        var spec7PinwheelBloomSettleSpec = new ScaleToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Scale,
            toScale = Vector2.one,
            duration = 0.52f,
            ease = Ease.InOutSine,
            wait = false
        };

        // 바로 사라지지 않고, 캐릭터 주변에서 기쁜 감정을 충분히 보여준다.
        // 이 시간 동안 회전은 계속 진행 중이다.
        var spec8PinwheelWarmHoldSpec = new WaitCommandSpec
        {
            duration = 2.15f
        };

        // 사라질 때 아주 살짝 작아지면서 힘이 빠지는 느낌.
        var spec9PinwheelFadeScaleSpec = new ScaleToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Scale,
            toScale = new Vector2(0.94f, 0.94f),
            duration = 0.95f,
            ease = Ease.InOutSine,
            wait = false
        };

        // 사라질 때 공기 중으로 살짝 더 떠오름.
        var spec10PinwheelFadeDriftSpec = new EmojiMoveByCommandSpecCharR
        {
            slotKey = roleKey,
            emojiKey = emojiKey,
            target = CharacterRigTarget.EmojiSlot00_Effect,
            delta = new Vector2(-2f, 6f),
            duration = 0.95f,
            ease = Ease.InOutSine,
            wait = false
        };

        // 꽃잎이 잘리는 게 아니라 투명하게 녹아드는 느낌.
        var spec11PinwheelFadeOutSpec = new FadeOutCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Root,
            duration = 0.95f,
            wait = true
        };

        Collect(spec0InitEmojiSpec);
        Collect(spec1PrepareRootAlphaSpec);
        Collect(spec2PinwheelFadeInSpec);
        Collect(spec3PinwheelSpinSpec);
        Collect(spec4PinwheelRiseSpec);
        Collect(spec5PinwheelBloomOpenSpec);
        Collect(spec7PinwheelBloomSettleSpec);
        Collect(spec8PinwheelWarmHoldSpec);
        Collect(spec9PinwheelFadeScaleSpec);
        Collect(spec10PinwheelFadeDriftSpec);
        Collect(spec11PinwheelFadeOutSpec);
    }
    
    private void EnqueueEmojiHeartFlySpec(string roleKey, string emojiKey)
    {
        var spec0InitEmojiSpec = new InitCharacterEmojiCommandSpecCharR {
            slotKey = roleKey,
            emojiKey = emojiKey,
            rootTarget = CharacterRigTarget.EmojiSlot00_Root,
            castTarget = CharacterRigTarget.EmojiSlot00_VisualOffset,
            imageTarget = CharacterRigTarget.EmojiSlot00_Image,
            initialReveal = 1f,
            resetMotionAxes = true
        };

        var spec1HeartPaperPlaneSpec = new EmojiHeartPaperPlaneCommandSpecCharR {
            slotKey = roleKey,
            emojiKey = emojiKey,
            rootTarget = CharacterRigTarget.EmojiSlot00_Root,
            moveTarget = CharacterRigTarget.EmojiSlot00_Track_Move,
            scaleTarget = CharacterRigTarget.EmojiSlot00_Scale,
            rotationTarget = CharacterRigTarget.EmojiSlot00_Rotation,

            direction = CharRigDirection.Right,
            startOffset = new Vector2(0f, 0f),
            travelDistance = 224f,
            endYOffset = 26f,
            arcHeight = 62f,
            controlForwardRatio = 0.44f,

            startScale = 0.68f,
            cruiseScale = 1.02f,
            endScale = 0.80f,

            baseTiltDegrees = -8f,
            tangentTiltWeight = 0.5f,

            fadeInPortion = 0.12f,
            fadeOutStart = 0.68f,

            duration = 1.28f,
            ease = Ease.InOutSine,
            wait = true
        };

        Collect(spec0InitEmojiSpec);
        Collect(spec1HeartPaperPlaneSpec);
    }
    private void EnqueueEmojiChatterSpec(string roleKey, string emojiKey)
    {
        var spec0InitEmojiSpec = new InitCharacterEmojiCommandSpecCharR {
            slotKey = roleKey,
            emojiKey = emojiKey,
            rootTarget = CharacterRigTarget.EmojiSlot00_Root,
            castTarget = CharacterRigTarget.EmojiSlot00_VisualOffset,
            imageTarget = CharacterRigTarget.EmojiSlot00_Image,
            initialReveal = 1f,
            resetMotionAxes = true
        };

        var spec1ChatterWiggleSpec = new EmojiChatterWiggleCommandSpecCharR {
            slotKey = roleKey,
            emojiKey = emojiKey,
            rootTarget = CharacterRigTarget.EmojiSlot00_Root,
            pivotTarget = CharacterRigTarget.EmojiSlot00_SwayPivot,
            effectTarget = CharacterRigTarget.EmojiSlot00_Effect,

            // 오른쪽 곡선의 중심을 잡는 느낌.
            pivot = new Vector2(1.08f, 0.52f),

            // 입 앞쪽에 살짝 붙여두는 보정.
            settleOffset = new Vector2(5f, 0f),
            baseTiltDegrees = 0f,

            fadeInDuration = 0.18f,

            // 귀엽고 부드럽게 2~3번만 흔들림.
            duration = 1.6f,
            cycles = 3.2f,
            amplitude = 24.8f,
            dampingPower = 0.45f,

            ease = Ease.Linear,
            wait = false
        };

        var spec3WaitSpec = new WaitCommandSpec()
        {
            duration = 1.28f,
        };
        
        var spec4FadeOutSpec = new FadeOutCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Root,
            duration = 0.65f,
        };
        
        Collect(spec0InitEmojiSpec);
        Collect(spec1ChatterWiggleSpec);
        Collect(spec3WaitSpec);
        Collect(spec4FadeOutSpec);
    }
    
    private void EnqueueEmojiEllipsisSpec(string roleKey, string emojiKey)
    {
        var spec0InitEmojiSpec = new InitCharacterEmojiCommandSpecCharR {
            slotKey = roleKey,
            emojiKey = emojiKey + "-1",
            rootTarget = CharacterRigTarget.EmojiSlot00_Root,
            castTarget = CharacterRigTarget.EmojiSlot00_VisualOffset,
            imageTarget = CharacterRigTarget.EmojiSlot00_Image,
            initialReveal = 1f,
            resetMotionAxes = true
        };

        var spec1SoftAppearSpec = new SpringAppearCommandSpecCharR {
            slotKey = roleKey,
            emojiKey = emojiKey,
            scaleTarget = CharacterRigTarget.EmojiSlot00_Scale,
            effectTarget = CharacterRigTarget.EmojiSlot00_Effect,
            rotationTarget = CharacterRigTarget.EmojiSlot00_Rotation,

            fromScale = new Vector2(0.82f, 0.82f),
            overshootAmount = 0.015f,
            liftOffset = new Vector2(0f, 0f),
            tiltDegrees = 0f,

            duration = 0.32f,
            ease = Ease.Linear,
            wait = true
        };

        var spec2FrameAnimationSpec = new AnimateCharacterEmojiFramesCommandSpecCharR {
            slotKey = roleKey,
            rootTarget = CharacterRigTarget.EmojiSlot00_Root,
            imageTarget = CharacterRigTarget.EmojiSlot00_Image,

            frameKeys = new List<string> {
                emojiKey + "-1",
                emojiKey + "-2",
                emojiKey + "-3",
                emojiKey + "-4"
            },

            frameDuration = 0.88f,
            loopUntilStepEnd = false,
            keepVisibleOnCleanup = false,
        };
        
        var spec3WaitSpec = new WaitCommandSpec()
        {
            duration = 3.68f,
        };
        
        var spec4FadeOutSpec = new FadeOutCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Root,
            duration = 0.65f,
        };

        Collect(spec0InitEmojiSpec);
        Collect(spec1SoftAppearSpec);
        Collect(spec2FrameAnimationSpec);
        Collect(spec3WaitSpec);
        Collect(spec4FadeOutSpec);
    }
    
    #region afterMotion
    // 완전 좋음.
    private void EnqueueEmojiIdleHeartBeatSpec(string roleKey)
    {
        var spec0IdleHeartBeatSpec = new EmojiIdleDoublePulseCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.EmojiSlot00_Scale,

            initialDelay = 0.9f,
            interval = 2.05f,

            firstPulseScale = new Vector2(1.046f, 1.046f),
            secondPulseScale = new Vector2(1.025f, 1.025f),

            firstUpDuration = 0.065f,
            firstDownDuration = 0.095f,
            pulseGap = 0.055f,
            secondUpDuration = 0.055f,
            secondDownDuration = 0.14f,

            upEase = Ease.OutSine,
            downEase = Ease.InOutSine
        };

        Collect(spec0IdleHeartBeatSpec);
    }

    #endregion
}