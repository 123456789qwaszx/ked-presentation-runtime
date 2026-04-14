using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    [Header("Emoji")]
    [SerializeField] private InlineEmojiResolver _emojiResolver;
    
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
    
    private void RegisterEmojiCommands()
    {
        _dialogueRunner.AddCommandHandler<string, string>("emoji", EnqueueInlineEmojiByCharacter);
        _dialogueRunner.AddCommandHandler<string>("emoji_hide", EnqueueInlineEmojiHideByCharacter);
    }

    public void EnqueueInlineEmojiByCharacter(string characterKey, string cue)
    {
        if (string.IsNullOrWhiteSpace(characterKey))
        {
            Debug.LogWarning("[YarnCommandBridge/Emoji] characterKey is null or empty.", this);
            return;
        }

        string emojiKey = NormalizeEmojiKey(cue);

        if (string.IsNullOrWhiteSpace(emojiKey))
        {
            EnqueueInlineEmojiHideByCharacter(characterKey);
            return;
        }

        Sprite sprite = ResolveEmojiSprite(emojiKey);
        if (sprite == null)
        {
            Debug.LogWarning(
                $"[YarnCommandBridge/Emoji] Failed to resolve emoji sprite. characterKey={characterKey}, emojiKey={emojiKey}",
                this);
            return;
        }

        EnqueueEmojiComboByCharacter(characterKey, emojiKey, sprite);
    }

    public void EnqueueInlineEmojiHideByCharacter(string characterKey)
    {
        if (string.IsNullOrWhiteSpace(characterKey))
        {
            Debug.LogWarning("[YarnCommandBridge/Emoji] characterKey is null or empty.", this);
            return;
        }

        EnqueueEmojiHideComboByCharacter(characterKey);
    }

    private void EnqueueEmojiComboByCharacter(string characterKey, string emojiKey, Sprite sprite)
    {
        var setSprite = new SetSpriteByCharacterCommandSpecCharR
        {
            characterKey = characterKey,
            target = CharacterRigTarget.CharacterEmoji_Image,
            sprite = sprite,
            strict = true
        };

        var resetAnchor = new MoveByByCharacterCommandSpecCharR
        {
            characterKey = characterKey,
            target = CharacterRigTarget.CharacterEmoji_Anchor,
            delta = Vector2.zero,
            duration = 0f,
            killTween = false,
            strict = true
        };

        var resetScale = new ScaleToByCharacterCommandSpecCharR
        {
            characterKey = characterKey,
            target = CharacterRigTarget.CharacterEmoji_Scale,
            toScale = Vector2.one,
            duration = 0f,
            strict = true
        };

        var fadeIn = new FadeInByCharacterCommandSpecCharR
        {
            characterKey = characterKey,
            targetMask = CharRigRootLayerMask.CharacterEmoji_Root,
            duration = 0.12f,
            wait = false,
            strict = true
        };

        Collect(setSprite);
        Collect(resetAnchor);
        Collect(resetScale);
        Collect(fadeIn);

        switch (emojiKey)
        {
            case "heart":
                EnqueueEmojiHeartComboByCharacter(characterKey);
                break;

            case "question":
                EnqueueEmojiQuestionComboByCharacter(characterKey);
                break;

            case "angry":
                EnqueueEmojiAngryComboByCharacter(characterKey);
                break;

            case "sweat":
                EnqueueEmojiSweatComboByCharacter(characterKey);
                break;

            default:
                Debug.LogWarning(
                    $"[YarnCommandBridge/Emoji] No emoji combo registered. characterKey={characterKey}, emojiKey={emojiKey}",
                    this);
                break;
        }
    }

    private void EnqueueEmojiHeartComboByCharacter(string characterKey)
    {
        var moveUp = new MoveByByCharacterCommandSpecCharR
        {
            characterKey = characterKey,
            target = CharacterRigTarget.CharacterEmoji_Anchor,
            delta = new Vector2(0f, 38f),
            duration = 0f,
            killTween = false,
            strict = true
        };

        var pop = new PunchScaleByCharacterCommandSpecCharR
        {
            characterKey = characterKey,
            target = CharacterRigTarget.CharacterEmoji_Scale,
            strength = -0.18f,
            duration = 0.38f,
            vibrato = 3,
            elasticity = 0.55f,
            strict = true
        };

        var sway = new SwayByCharacterCommandSpecCharR
        {
            characterKey = characterKey,
            target = CharacterRigTarget.CharacterEmoji_SwayPivot,
            strength = 8f,
            duration = 0.65f,
            cycles = 1,
            damping = 4f,
            speed = 1.2f,
            anticipation = 0f,
            wait = false,
            strict = true
        };

        var wait = new WaitCommandSpec
        {
            roleKey = characterKey,
            seconds = 0.48f
        };

        var fadeOut = new FadeOutByCharacterCommandSpecCharR
        {
            characterKey = characterKey,
            targetMask = CharRigRootLayerMask.CharacterEmoji_Root,
            duration = 0.16f,
            strict = true
        };

        Collect(moveUp);
        Collect(pop);
        Collect(sway);
        Collect(wait);
        Collect(fadeOut);
    }

    private void EnqueueEmojiQuestionComboByCharacter(string characterKey)
    {
        var moveUp = new MoveByByCharacterCommandSpecCharR
        {
            characterKey = characterKey,
            target = CharacterRigTarget.CharacterEmoji_Anchor,
            delta = new Vector2(0f, 44f),
            duration = 0f,
            killTween = false,
            strict = true
        };

        var nudge = new JoltByCharacterCommandSpec
        {
            characterKey = characterKey,
            target = CharacterRigTarget.CharacterEmoji_Track,
            direction = CharRDirection.Up,
            strength = 32f,
            duration = 0.32f,
            taps = 2,
            damping = 8,
            anticipation = 0,
            strict = true
        };

        var wait = new WaitCommandSpec
        {
            roleKey = characterKey,
            seconds = 0.52f
        };

        var fadeOut = new FadeOutByCharacterCommandSpecCharR
        {
            characterKey = characterKey,
            targetMask = CharRigRootLayerMask.CharacterEmoji_Root,
            duration = 0.14f,
            strict = true
        };

        Collect(moveUp);
        Collect(nudge);
        Collect(wait);
        Collect(fadeOut);
    }

    private void EnqueueEmojiAngryComboByCharacter(string characterKey)
    {
        var moveUp = new MoveByByCharacterCommandSpecCharR
        {
            characterKey = characterKey,
            target = CharacterRigTarget.CharacterEmoji_Anchor,
            delta = new Vector2(0f, 32f),
            duration = 0f,
            killTween = false,
            strict = true
        };

        var shake = new JoltByCharacterCommandSpec
        {
            characterKey = characterKey,
            target = CharacterRigTarget.CharacterEmoji_SwayPivot,
            direction = CharRDirection.Right,
            strength = 26f,
            duration = 0.42f,
            taps = 4,
            damping = 10,
            anticipation = -2,
            strict = true
        };

        var scale = new ScaleToByCharacterCommandSpecCharR
        {
            characterKey = characterKey,
            target = CharacterRigTarget.CharacterEmoji_Scale,
            toScale = new Vector2(1.08f, 1.08f),
            duration = 0.08f,
            strict = true
        };

        var wait = new WaitCommandSpec
        {
            roleKey = characterKey,
            seconds = 0.42f
        };

        var fadeOut = new FadeOutByCharacterCommandSpecCharR
        {
            characterKey = characterKey,
            targetMask = CharRigRootLayerMask.CharacterEmoji_Root,
            duration = 0.12f,
            strict = true
        };

        Collect(moveUp);
        Collect(scale);
        Collect(shake);
        Collect(wait);
        Collect(fadeOut);
    }

    private void EnqueueEmojiSweatComboByCharacter(string characterKey)
    {
        var move = new MoveByByCharacterCommandSpecCharR
        {
            characterKey = characterKey,
            target = CharacterRigTarget.CharacterEmoji_Anchor,
            delta = new Vector2(16f, 42f),
            duration = 0f,
            killTween = false,
            strict = true
        };

        var dip = new DipInOutByCharacterCommandSpecCharR
        {
            characterKey = characterKey,
            target = CharacterRigTarget.CharacterEmoji_Track,
            dir = CharRDirection.Down,
            distance = 10f,
            duration = 0.55f,
            strict = true
        };

        var wait = new WaitCommandSpec
        {
            roleKey = characterKey,
            seconds = 0.42f
        };

        var fadeOut = new FadeOutByCharacterCommandSpecCharR
        {
            characterKey = characterKey,
            targetMask = CharRigRootLayerMask.CharacterEmoji_Root,
            duration = 0.12f,
            strict = true
        };

        Collect(move);
        Collect(dip);
        Collect(wait);
        Collect(fadeOut);
    }

    private void EnqueueEmojiHideComboByCharacter(string characterKey)
    {
        var fadeOut = new FadeOutByCharacterCommandSpecCharR
        {
            characterKey = characterKey,
            targetMask = CharRigRootLayerMask.CharacterEmoji_Root,
            duration = 0.12f,
            wait = false,
            strict = true
        };

        Collect(fadeOut);
    }
    
}