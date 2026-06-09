using DG.Tweening;
using UnityEngine;

#region DoTween 효과 정리 주석
// Ease.InQuad 
// 초반: 천천히 내려옴
// 후반: 점점 빨라짐
// 착지: 탁 떨어지는 느낌
// 자주 쓸 만한 fallEase 감각
// Ease.Linear
//     = 일정한 속도로 내려옴.
//     = 기계적이고 단순함.
//
//         Ease.InSine
//     = 위에서 살짝 머물다가 자연스럽게 내려옴.
//     = 부드러운 낙하.
//
//         Ease.InQuad
//     = 위에서 천천히 내려오다가 후반에 빨라짐.
//     = 지금 기본값. "톡 튀고 착지"에 적당함.
//
//         Ease.InCubic
//     = InQuad보다 더 오래 위에 있다가 더 빠르게 떨어짐.
//     = 더 만화적인 "탁!" 느낌.
//
//         Ease.InQuart / Ease.InQuint
//     = 거의 공중에 멈춘 듯하다가 급하게 떨어짐.
//     = 과장된 코믹/카툰 느낌.
//
//         Ease.OutQuad
//     = 처음에 빨리 내려오고, 바닥에 가까워질수록 천천히 착지.
//     = 부드럽게 내려앉는 느낌.
//
//         Ease.OutSine
//     = 아주 부드러운 착지.
//     = 말랑하고 가벼운 느낌.
//
//         Ease.InOutSine
//     = 초반/후반 모두 부드럽고 중간이 빠름.
//     = 자연스럽지만 "탁" 느낌은 약함.

//<<walk_in_place Mercurio 99 2.2 28 0.9>> 터벅터벅
//<<walk_in_place Mercurio 99 4.0 21 0.7>> 종종걸음
#endregion
public sealed partial class YarnCommandBridge
{
    private void EnqueueBounceInPlaceSpec(
        string roleKey,
        float duration = 99f,
        float bouncesPerSecond = 2.5f,
        float height = 32f,
        float riseRatio = 0.18f)
    {
        var spec = new BounceInPlaceCommandSpecCharR
        {
            slotKey = roleKey.Trim(),
            duration = duration,
            bouncesPerSecond = bouncesPerSecond,
            height = height,
            riseRatio = Mathf.Clamp(riseRatio, 0.05f, 0.8f),
            sideSway = 0.2f,
            riseEase = Ease.InQuart,
            fallEase = Ease.InOutSine,
            blendIn = 0.04f,
            blendOut = 0.08f,
            wait = false,
            killTween = true
        };

        Collect(spec);
    }
    
    private void EnqueueBreathInPlaceSpec(
        string roleKey,
        float duration = 2.4f,
        float height = 8f,
        float breathsPerSecond = 0.35f)
    {
        var spec = new BreathInPlaceCommandSpecCharR
        {
            slotKey = roleKey.Trim(),
            target = CharacterRigTarget.CharacterPortrait_Track_Y,
            duration = duration,
            breathsPerSecond = breathsPerSecond,
            height = height,
            sideSway = 0f,
            useScalePulse = false,
            scaleAmount = 0.015f,
            ease = Ease.InOutSine,
            phaseOffset = 0f,
            blendIn = 0.25f,
            blendOut = 0.25f,
            wait = false,
            killTween = true
        };

        Collect(spec);
    }
    
    private void EnqueueTremblePulseSpec(
        string roleKey,
        float duration = 99.0f,
        float strength = 5f,
        float frequency = 28f,
        float pulseInterval = 1.0f,
        float pulseDuration = 0.16f,
        string direction = "right")
    {
        CharRigDirection dir = CharRigDirectionParser.ParseSlideDirection(direction, CharRigDirection.Right);

        var spec = new TrembleCommandSpecCharR
        {
            slotKey = roleKey.Trim(),
            target = CharacterRigTarget.CharacterPortrait_Shake,
            direction = dir,
            duration = duration,
            strength = strength,
            frequency = frequency,
            crossAxisRatio = 0.25f,
            noiseRatio = 0.25f,
            blendIn = 0.025f,
            blendOut = 0.06f,
            usePulse = true,
            pulseInterval = pulseInterval,
            pulseDuration = pulseDuration,
            wait = false
        };

        Collect(spec);
    }

    private void EnqueueWalkInPlaceSpec(
        string roleKey,
        float duration = 99f,
        float stepsPerSecond = 1.9f,
        float arcHeight = 18f,
        float airWidth = 0.95f)
    {
        var spec = new WalkInPlaceCommandSpecCharR
        {
            slotKey = roleKey.Trim(),
            duration = duration,
            stepsPerSecond = stepsPerSecond,
            arcHeight = arcHeight,
            airWidth = Mathf.Clamp(airWidth, 0.05f, 1f),
            sideSway = 0.3f,
            blendIn = 0.08f,
            blendOut = 0.08f,
            wait = false
        };

        Collect(spec);
    }
}