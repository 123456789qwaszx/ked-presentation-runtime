using DG.Tweening;
using Ked.Presentation.Core;
using UnityEngine;

public sealed partial class StageCommandParser
{
    private void EnqueueSetupCharRigSpec(string slotKey, string stageKey, string layerKey)
        => Emit(new SetupCharRigCommandSpec
        {
            roleKey = slotKey,
            rigPrefab = _charRigPrefab,

            stage = StageKeyParser.ParseStage(stageKey),
            layerDepth = StageKeyParser.ParseLayerDepth(layerKey)
        });

    private void EnqueueCastCharacterSpec(string slotKey, string characterKey, string variantKey, string emotionKey)
    {
        var castSpec = new CastCharacterCommandSpec
        {
            slotKey = slotKey,
            characterKey = characterKey
        };

        Emit(castSpec);

        EnqueueSetPortraitPoseSpec(slotKey, variantKey);
        EnqueueSetPortraitFaceSpec(slotKey, emotionKey);
        EnqueueSetAnchorSpecs(slotKey);
    }

    private void EnqueueSetAnchorSpecs(string slotKey, bool resetSlotPos = true,
        bool resetCharPos = true)
        => Emit(new SetAnchorCommandSpecCharR
        {
            slotKey = slotKey,
            node = CharacterRigNode.CharacterPortrait_VisualOffset,
            resetSlotPos = resetSlotPos,
            resetCharacterPos = resetCharPos
        });

    private void EnqueueMirrorSetSpec(string roleKey, string directionToken)
        => Emit(new MirrorCharacterCommandSpecCharR
        {
            slotKey = roleKey,
            mode = CharacterMirrorModeParser.Parse(directionToken),
            node = CharacterRigNode.CharacterPortrait_ActingScale_X,
            duration = 0f,
        });

    private void EnqueueSetPortraitPoseSpec(string slotKey, string variantKey)
        => Emit(new SetPortraitPoseCommandSpecCharR
        {
            slotKey = slotKey,
            variantKey = variantKey,
        });

    private void EnqueueSetPortraitFaceSpec(string slotKey, string emotionKey)
        => Emit(new SetPortraitSpriteCommandSpecCharR
        {
            slotKey = slotKey,
            portrait = new PortraitIdentity
            {
                character = "",
                variant = "",
                emotion = emotionKey
            }
        });

    private void EnqueueSetAnchorOffsetSpecs(string slotKey, string xToken, string yToken, string durationToken, string easeToken)
    {
        Ease ease = YarnEaseParser.Parse(easeToken);

        Emit(new MoveByCommandSpecCharR
        {
            slotKey = slotKey,
            node = CharacterRigNode.CharSlot_Track,
            useAbsolutePosition = false,
            delta = new Vector2(ParseSignedUnit(xToken), ParseSignedUnit(yToken)),
            duration = YarnDurationParser.Parse(durationToken),
            ease = ease
        });
    }

    // motion(slot, name, mode) — motions.json의 클립을 그대로 싣는다. 길이는 클립이 안다.
    // mode(relative|absolute)는 클립이 아니라 이 커맨드가 정한다 — 생략값은 표에 있다(relative).
    // 모르는 이름·낱말은 소리 내고 아무것도 내지 않는다 — 대신 재생할 기본 모션이 없다(이징의 OutCubic과 다른 점).
    private void EnqueueMotionSpec(string roleKey, string motionName, string modeToken)
    {
        if (!_motions.TryGet(motionName, out MotionClip clip))
        {
            Debug.LogError(
                $"[StageCommandParser] Unknown motion '{motionName}' — {MotionLibrary.BundleFileName}에 없다. 이 커맨드는 재생하지 않는다.");
            return;
        }

        if (!MotionModeToken.TryParse(modeToken, out MotionChannelMode mode))
        {
            Debug.LogError(
                $"[StageCommandParser] motion '{motionName}': mode는 relative|absolute 여야 한다 ('{modeToken}'). 이 커맨드는 재생하지 않는다.");
            return;
        }

        Emit(new MotionCommandSpecCharR { slotKey = roleKey, clip = clip, mode = mode });
    }

