using System;
using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge : InlineEventMarkupHandler.IInlineEmojiHost
{
    [Header("Emoji")]
    [SerializeField] private InlineEmojiHost _inlineEmojiHost;

    [Header("Emoji")]
    [SerializeField] private InlineEmojiResolver _emojiResolver;
    
    public void PlayEmojiCue(string cue)
    {
        string roleKey = ResolveCurrentSpeakerRoleKey();

        if (string.IsNullOrWhiteSpace(cue))
        {
            EnqueueEmojiHideCombo(roleKey);
            return;
        }

        EnqueueEmojiCombo(roleKey, cue);
    }
    
    private string _currentSpeakerRoleKey;

    public void SetCurrentSpeakerRoleKey(string roleKey)
    {
        _currentSpeakerRoleKey = roleKey ?? string.Empty;
    }

    private string ResolveCurrentSpeakerRoleKey()
    {
        if (string.IsNullOrWhiteSpace(_currentSpeakerRoleKey))
        {
            Debug.LogWarning("[YarnCommandBridge/Emoji] Current speaker roleKey is empty.", this);
            return string.Empty;
        }

        return _currentSpeakerRoleKey;
    }
    
    private Sprite ResolveEmojiSprite(string emojiKey)
    {
        if (_emojiResolver == null)
        {
            Debug.LogWarning("[YarnCommandBridge/Emoji] InlineEmojiResolver is null.", this);
            return null;
        }

        if (_emojiResolver.TryResolveEmoji(emojiKey, out Sprite sprite))
            return sprite;

        return null;
    }
    // private Sprite ResolveEmojiSprite(string emojiKey)
    // {
    //     if (_inlineEmojiHost == null)
    //     {
    //         Debug.LogWarning("[YarnCommandBridge/Emoji] InlineEmojiHost is null.", this);
    //         return null;
    //     }
    //
    //     if (string.IsNullOrWhiteSpace(emojiKey))
    //     {
    //         Debug.LogWarning("[YarnCommandBridge/Emoji] emojiKey is null or empty.", this);
    //         return null;
    //     }
    //
    //     if (_inlineEmojiHost.TryResolveEmoji(emojiKey, out Sprite sprite))
    //         return sprite;
    //
    //     return null;
    // }
    
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
            Debug.LogWarning($"[YarnCommandBridge/Emoji] Failed to resolve emoji sprite. emojiKey={emojiKey}", this);
            return;
        }

        // 1) sprite 교체
        var setSprite = new SetSpriteCommandSpecCharR
        {
            roleKey = roleKey,
            target = CharacterRigTarget.CharacterEmoji_Image,
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
        var resetScale = new ScaleToCommandSpecCharR
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
        Collect(resetScale);
        Collect(fadeIn);

        switch (NormalizeEmojiKey(emojiKey))
        {
            case "heart":
                EnqueueEmojiHeartCombo(roleKey);
                break;

            case "question":
                EnqueueEmojiQuestionCombo(roleKey);
                break;

            case "angry":
                EnqueueEmojiAngryCombo(roleKey);
                break;

            case "sweat":
                EnqueueEmojiSweatCombo(roleKey);
                break;

            default:
                Debug.LogWarning($"[YarnCommandBridge/Emoji] No combo registered for emojiKey={emojiKey}", this);
                break;
        }
    }

    private void EnqueueEmojiHeartCombo(string roleKey)
    {
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


    private string NormalizeEmojiKey(string emojiKey)
    {
        string key = (emojiKey ?? string.Empty).Trim().ToLowerInvariant();

        switch (key)
        {
            case "q":
                return "question";

            default:
                return key;
        }
    }
}