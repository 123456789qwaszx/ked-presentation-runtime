using DG.Tweening;
using Ked.Presentation.Core;
using UnityEngine;

public sealed partial class StageCommandParser
{
    private void EnqueueSetupCharRigSpec(string roleKey, string stageKey, string layerKey)
    {
        string stage = StageKeyParser.ParseStage(stageKey);

        // 촬영소는 대본의 무대가 아니다 — 거기에 세운 슬롯은 아무도 찍지 않는다. 초상은 <<portrait>>가 세운다.
        if (string.Equals(stage, PortraitStudio.StageKey, System.StringComparison.Ordinal))
        {
            Debug.LogWarning(
                $"[StageCommandParser] <<slot {roleKey} {stage}>> 촬영소({stage})에는 대본이 직접 세우지 않는다 — " +
                "대사창 초상은 <<portrait 캐릭터 [포즈] [표정]>>로 세운다. 건너뛴다.");
            return;
        }

        Emit(new SetupCharRigCommandSpec
        {
            roleKey = roleKey,
            rigPrefab = _charRigPrefab,

            stage = stage,
            layerDepth = StageKeyParser.ParseLayerDepth(layerKey)
        });
    }

    private void EnqueueCastCharacterSpec(string roleKey, string characterKey, string variantKey, string emotionKey, bool slotOnly = false)
    {
        var castSpec = new CastCharacterCommandSpec
        {
            roleKey = roleKey,
            characterKey = characterKey,
            slotOnly = slotOnly
        };

        Emit(castSpec);

        EnqueueSetPortraitPoseSpec(roleKey, variantKey);
        EnqueueSetPortraitFaceSpec(roleKey, emotionKey);
        EnqueueSetAnchorSpecs(roleKey);
    }

    private void EnqueueSetAnchorSpecs(string roleKey, bool resetSlotPos = true,
        bool resetCharPos = true)
        => Emit(new SetAnchorCommandSpecCharR
        {
            roleKey = roleKey,
            node = CharacterRigNode.CharacterPortrait_VisualOffset,
            resetSlotPos = resetSlotPos,
            resetCharacterPos = resetCharPos
        });

    private void EnqueueMirrorSetSpec(string roleKey, string directionToken)
        => Emit(new MirrorCharacterCommandSpecCharR
        {
            roleKey = roleKey,
            mode = CharacterMirrorModeParser.Parse(directionToken),
            node = CharacterRigNode.CharacterPortrait_Mirror,
            duration = 0f,
        });

    private void EnqueueSetPortraitPoseSpec(string roleKey, string variantKey)
        => Emit(new SetPortraitPoseCommandSpecCharR
        {
            roleKey = roleKey,
            variantKey = variantKey,
        });

    private void EnqueueSetPortraitFaceSpec(string roleKey, string emotionKey)
        => Emit(new SetPortraitSpriteCommandSpecCharR
        {
            roleKey = roleKey,
            portrait = new PortraitIdentity
            {
                character = "",
                variant = "",
                emotion = emotionKey
            }
        });

