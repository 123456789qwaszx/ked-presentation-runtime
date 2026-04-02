using System;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

/// <summary>
/// Yarn <<command>> 를 즉시 실행하지 않고,
/// CommandSpecBase 목록으로 수집하는 브리지.
/// 
/// 역할:
/// 1) Yarn command -> Spec 생성
/// 2) 생성된 Spec을 내부 버퍼에 축적
/// 3) 외부에서 Flush / Consume 하여 별도 플레이어가 사용
/// </summary>
public sealed class YarnCommandBridge : MonoBehaviour
{
    private DialogueRunner _dialogueRunner;

    [Header("Rig")]
    public GameObject rigPrefab;

    [Header("Global Tuning")]
    public CharStageTuningSO globalTuning;

    // 다음 몇 개의 "즉시 커맨드"에 wait=true 를 부여할지
    private int _pendingImmediateWaitCount;

    // 현재 브리지가 수집한 Spec 버퍼
    private readonly List<CommandSpecBase> _collectedSpecs = new();

    /// <summary>
    /// 현재까지 수집된 Spec을 읽기 전용으로 확인.
    /// </summary>
    public IReadOnlyList<CommandSpecBase> CollectedSpecs => _collectedSpecs;

    public void Initialize(DialogueRunner dialogueRunner)
    {
        _dialogueRunner = dialogueRunner;

        _dialogueRunner.AddCommandHandler<int>("await_for", WaitNextImmediateCommands);

        _dialogueRunner.AddCommandHandler<string>("slot", SetCharRig);
        _dialogueRunner.AddCommandHandler<string, string>("place", SetAnchorPosition);
        _dialogueRunner.AddCommandHandler<string, float>("scale", SetOriginSize);

        _dialogueRunner.AddCommandHandler<string, string>("slide_in", SlideIn);
        _dialogueRunner.AddCommandHandler<string, string>("slide_out", SlideOut);
        _dialogueRunner.AddCommandHandler<string, string>("slide_in_bouncy", BouncySlideIn);

        _dialogueRunner.AddCommandHandler<string>("fade_in", FadeIn);
        _dialogueRunner.AddCommandHandler<string>("fade_out", FadeOut);

        _dialogueRunner.AddCommandHandler<string, float, float>("move_by", MoveBy);
        _dialogueRunner.AddCommandHandler<string, string>("dip", DipInOut);

        _dialogueRunner.AddCommandHandler<string, string>("hop_in", HopIn);

        _dialogueRunner.AddCommandHandler<string, string>("jolt", NudgeJolt);
        _dialogueRunner.AddCommandHandler<string, string>("shake", NudgeShake);
        _dialogueRunner.AddCommandHandler<string, string>("nudge", NudgeTap);
        _dialogueRunner.AddCommandHandler<string, string>("nudge_hard", NudgeTapHard);

        _dialogueRunner.AddCommandHandler<string, string>("cast", SetPortrait);
    }

    /// <summary>
    /// 다음 count개의 즉시성 연출 Spec에 wait 플래그를 부여.
    /// </summary>
    private void WaitNextImmediateCommands(int count = 1)
    {
        _pendingImmediateWaitCount = Mathf.Max(0, count);
    }

    /// <summary>
    /// 라인이 바뀌거나 새 수집 세션 시작 시 호출.
    /// </summary>
    public void ResetImmediateWaitForNewLine()
    {
        _pendingImmediateWaitCount = 0;
    }

    /// <summary>
    /// 현재까지 쌓인 Spec을 복사해서 반환하고 내부 버퍼를 비운다.
    /// 외부 플레이어/빌더가 이 메서드로 가져가면 된다.
    /// </summary>
    public List<CommandSpecBase> ConsumeCollectedSpecs()
    {
        var result = new List<CommandSpecBase>(_collectedSpecs);
        _collectedSpecs.Clear();
        return result;
    }

    /// <summary>
    /// 현재 버퍼를 비운다.
    /// </summary>
    public void ClearCollectedSpecs()
    {
        _collectedSpecs.Clear();
    }