    // ── 회전 (3a75d4f6에서 지운 것을 되살린 자리) ──────────────────
    // 표적은 CharSlot_SwayPivot이다 — 코어 리듀서(ApplyRotateBy/ApplyRotateReset)가
    // 접는 노드와 같아야 "재생 = 정지 프레임"이 유지된다.

    private void EnqueueRotateBySpec(string roleKey, float degree, string durationToken, string easeToken)
    {
        Ease ease = YarnEaseParser.Parse(easeToken);

        Emit(new RotateToCommandSpecCharR
        {
            slotKey = roleKey,
            node = CharacterRigNode.CharSlot_SwayPivot,

            toEuler = new Vector3(0f, 0f, degree),
            relativeToCurrent = true,

            duration = YarnDurationParser.Parse(durationToken),
            ease = ease
        });
    }

    private void EnqueueRotateResetSpec(string roleKey, string durationToken, string easeToken)
    {
        Ease ease = YarnEaseParser.Parse(easeToken);

        Emit(new RotateToCommandSpecCharR
        {
            slotKey = roleKey,
            node = CharacterRigNode.CharSlot_SwayPivot,

            toEuler = Vector3.zero,

            duration = YarnDurationParser.Parse(durationToken),
            ease = ease
        });
    }

    // 초상 축의 절대 회전. 종전 char_rotate_to는 다단 연출(PivotRotateTo)이었고
    // 그 커맨드는 되살리지 않았다 — 여기서는 RotateTo의 절대 모드로 선다.
    private void EnqueuePortraitRotateToSpec(string roleKey, float degree, string durationToken, string easeToken)
    {
        Ease ease = YarnEaseParser.Parse(easeToken);

        Emit(new RotateToCommandSpecCharR
        {
            slotKey = roleKey,
            node = CharacterRigNode.CharacterPortrait_SwayPivot,

            toEuler = new Vector3(0f, 0f, degree),
            relativeToCurrent = false,

            duration = YarnDurationParser.Parse(durationToken),
            ease = ease
        });
    }

    private void EnqueueSizeBySpec(string roleKey, float multiplier, string durationToken, string easeToken)
    {
        Ease ease = YarnEaseParser.Parse(easeToken);

        Emit(new ScaleToCommandSpecCharR
        {
            slotKey = roleKey,
            node = CharacterRigNode.CharSlot_Scale,

            toScale = new Vector2(multiplier, multiplier),
            relativeToCurrent = true,

            duration = YarnDurationParser.Parse(durationToken),
            ease = ease
        });
    }

    private void EnqueueSetPlaceResetSpecs(string slotKey, string durationToken)
    {
        float duration = YarnDurationParser.Parse(durationToken);

        var slotOffsetSpec = new MoveByCommandSpecCharR
        {
            slotKey = slotKey,
            node = CharacterRigNode.CharSlot_Track,
            useAbsolutePosition = true,
            delta = new Vector2(0, 0),
            duration = duration
        };

        var spec2 = new MoveByCommandSpecCharR
        {
            slotKey = slotKey,
            node = CharacterRigNode.CharSlot_Track_Focus,
            useAbsolutePosition = true,
            delta = new Vector2(0, 0),
            duration = duration
        };

        Emit(slotOffsetSpec);
        Emit(spec2);
    }

    private void EnqueueSizeResetSpec(string roleKey, string durationToken, string easeToken)
    {
        Ease ease = YarnEaseParser.Parse(easeToken);

        Emit(new ScaleToCommandSpecCharR
        {
            slotKey = roleKey,
            node = CharacterRigNode.CharSlot_Scale,

            toScale = new Vector2(1, 1),

            duration = YarnDurationParser.Parse(durationToken),
            ease = ease
        });
    }

    private void EnqueueCharacterSiblingFrontSpec(string roleKey)
        => Emit(new SetCharacterSiblingOrderCommandSpecCharR
        {
            slotKey = roleKey,
            mode = CharacterRigSiblingOrderMode.Front
        });

    private void EnqueueCharacterSiblingBackSpec(string roleKey)
        => Emit(new SetCharacterSiblingOrderCommandSpecCharR
        {
            slotKey = roleKey,
            mode = CharacterRigSiblingOrderMode.Back
        });
}