    private void EnqueueSetAnchorOffsetSpecs(string roleKey, string xToken, string yToken, string durationToken, string easeToken)
    {
        Ease ease = YarnEaseParser.Parse(easeToken);

        Emit(new MoveByCommandSpecCharR
        {
            roleKey = roleKey,
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

        // 캐릭터 슬롯에 트는 것은 캐릭터 리그의 모션뿐이다. 카메라 리그의 클립(화면 흔들림)은 shot_shake가 튼다.
        if (clip.Rig != MotionRules.CharacterRig)
        {
            Debug.LogError(
                $"[StageCommandParser] motion '{motionName}'은 '{clip.Rig}' 리그의 모션이다 — 캐릭터 슬롯에 틀 수 없다. 이 커맨드는 재생하지 않는다.");
            return;
        }

        if (!MotionModeToken.TryParse(modeToken, out MotionChannelMode mode))
        {
            Debug.LogError(
                $"[StageCommandParser] motion '{motionName}': mode는 relative|absolute 여야 한다 ('{modeToken}'). 이 커맨드는 재생하지 않는다.");
            return;
        }

        Emit(new MotionCommandSpecCharR { roleKey = roleKey, clip = clip, mode = mode });
    }

    // ── 회전 (3a75d4f6에서 지운 것을 되살린 자리) ──────────────────
    // 표적은 CharSlot_SwayPivot이다 — 코어 리듀서(ApplyRotateBy/ApplyRotateReset)가
    // 접는 노드와 같아야 "재생 = 정지 프레임"이 유지된다.

    private void EnqueueRotateBySpec(string roleKey, float degree, string durationToken, string easeToken)
    {
        Ease ease = YarnEaseParser.Parse(easeToken);

        Emit(new RotateToCommandSpecCharR
        {
            roleKey = roleKey,
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
            roleKey = roleKey,
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
            roleKey = roleKey,
            node = CharacterRigNode.CharacterPortrait_SwayPivot,

            toEuler = new Vector3(0f, 0f, degree),
            relativeToCurrent = false,

            duration = YarnDurationParser.Parse(durationToken),
            ease = ease
        });
    }

    // ── 슬롯의 자리 · 포커스 (docs/PLAN_rig-layers.md §4) ──────────────
    // 코어 폴드(StageReducer.Placement)가 접는 노드와 같아야 "재생 = 정지 프레임"이 유지된다.

    // left · right · up · down — 본래 자리 위의 움직임(docs/PLAN_move-axes.md §4). 상대값이라 쌓인다. 가로는 Move_X, 세로는 Move_Y라
    // 둘을 나란히 적으면 함께 돈다(대각선). 노드와 방향은 코어의 표(NodeTargets.TryGetSlotMove) 하나를 폴드와 함께 본다.
    // 거리에는 생략값이 없다 — 빠졌거나 읽지 못하면 소리 내고 아무것도 내지 않는다(폴드도 Unhandled로 남긴다). 음수는 반대 방향이다.
    private void EnqueueSlotMoveSpec(StageCommand c, string roleKey, string distanceToken, string durationToken, string easeToken)
    {
        if (!NodeTargets.TryGetSlotMove(c.Name, out string nodeId, out Vec2 direction))
        {
            Debug.LogError($"[StageCommandParser] <<{c.Name}>>은 슬롯의 움직임이 아니다 — 표가 어긋났다. 이 커맨드는 실행하지 않는다.");
            return;
        }

        if (distanceToken == null)
        {
            Debug.LogError($"[StageCommandParser] <<{c.Name}>>에 거리가 없다 — 거리는 꼭 적는다. 이 커맨드는 실행하지 않는다.");
            return;
        }

        if (!UnitToken.TryParseSignedPixels(distanceToken, UnitToken.DefaultReferenceStageWidth, out float distance))
        {
            Debug.LogError($"[StageCommandParser] <<{c.Name}>>의 거리를 읽지 못했다 ('{distanceToken}'). 이 커맨드는 실행하지 않는다.");
            return;
        }

        Emit(new MoveByCommandSpecCharR
        {
            roleKey = roleKey,
            node = nodeId == NodeTargets.SlotMoveYNodeId ? CharacterRigNode.CharSlot_Move_Y : CharacterRigNode.CharSlot_Move_X,
            useAbsolutePosition = false,
            delta = (direction * distance).ToUnity(),
            duration = YarnDurationParser.Parse(durationToken),
            ease = YarnEaseParser.Parse(easeToken)
        });
    }

    // move_reset — 쌓인 움직임(Move_X · _Y)을 0으로. 본래 자리(place의 Track)는 그대로다(docs/PLAN_move-axes.md M6).
    // 노드가 둘이라 스펙도 둘이다(한 노드에 트윈 하나). 포커스는 focus_reset이 걷는다.
    private void EnqueueMoveResetSpec(string roleKey, string durationToken)
    {
        EmitMoveReset(roleKey, CharacterRigNode.CharSlot_Move_X, durationToken);
        EmitMoveReset(roleKey, CharacterRigNode.CharSlot_Move_Y, durationToken);
    }

    private void EmitMoveReset(string roleKey, CharacterRigNode node, string durationToken)
        => Emit(new MoveByCommandSpecCharR
        {
            roleKey = roleKey,
            node = node,
            useAbsolutePosition = true,
            delta = Vector2.zero,
            duration = YarnDurationParser.Parse(durationToken)
        });

    // place — 슬롯의 본래 자리를 절대값으로. move_by(상대)와 같은 노드다.
    private void EnqueuePlaceSpec(string roleKey, string xToken, string yToken, string durationToken, string easeToken)
        => EmitAbsoluteMove(roleKey, CharacterRigNode.CharSlot_Track, xToken, yToken, durationToken, easeToken);

    // focus_move — 대화할 때만 서는 자리(포커스 계층의 맨 위). 0 0이 본래 자리다.
    private void EnqueueFocusMoveSpec(string roleKey, string xToken, string yToken, string durationToken, string easeToken)
        => EmitAbsoluteMove(roleKey, CharacterRigNode.CharFocus_Track, xToken, yToken, durationToken, easeToken);

    // focus_rotate — 발밑을 중심으로 기운다. 0이 본래 각도다.
    private void EnqueueFocusRotateSpec(string roleKey, float degree, string durationToken, string easeToken)
        => Emit(new RotateToCommandSpecCharR
        {
            roleKey = roleKey,
            node = CharacterRigNode.CharFocus_Rotation,
            toEuler = new Vector3(0f, 0f, degree),
            relativeToCurrent = false,
            duration = YarnDurationParser.Parse(durationToken),
            ease = YarnEaseParser.Parse(easeToken)
        });

    // focus_scale — 발밑을 중심으로 커진다. 1이 본래 크기다.
    private void EnqueueFocusScaleSpec(string roleKey, float scale, string durationToken, string easeToken)
        => Emit(new ScaleToCommandSpecCharR
        {
            roleKey = roleKey,
            node = CharacterRigNode.CharFocus_Scale,
            toScale = new Vector2(scale, scale),
            relativeToCurrent = false,
            duration = YarnDurationParser.Parse(durationToken),
            ease = YarnEaseParser.Parse(easeToken)
        });

    // focus_reset — 포커스의 셋을 함께 본래대로. 노드가 셋이라 스펙도 셋이다(한 노드에 트윈 하나).
    private void EnqueueFocusResetSpec(string roleKey, string durationToken, string easeToken)
    {
        EnqueueFocusMoveSpec(roleKey, "0u", "0u", durationToken, easeToken);
        EnqueueFocusRotateSpec(roleKey, 0f, durationToken, easeToken);
        EnqueueFocusScaleSpec(roleKey, 1f, durationToken, easeToken);
    }

    private void EmitAbsoluteMove(
        string roleKey, CharacterRigNode node, string xToken, string yToken, string durationToken, string easeToken)
        => Emit(new MoveByCommandSpecCharR
        {
            roleKey = roleKey,
            node = node,
            useAbsolutePosition = true,
            delta = new Vector2(ParseSignedUnit(xToken), ParseSignedUnit(yToken)),
            duration = YarnDurationParser.Parse(durationToken),
            ease = YarnEaseParser.Parse(easeToken)
        });

    private void EnqueueCharacterSiblingFrontSpec(string roleKey)
        => Emit(new SetCharacterSiblingOrderCommandSpecCharR
        {
            roleKey = roleKey,
            mode = CharacterRigSiblingOrderMode.Front
        });

    private void EnqueueCharacterSiblingBackSpec(string roleKey)
        => Emit(new SetCharacterSiblingOrderCommandSpecCharR
        {
            roleKey = roleKey,
            mode = CharacterRigSiblingOrderMode.Back
        });
}