    private void ApplyImmediateWait(CommandSpecBase spec)
    {
        if (spec == null)
            return;

        bool shouldWait = _pendingImmediateWaitCount > 0;

        switch (spec)
        {
            case NudgeTapCommandSpecCharR nudgeTap:
                nudgeTap.wait = shouldWait;
                break;

            case BounceArcInCommandSpecCharR bounceArcIn:
                bounceArcIn.wait = shouldWait;
                break;

            case DipInOutCommandSpecCharR dipInOut:
                dipInOut.wait = shouldWait;
                break;

            case MoveByCommandSpecCharR moveBy:
                moveBy.wait = shouldWait;
                break;

            case BouncySlideInCommandSpecCharR bouncySlideIn:
                bouncySlideIn.wait = shouldWait;
                break;

            case FadeInCommandSpecCharR fadeIn:
                fadeIn.wait = shouldWait;
                break;

            case FadeOutCommandSpecCharR fadeOut:
                fadeOut.wait = shouldWait;
                break;

            case JuicySlideInCommandSpecCharR slideIn:
                slideIn.wait = shouldWait;
                break;

            case JuicySlideOutCommandSpecCharR slideOut:
                slideOut.wait = shouldWait;
                break;
        }

        if (shouldWait)
            _pendingImmediateWaitCount--;
    }

    /// <summary>
    /// 실행 대신 수집.
    /// </summary>
    private void Collect(CommandSpecBase spec)
    {
        if (spec == null)
            return;

        ApplyImmediateWait(spec);
        _collectedSpecs.Add(spec);
    }

    private void NudgeJolt(string roleKey, string direction = "right")
    {
        SlideFromCharR dir = ParseSlideDirection(direction, SlideFromCharR.Right);

        var spec = new NudgeTapCommandSpecCharR
        {
            roleKey = roleKey,
            target = CharacterRigTarget.Character_Track_Y,
            direction = dir,
            strength = 340f,
            duration = 0.6f,
            taps = 3,
            damping = 8,
            anticipation = -12
        };

        Collect(spec);
    }

    private void NudgeShake(string roleKey, string direction = "right")
    {
        SlideFromCharR dir = ParseSlideDirection(direction, SlideFromCharR.Right);

        var spec = new NudgeTapCommandSpecCharR
        {
            roleKey = roleKey,
            direction = dir,
            strength = 44f,
            duration = 1.2f,
            taps = 4
        };

        Collect(spec);
    }

    private void NudgeTap(string roleKey, string direction = "right")
    {
        SlideFromCharR dir = ParseSlideDirection(direction, SlideFromCharR.Right);

        var spec = new NudgeTapCommandSpecCharR
        {
            roleKey = roleKey,
            target = CharacterRigTarget.Character_Track,
            direction = dir,
            strength = 340f,
            duration = 0.6f,
            taps = 1,
            damping = 9,
            anticipation = -12
        };

        Collect(spec);
    }

    private void NudgeTapHard(string roleKey, string direction = "down")
    {
        SlideFromCharR dir = ParseSlideDirection(direction, SlideFromCharR.Down);

        var spec = new NudgeTapCommandSpecCharR
        {
            roleKey = roleKey,
            direction = dir,
            strength = 1400f,
            duration = 0.7f,
            taps = 1,
            damping = 9,
            anticipation = 4
        };

        Collect(spec);
    }

    private void HopIn(string roleKey, string direction = "left")
    {
        SlideFromCharR dir = ParseSlideDirection(direction, SlideFromCharR.Down);

        var spec = new BounceArcInCommandSpecCharR
        {
            roleKey = roleKey,
            from = dir
        };

        Collect(spec);
    }

    private void DipInOut(string roleKey, string direction = "down")
    {
        SlideFromCharR dir = ParseSlideDirection(direction, SlideFromCharR.Down);

        var spec = new DipInOutCommandSpecCharR
        {
            roleKey = roleKey,
            dir = dir
        };

        Collect(spec);
    }

