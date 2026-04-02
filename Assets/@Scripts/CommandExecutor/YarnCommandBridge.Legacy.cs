// using UnityEngine;
// using Yarn.Unity;
//
// public sealed class YarnCommandBridge : MonoBehaviour
// {
//     private DialogueRunner _dialogueRunner;
//     private ImmediateCommandRunner _commandPlayer;
//
//     public GameObject rigPrefab;
//     public CharStageTuningSO globalTuning;
//
//     // 다음 "즉시 커맨드" 몇 개를 wait=true 로 실행할지
//     private int _pendingImmediateWaitCount;
//
//     public void Initialize(DialogueRunner dialogueRunner, ImmediateCommandRunner commandPlayer)
//     {
//         _dialogueRunner = dialogueRunner;
//         _commandPlayer = commandPlayer;
//
//         _dialogueRunner.AddCommandHandler<int>("await_for", WaitNextImmediateCommands);
//
//         _dialogueRunner.AddCommandHandler<string>("slot", SetCharRig);
//         _dialogueRunner.AddCommandHandler<string, string>("place", SetAnchorPosition);
//         _dialogueRunner.AddCommandHandler<string, float>("scale", SetOriginSize);
//
//         _dialogueRunner.AddCommandHandler<string, string>("slide_in", SlideIn);
//         _dialogueRunner.AddCommandHandler<string, string>("slide_out", SlideOut);
//         _dialogueRunner.AddCommandHandler<string, string>("slide_in_bouncy", BouncySlideIn);
//
//         _dialogueRunner.AddCommandHandler<string>("fade_in", FadeIn);
//         _dialogueRunner.AddCommandHandler<string>("fade_out", FadeOut);
//
//         _dialogueRunner.AddCommandHandler<string, float, float>("move_by", MoveBy);
//         _dialogueRunner.AddCommandHandler<string, string>("dip", DipInOut);
//
//         _dialogueRunner.AddCommandHandler<string, string>("hop_in", HopIn);
//
//         _dialogueRunner.AddCommandHandler<string, string>("jolt", NudgeJolt);
//         _dialogueRunner.AddCommandHandler<string, string>("shake", NudgeShake);
//         _dialogueRunner.AddCommandHandler<string, string>("nudge", NudgeTap);
//         _dialogueRunner.AddCommandHandler<string, string>("nudge_hard", NudgeTapHard);
//
//         _dialogueRunner.AddCommandHandler<string, string>("cast", SetPortrait);
//     }
//
//
//
//     private void WaitNextImmediateCommands(int count = 1)
//     {
//         _pendingImmediateWaitCount = Mathf.Max(0, count);
//     }
//     
//     public void ResetImmediateWaitForNewLine()
//     {
//         _pendingImmediateWaitCount = 0;
//     }
//
//     private void ApplyImmediateWait(CommandSpecBase spec)
//     {
//         if (spec == null)
//             return;
//
//         bool shouldWait = _pendingImmediateWaitCount > 0;
//
//         switch (spec)
//         {
//             case NudgeTapCommandSpecCharR nudgeTap:
//                 nudgeTap.wait = shouldWait;
//                 break;
//
//             case BounceArcInCommandSpecCharR bounceArcIn:
//                 bounceArcIn.wait = shouldWait;
//                 break;
//
//             case DipInOutCommandSpecCharR dipInOut:
//                 dipInOut.wait = shouldWait;
//                 break;
//
//             case MoveByCommandSpecCharR moveBy:
//                 moveBy.wait = shouldWait;
//                 break;
//
//             case BouncySlideInCommandSpecCharR bouncySlideIn:
//                 bouncySlideIn.wait = shouldWait;
//                 break;
//
//             case FadeInCommandSpecCharR fadeIn:
//                 fadeIn.wait = shouldWait;
//                 break;
//
//             case FadeOutCommandSpecCharR fadeOut:
//                 fadeOut.wait = shouldWait;
//                 break;
//
//             case JuicySlideInCommandSpecCharR slideIn:
//                 slideIn.wait = shouldWait;
//                 break;
//
//             case JuicySlideOutCommandSpecCharR slideOut:
//                 slideOut.wait = shouldWait;
//                 break;
//         }
//
//         if (shouldWait)
//             _pendingImmediateWaitCount--;
//     }
//
//     private void RunImmediate(CommandSpecBase spec)
//     {
//         ApplyImmediateWait(spec);
//         _commandPlayer.Run(spec);
//     }
//
//     private void NudgeJolt(string roleKey, string direction = "right")
//     {
//         SlideFromCharR dir = ParseSlideDirection(direction, SlideFromCharR.Right);
//
//         var spec = new NudgeTapCommandSpecCharR
//         {
//             roleKey = roleKey,
//             target = CharacterRigTarget.Character_Track_Y,
//             direction = dir,
//             strength = 340f,
//             duration = 0.6f,
//             taps = 3,
//             damping = 8,
//             anticipation = -12
//         };
//
//         RunImmediate(spec);
//     }
//
//     private void NudgeShake(string roleKey, string direction = "right")
//     {
//         SlideFromCharR dir = ParseSlideDirection(direction, SlideFromCharR.Right);
//
//         var spec = new NudgeTapCommandSpecCharR
//         {
//             roleKey = roleKey,
//             direction = dir,
//             strength = 44f,
//             duration = 1.2f,
//             taps = 4
//         };
//
//         RunImmediate(spec);
//     }
//
//     private void NudgeTap(string roleKey, string direction = "right")
//     {
//         SlideFromCharR dir = ParseSlideDirection(direction, SlideFromCharR.Right);
//
//         var spec = new NudgeTapCommandSpecCharR
//         {
//             roleKey = roleKey,
//             target = CharacterRigTarget.Character_Track,
//             direction = dir,
//             strength = 340f,
//             duration = 0.6f,
//             taps = 1,
//             damping = 9,
//             anticipation = -12
//         };
//
//         RunImmediate(spec);
//     }
//
//     private void NudgeTapHard(string roleKey, string direction = "down")
//     {
//         SlideFromCharR dir = ParseSlideDirection(direction, SlideFromCharR.Down);
//
//         var spec = new NudgeTapCommandSpecCharR
//         {
//             roleKey = roleKey,
//             direction = dir,
//             strength = 1400f,
//             duration = 0.7f,
//             taps = 1,
//             damping = 9,
//             anticipation = 4
//         };
//
//          RunImmediate(spec);
//     }
//
//     private void HopIn(string roleKey, string direction = "left")
//     {
//         SlideFromCharR dir = ParseSlideDirection(direction, SlideFromCharR.Down);
//
//         var spec = new BounceArcInCommandSpecCharR
//         {
//             roleKey = roleKey,
//             from = dir
//         };
//
//          RunImmediate(spec);
//     }
//
//     private void DipInOut(string roleKey, string direction = "down")
//     {
//         SlideFromCharR dir = ParseSlideDirection(direction, SlideFromCharR.Down);
//
//         var spec = new DipInOutCommandSpecCharR
//         {
//             roleKey = roleKey,
//             dir = dir
//         };
//
//          RunImmediate(spec);
//     }
//
//     private void MoveBy(string roleKey, float x, float y)
//     {
//         var spec = new MoveByCommandSpecCharR
//         {
//             roleKey = roleKey,
//             delta = new Vector2(x, y)
//         };
//
//          RunImmediate(spec);
//     }
//
//     private void BouncySlideIn(string roleKey, string direction = "left")
//     {
//         SlideFromCharR from = ParseSlideDirection(direction, SlideFromCharR.Left);
//
//         var spec = new BouncySlideInCommandSpecCharR
//         {
//             roleKey = roleKey,
//             from = from
//         };
//
//          RunImmediate(spec);
//     }
//
//     private void FadeIn(string roleKey)
//     {
//         var spec = new FadeInCommandSpecCharR
//         {
//             roleKey = roleKey
//         };
//
//          RunImmediate(spec);
//     }
//
//     private void FadeOut(string roleKey)
//     {
//         var spec = new FadeOutCommandSpecCharR
//         {
//             roleKey = roleKey
//         };
//
//          RunImmediate(spec);
//     }
//
//     private void SlideIn(string roleKey, string direction = "left")
//     {
//         SlideFromCharR from = ParseSlideDirection(direction, SlideFromCharR.Left);
//
//         var spec = new JuicySlideInCommandSpecCharR
//         {
//             roleKey = roleKey,
//             direction = from
//         };
//
//          RunImmediate(spec);
//     }
//
//     private void SlideOut(string roleKey, string direction = "right")
//     {
//         SlideFromCharR to = ParseSlideDirection(direction, SlideFromCharR.Right);
//
//         var spec = new JuicySlideOutCommandSpecCharR
//         {
//             roleKey = roleKey,
//             to = to
//         };
//
//          RunImmediate(spec);
//     }
//
//     private void SetCharRig(string roleKey)
//     {
//         if (string.IsNullOrWhiteSpace(roleKey))
//         {
//             Debug.LogError("[YarnCommandBridge] char_rig: roleKey is null or empty.");
//             return;
//         }
//
//         var spec = new SetCharRigCommandSpec
//         {
//             roleKey = roleKey,
//             rigPrefab = rigPrefab
//         };
//
//         _commandPlayer.Run(spec);
//     }
//
//     private void SetAnchorPosition(string roleKey, string positionPreset)
//     {
//         if (string.IsNullOrWhiteSpace(roleKey))
//         {
//             Debug.LogError("[YarnCommandBridge] anchor: roleKey is null or empty.");
//             return;
//         }
//
//         RectAnchorPreset3CharR preset = positionPreset switch
//         {
//             "left" => RectAnchorPreset3CharR.Left,
//             "center" => RectAnchorPreset3CharR.Center,
//             "right" => RectAnchorPreset3CharR.Right,
//             _ => RectAnchorPreset3CharR.Center
//         };
//
//         var spec = new SetAnchorCommandSpecCharR
//         {
//             roleKey = roleKey,
//             preset = preset,
//             globalTuning = globalTuning
//         };
//
//         var spec2 = new SetPosOffsetCommandSpecCharR
//         {
//             roleKey = roleKey,
//         };
//
//         _commandPlayer.Run(spec);
//         _commandPlayer.Run(spec2);
//     }
//
//     private void SetOriginSize(string roleKey, float xyValue)
//     {
//         var spec = new SetOriginSizeCommandSpecCharR
//         {
//             roleKey = roleKey,
//             toScale = new Vector2(xyValue, xyValue)
//         };
//
//         _commandPlayer.Run(spec);
//     }
//
//     private void SetPortrait(string roleKey, string character)
//     {
//         var portraitIdentity = new PortraitIdentity
//         {
//             character = character,
//             variant = "a",
//             emotion = "1"
//         };
//
//         var spec = new SetPortraitSpriteCommandSpecCharR
//         {
//             roleKey = roleKey,
//             portrait = portraitIdentity
//         };
//
//         _commandPlayer.Run(spec);
//     }
//
//     private SlideFromCharR ParseSlideDirection(string direction, SlideFromCharR fallback)
//     {
//         switch (direction?.Trim().ToLowerInvariant())
//         {
//             case "left":
//             case "l":
//                 return SlideFromCharR.Left;
//
//             case "right":
//             case "r":
//                 return SlideFromCharR.Right;
//
//             case "up":
//             case "u":
//             case "top":
//                 return SlideFromCharR.Up;
//
//             case "down":
//             case "d":
//             case "bottom":
//                 return SlideFromCharR.Down;
//
//             default:
//                 return fallback;
//         }
//     }
// }