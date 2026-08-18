using UnityEngine;

public sealed partial class YarnCommandBridge
{
    private void EnqueueFadeInDslSpec(string roleKey, string durationToken = "14fr")
        => Collect(new FadeInCommandSpecCharR
        {
            target = CharacterRigTarget.CharacterPortraitSprite_Root,
            slotKey = roleKey,
            duration = YarnDurationParser.Parse(durationToken)
        });

    private void EnqueueFadeOutDslSpec(string roleKey, string durationToken = "14fr")
        => Collect(new FadeOutCommandSpecCharR
        {
            target = CharacterRigTarget.CharacterPortraitSprite_Root,
            slotKey = roleKey,
            duration = YarnDurationParser.Parse(durationToken)
        });

    private void EnqueueSetPortraitCrossfadeDslSpec(
        string roleKey,
        string character,
        string emotionKey,
        string durationToken = "10fr")
        => Collect(new SetPortraitCrossfadeCommandSpecCharR
        {
            slotKey = roleKey,
            portrait = new PortraitIdentity { character = character, emotion = emotionKey },
            duration = YarnDurationParser.Parse(durationToken)
        });

    // 표정 교체. Spine 이관 대상이지만 저작 콘텐츠에서 가장 많이 쓰이는 커맨드라
    // 마이그레이션이 끝날 때까지 스프라이트 경로를 유지한다.
    private void EnqueueSetEmotionPortraitWipeDslSpec(
        string targetKey,
        string emotion,
        string durationToken = "10fr")
        => Collect(new SetEmotionPortraitWipeCommandSpec
        {
            slotKey = targetKey,
            portrait = new PortraitIdentity { emotion = emotion },
            duration = YarnDurationParser.Parse(durationToken)
        });

    // 등퇴장. 연기가 아니라 이동이다 — 정지 프레임에 최종 위치가 남으므로
    // 무대 축(CharSlot_Track)에 걸리고 코어가 모델링한다.
    private void EnqueueSlideInDslSpec(
        string roleKey,
        string direction = "left",
        string distanceToken = "12u",
        string durationToken = "10fr")
        => Collect(new SlideInCommandSpecCharR
        {
            slotKey = roleKey,
            direction = CharRigDirectionParser.ParseSlideDirection(direction),
            distance = YarnUnitParser.Parse(distanceToken),
            duration = YarnDurationParser.Parse(durationToken)
        });

    private void EnqueueSlideOutDslSpec(
        string roleKey,
        string direction = "right",
        string distanceToken = "12u",
        string durationToken = "10fr")
        => Collect(new SlideOutCommandSpecCharR
        {
            slotKey = roleKey,
            to = CharRigDirectionParser.ParseSlideDirection(direction),
            distance = YarnUnitParser.Parse(distanceToken),
            duration = YarnDurationParser.Parse(durationToken)
        });

    private void EnqueueMoveByUnitCharSpec(
        string roleKey,
        string xToken,
        string yToken,
        string durationToken = "10fr")
        => Collect(new MoveByCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterPortrait_Track,
            delta = new Vector2(YarnUnitParser.Parse(xToken), YarnUnitParser.Parse(yToken)),
            duration = YarnDurationParser.Parse(durationToken),
        });

    private void EnqueueScaleToDslSpec(
        string roleKey,
        float xyValue,
        string durationToken = "10fr")
        => Collect(new ScaleToCommandSpecCharR
        {
            slotKey = roleKey,
            target = CharacterRigTarget.CharacterPortrait_ActingScale,
            duration = YarnDurationParser.Parse(durationToken),
            toScale = new Vector2(xyValue, xyValue)
        });
}