    private void MoveBy(string roleKey, float x, float y)
    {
        var spec = new MoveByCommandSpecCharR
        {
            roleKey = roleKey,
            delta = new Vector2(x, y)
        };

        Collect(spec);
    }

    private void BouncySlideIn(string roleKey, string direction = "left")
    {
        SlideFromCharR from = ParseSlideDirection(direction, SlideFromCharR.Left);

        var spec = new BouncySlideInCommandSpecCharR
        {
            roleKey = roleKey,
            from = from
        };

        Collect(spec);
    }

    private void FadeIn(string roleKey)
    {
        var spec = new FadeInCommandSpecCharR
        {
            roleKey = roleKey
        };

        Collect(spec);
    }

    private void FadeOut(string roleKey)
    {
        var spec = new FadeOutCommandSpecCharR
        {
            roleKey = roleKey
        };

        Collect(spec);
    }

    private void SlideIn(string roleKey, string direction = "left")
    {
        SlideFromCharR from = ParseSlideDirection(direction, SlideFromCharR.Left);

        var spec = new JuicySlideInCommandSpecCharR
        {
            roleKey = roleKey,
            direction = from
        };

        Collect(spec);
    }

    private void SlideOut(string roleKey, string direction = "right")
    {
        SlideFromCharR to = ParseSlideDirection(direction, SlideFromCharR.Right);

        var spec = new JuicySlideOutCommandSpecCharR
        {
            roleKey = roleKey,
            to = to
        };

        Collect(spec);
    }

    private void SetCharRig(string roleKey)
    {
        if (string.IsNullOrWhiteSpace(roleKey))
        {
            Debug.LogError("[YarnCommandBridge] slot: roleKey is null or empty.");
            return;
        }

        var spec = new SetCharRigCommandSpec
        {
            roleKey = roleKey,
            rigPrefab = rigPrefab
        };

        Collect(spec);
    }

    private void SetAnchorPosition(string roleKey, string positionPreset)
    {
        if (string.IsNullOrWhiteSpace(roleKey))
        {
            Debug.LogError("[YarnCommandBridge] place: roleKey is null or empty.");
            return;
        }

        RectAnchorPreset3CharR preset = positionPreset switch
        {
            "left" => RectAnchorPreset3CharR.Left,
            "center" => RectAnchorPreset3CharR.Center,
            "right" => RectAnchorPreset3CharR.Right,
            _ => RectAnchorPreset3CharR.Center
        };

        var anchorSpec = new SetAnchorCommandSpecCharR
        {
            roleKey = roleKey,
            preset = preset,
            globalTuning = globalTuning
        };

        var offsetSpec = new SetPosOffsetCommandSpecCharR
        {
            roleKey = roleKey
        };

        Collect(anchorSpec);
        Collect(offsetSpec);
    }

    private void SetOriginSize(string roleKey, float xyValue)
    {
        var spec = new SetOriginSizeCommandSpecCharR
        {
            roleKey = roleKey,
            toScale = new Vector2(xyValue, xyValue)
        };

        Collect(spec);
    }

    private void SetPortrait(string roleKey, string character)
    {
        var portraitIdentity = new PortraitIdentity
        {
            character = character,
            variant = "a",
            emotion = "1"
        };

        var spec = new SetPortraitSpriteCommandSpecCharR
        {
            roleKey = roleKey,
            portrait = portraitIdentity
        };

        Collect(spec);
    }

    private SlideFromCharR ParseSlideDirection(string direction, SlideFromCharR fallback)
    {
        switch (direction?.Trim().ToLowerInvariant())
        {
            case "left":
            case "l":
                return SlideFromCharR.Left;

            case "right":
            case "r":
                return SlideFromCharR.Right;

            case "up":
            case "u":
            case "top":
                return SlideFromCharR.Up;

            case "down":
            case "d":
            case "bottom":
                return SlideFromCharR.Down;

            default:
                return fallback;
        }
    }
}