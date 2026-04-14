using System;
using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    [Header("Emoji Test Sprites")]
    [SerializeField] private Sprite emojiHeart;
    [SerializeField] private Sprite emojiQuestion;
    [SerializeField] private Sprite emojiAngry;
    [SerializeField] private Sprite emojiSweat;

    private void RegisterEmojiCommands()
    {
        _dialogueRunner.AddCommandHandler<string, string>("emoji", EnqueueEmojiCombo);
        _dialogueRunner.AddCommandHandler<string>("emoji_hide", EnqueueEmojiHideCombo);
    }

    private void EnqueueEmojiCombo(string roleKey, string emojiKey)
    {
        if (string.IsNullOrWhiteSpace(roleKey))
        {
            Debug.LogError("[YarnCommandBridge/Emoji] roleKey is null or empty.");
            return;
        }

        Sprite sprite = ResolveEmojiSprite(emojiKey);
        if (sprite == null)
        {
            Debug.LogWarning($"[YarnCommandBridge/Emoji] Unknown emojiKey '{emojiKey}'.");
            return;
        }

        // 1) sprite 교체
        var setSprite = new SetSpriteCommandSpecCharR()
        {
            target = CharacterRigTarget.CharacterEmoji_Image,
            
            roleKey = roleKey,
            sprite = sprite
        };

        // 2) 기본 위치 리셋
        var resetAnchor = new MoveByCommandSpecCharR
        {
            roleKey = roleKey,
            target = CharacterRigTarget.CharacterEmoji_Anchor,
            delta = Vector2.zero,
            duration = 0f,
            killTween = false
        };

        // 3) 기본 scale
        var scale = new ScaleToCommandSpecCharR
        {
            roleKey = roleKey,
            target = CharacterRigTarget.CharacterEmoji_Scale,
            toScale = Vector2.one,
            duration = 0f
        };

        // 4) 등장
        var fadeIn = new FadeInCommandSpecCharR
        {
            roleKey = roleKey,
            targetMask = CharRigRootLayerMask.CharacterEmoji_Root,
            duration = 0.12f,
            wait = false
        };

        Collect(setSprite);
        Collect(resetAnchor);
        Collect(scale);
        Collect(fadeIn);

        switch ((emojiKey ?? "").Trim().ToLowerInvariant())
        {
            case "heart":
                EnqueueEmojiHeartCombo(roleKey);
                break;

            case "question":
            case "q":
                EnqueueEmojiQuestionCombo(roleKey);
                break;

            case "angry":
                EnqueueEmojiAngryCombo(roleKey);
                break;

            case "sweat":
                EnqueueEmojiSweatCombo(roleKey);
                break;

            // default:
            //     // 기본은 그냥 짧게 보여주고 사라짐
            //     Collect(new WaitCommandSpec
            //     {
            //         roleKey = roleKey,
            //         seconds = 0.55f
            //     });
            //
            //     Collect(new FadeOutCommandSpecCharR
            //     {
            //         roleKey = roleKey,
            //         targetMask = CharRigRootLayerMask.CharacterEmoji_Root,
            //         duration = 0.14f,
            //         wait = false
            //     });
                break;
        }
    }

    private void EnqueueEmojiHideCombo(string roleKey)
    {
        if (string.IsNullOrWhiteSpace(roleKey))
        {
            Debug.LogError("[YarnCommandBridge/Emoji] roleKey is null or empty.");
            return;
        }

        var fadeOut = new FadeOutCommandSpecCharR
        {
            roleKey = roleKey,
            targetMask = CharRigRootLayerMask.CharacterEmoji_Root,
            duration = 0.12f,
            wait = false
        };

        Collect(fadeOut);
    }

    private void EnqueueEmojiHeartCombo(string roleKey)
    {
        var showEmojiLayer = new FadeInCommandSpecCharR()
        {
            roleKey = roleKey,
            targetMask = CharRigRootLayerMask.CharacterEmoji_Root
        };

        Collect(showEmojiLayer);
        
        var moveUp = new MoveByCommandSpecCharR
        {
            roleKey = roleKey,
            target = CharacterRigTarget.CharacterEmoji_Anchor,
            delta = new Vector2(0f, 38f),
            duration = 0f,
            killTween = false
        };

        var pop = new PunchScaleCommandSpecCharR
        {
            roleKey = roleKey,
            target = CharacterRigTarget.CharacterEmoji_Scale,
            strength = -0.18f,
            duration = 0.38f,
            vibrato = 3,
            elasticity = 0.55f
        };

        var sway = new SwayCommandSpecCharR
        {
            roleKey = roleKey,
            target = CharacterRigTarget.CharacterEmoji_SwayPivot,
            strength = 8f,
            duration = 0.65f,
            cycles = 1,
            damping = 4f,
            speed = 1.2f,
            anticipation = 0f,
            wait = false
        };

        var wait = new WaitCommandSpec
        {
            roleKey = roleKey,
            seconds = 0.48f
        };

        var fadeOut = new FadeOutCommandSpecCharR
        {
            roleKey = roleKey,
            targetMask = CharRigRootLayerMask.CharacterEmoji_Root,
            duration = 0.16f
        };

        Collect(moveUp);
        Collect(pop);
        Collect(sway);
        Collect(wait);
        Collect(fadeOut);
    }

    private void EnqueueEmojiQuestionCombo(string roleKey)
    {
        
        var showEmojiLayer = new FadeInCommandSpecCharR()
        {
            roleKey = roleKey,
            targetMask = CharRigRootLayerMask.CharacterEmoji_Root
        };

        Collect(showEmojiLayer);
        
        var moveUp = new MoveByCommandSpecCharR
        {
            roleKey = roleKey,
            target = CharacterRigTarget.CharacterEmoji_Anchor,
            delta = new Vector2(0f, 44f),
            duration = 0f,
            killTween = false
        };

        var nudge = new JoltCommandSpecCharR
        {
            roleKey = roleKey,
            target = CharacterRigTarget.CharacterEmoji_Track,
            direction = CharRDirection.Up,
            strength = 32f,
            duration = 0.32f,
            taps = 2,
            damping = 8,
            anticipation = 0
        };

        var wait = new WaitCommandSpec
        {
            roleKey = roleKey,
            seconds = 0.52f
        };

        var fadeOut = new FadeOutCommandSpecCharR
        {
            roleKey = roleKey,
            targetMask = CharRigRootLayerMask.CharacterEmoji_Root,
            duration = 0.14f
        };

        Collect(moveUp);
        Collect(nudge);
        Collect(wait);
        Collect(fadeOut);
    }

    private void EnqueueEmojiAngryCombo(string roleKey)
    {
        var showEmojiLayer = new FadeInCommandSpecCharR()
        {
            roleKey = roleKey,
            targetMask = CharRigRootLayerMask.CharacterEmoji_Root
        };

        Collect(showEmojiLayer);
        
        var moveUp = new MoveByCommandSpecCharR
        {
            roleKey = roleKey,
            target = CharacterRigTarget.CharacterEmoji_Anchor,
            delta = new Vector2(0f, 32f),
            duration = 0f,
            killTween = false
        };

        var shake = new JoltCommandSpecCharR
        {
            roleKey = roleKey,
            target = CharacterRigTarget.CharacterEmoji_SwayPivot,
            direction = CharRDirection.Right,
            strength = 26f,
            duration = 0.42f,
            taps = 4,
            damping = 10,
            anticipation = -2
        };

        var scale = new ScaleToCommandSpecCharR
        {
            roleKey = roleKey,
            target = CharacterRigTarget.CharacterEmoji_Scale,
            toScale = new Vector2(1.08f, 1.08f),
            duration = 0.08f
        };

        var wait = new WaitCommandSpec
        {
            roleKey = roleKey,
            seconds = 0.42f
        };

        var fadeOut = new FadeOutCommandSpecCharR
        {
            roleKey = roleKey,
            targetMask = CharRigRootLayerMask.CharacterEmoji_Root,
            duration = 0.12f
        };

        Collect(moveUp);
        Collect(scale);
        Collect(shake);
        Collect(wait);
        Collect(fadeOut);
    }

    private void EnqueueEmojiSweatCombo(string roleKey)
    {
        var showEmojiLayer = new FadeInCommandSpecCharR()
        {
            roleKey = roleKey,
            targetMask = CharRigRootLayerMask.CharacterEmoji_Root
        };

        Collect(showEmojiLayer);
        
        var move = new MoveByCommandSpecCharR
        {
            roleKey = roleKey,
            target = CharacterRigTarget.CharacterEmoji_Anchor,
            delta = new Vector2(16f, 42f),
            duration = 0f,
            killTween = false
        };

        var dip = new DipInOutCommandSpecCharR
        {
            roleKey = roleKey,
            target = CharacterRigTarget.CharacterEmoji_Track,
            dir = CharRDirection.Down,
            distance = 10f,
            duration = 0.55f
        };

        var wait = new WaitCommandSpec
        {
            roleKey = roleKey,
            seconds = 0.42f
        };

        var fadeOut = new FadeOutCommandSpecCharR
        {
            roleKey = roleKey,
            targetMask = CharRigRootLayerMask.CharacterEmoji_Root,
            duration = 0.12f
        };

        Collect(move);
        Collect(dip);
        Collect(wait);
        Collect(fadeOut);
    }

    private Sprite ResolveEmojiSprite(string emojiKey)
    {
        switch ((emojiKey ?? "").Trim().ToLowerInvariant())
        {
            case "heart":
                return emojiHeart;

            case "question":
            case "q":
                return emojiQuestion;

            case "angry":
                return emojiAngry;

            case "sweat":
                return emojiSweat;

            default:
                return null;
        }
    }
}