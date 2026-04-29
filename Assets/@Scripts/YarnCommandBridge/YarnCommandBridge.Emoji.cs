using System;
using System.Collections.Generic;
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
        _dialogueRunner.AddCommandHandler(
            "emoji",
            (Action<string, string>)EnqueueInlineEmojiByCharacter);

        _dialogueRunner.AddCommandHandler(
            "emoji_hide",
            (Action<string>)EnqueueInlineEmojiHideByCharacter);
    }

    // inline markup용: 지금 즉시 재생
    public void PlayInlineEmojiByCharacterNow(string characterKey, string cue)
    {
        if (string.IsNullOrWhiteSpace(characterKey))
        {
            Debug.LogWarning("[YarnCommandBridge/Emoji] characterKey is null or empty.", this);
            return;
        }

        if (_playbackDriver == null)
        {
            Debug.LogWarning("[YarnCommandBridge/Emoji] YarnBridgePlaybackDriver is null.", this);
            return;
        }

        string emojiKey = NormalizeEmojiKey(cue);

        if (string.IsNullOrWhiteSpace(emojiKey))
        {
            List<CommandSpecBase> hideSpecs = BuildEmojiHideComboByCharacter(characterKey);
            _playbackDriver.PlayImmediate(hideSpecs, "inline-emoji-hide");
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

        List<CommandSpecBase> specs = BuildEmojiComboByCharacter(characterKey, emojiKey, sprite);
        _playbackDriver.PlayImmediate(specs, "inline-emoji");
    }

    // Yarn command용: 기존처럼 큐에 모아두기
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

        List<CommandSpecBase> specs = BuildEmojiComboByCharacter(characterKey, emojiKey, sprite);
        CollectAll(specs);
    }

    public void EnqueueInlineEmojiHideByCharacter(string characterKey)
    {
        if (string.IsNullOrWhiteSpace(characterKey))
        {
            Debug.LogWarning("[YarnCommandBridge/Emoji] characterKey is null or empty.", this);
            return;
        }

        List<CommandSpecBase> specs = BuildEmojiHideComboByCharacter(characterKey);
        CollectAll(specs);
    }

    private void CollectAll(IReadOnlyList<CommandSpecBase> specs)
    {
        if (specs == null)
            return;

        for (int i = 0; i < specs.Count; i++)
            Collect(specs[i]);
    }

    private List<CommandSpecBase> BuildEmojiComboByCharacter(string characterKey, string emojiKey, Sprite sprite)
    {
        var specs = new List<CommandSpecBase>(8);

        specs.Add(new SetSpriteByCharacterCommandSpecCharR
        {
            characterKey = characterKey,
            target = CharacterRigTarget.CharacterEmoji_Image,
            sprite = sprite,
            strict = true
        });

        specs.Add(new MoveByByCharacterCommandSpecCharR
        {
            characterKey = characterKey,
            target = CharacterRigTarget.CharacterEmoji_Anchor,
            delta = Vector2.zero,
            duration = 0f,
            killTween = false,
            strict = true
        });

        specs.Add(new ScaleToByCharacterCommandSpecCharR
        {
            characterKey = characterKey,
            target = CharacterRigTarget.CharacterEmoji_Scale,
            toScale = Vector2.one,
            duration = 0f,
            strict = true
        });

        specs.Add(new FadeInByCharacterCommandSpecCharR
        {
            characterKey = characterKey,
            targetMask = CharRigRootLayerMask.CharacterEmoji_Root,
            duration = 0.12f,
            wait = false,
            strict = true
        });

        switch (emojiKey)
        {
            case "heart":
                AppendEmojiHeartComboByCharacter(specs, characterKey);
                break;

            case "question":
                AppendEmojiQuestionComboByCharacter(specs, characterKey);
                break;

            case "angry":
                AppendEmojiAngryComboByCharacter(specs, characterKey);
                break;

            case "sweat":
                AppendEmojiSweatComboByCharacter(specs, characterKey);
                break;

            default:
                Debug.LogWarning(
                    $"[YarnCommandBridge/Emoji] No emoji combo registered. characterKey={characterKey}, emojiKey={emojiKey}",
                    this);
                break;
        }

        return specs;
    }

    private List<CommandSpecBase> BuildEmojiHideComboByCharacter(string characterKey)
    {
        var specs = new List<CommandSpecBase>(1);

        specs.Add(new FadeOutByCharacterCommandSpecCharR
        {
            characterKey = characterKey,
            targetMask = CharRigRootLayerMask.CharacterEmoji_Root,
            duration = 0.12f,
            wait = false,
            strict = true
        });

        return specs;
    }

    private void AppendEmojiHeartComboByCharacter(List<CommandSpecBase> specs, string characterKey)
    {
        specs.Add(new MoveByByCharacterCommandSpecCharR
        {
            characterKey = characterKey,
            target = CharacterRigTarget.CharacterEmoji_Anchor,
            delta = new Vector2(0f, 38f),
            duration = 0f,
            killTween = false,
            strict = true
        });

        specs.Add(new PunchScaleByCharacterCommandSpecCharR
        {
            characterKey = characterKey,
            target = CharacterRigTarget.CharacterEmoji_Scale,
            strength = -0.18f,
            duration = 0.38f,
            vibrato = 3,
            elasticity = 0.55f,
            strict = true
        });

        specs.Add(new SwayByCharacterCommandSpecCharR
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
        });

        specs.Add(new WaitCommandSpec
        {
            roleKey = characterKey,
            seconds = 0.48f
        });

        specs.Add(new FadeOutByCharacterCommandSpecCharR
        {
            characterKey = characterKey,
            targetMask = CharRigRootLayerMask.CharacterEmoji_Root,
            duration = 0.16f,
            strict = true
        });
    }

    private void AppendEmojiQuestionComboByCharacter(List<CommandSpecBase> specs, string characterKey)
    {
        specs.Add(new MoveByByCharacterCommandSpecCharR
        {
            characterKey = characterKey,
            target = CharacterRigTarget.CharacterEmoji_Anchor,
            delta = new Vector2(0f, 44f),
            duration = 0f,
            killTween = false,
            strict = true
        });

        specs.Add(new JoltByCharacterCommandSpec
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
        });

        specs.Add(new WaitCommandSpec
        {
            roleKey = characterKey,
            seconds = 0.52f
        });

        specs.Add(new FadeOutByCharacterCommandSpecCharR
        {
            characterKey = characterKey,
            targetMask = CharRigRootLayerMask.CharacterEmoji_Root,
            duration = 0.14f,
            strict = true
        });
    }

    private void AppendEmojiAngryComboByCharacter(List<CommandSpecBase> specs, string characterKey)
    {
        specs.Add(new MoveByByCharacterCommandSpecCharR
        {
            characterKey = characterKey,
            target = CharacterRigTarget.CharacterEmoji_Anchor,
            delta = new Vector2(0f, 32f),
            duration = 0f,
            killTween = false,
            strict = true
        });

        specs.Add(new JoltByCharacterCommandSpec
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
        });

        specs.Add(new ScaleToByCharacterCommandSpecCharR
        {
            characterKey = characterKey,
            target = CharacterRigTarget.CharacterEmoji_Scale,
            toScale = new Vector2(1.08f, 1.08f),
            duration = 0.08f,
            strict = true
        });

        specs.Add(new WaitCommandSpec
        {
            roleKey = characterKey,
            seconds = 0.42f
        });

        specs.Add(new FadeOutByCharacterCommandSpecCharR
        {
            characterKey = characterKey,
            targetMask = CharRigRootLayerMask.CharacterEmoji_Root,
            duration = 0.12f,
            strict = true
        });
    }

    private void AppendEmojiSweatComboByCharacter(List<CommandSpecBase> specs, string characterKey)
    {
        specs.Add(new MoveByByCharacterCommandSpecCharR
        {
            characterKey = characterKey,
            target = CharacterRigTarget.CharacterEmoji_Anchor,
            delta = new Vector2(16f, 42f),
            duration = 0f,
            killTween = false,
            strict = true
        });

        specs.Add(new DipInOutByCharacterCommandSpecCharR
        {
            characterKey = characterKey,
            target = CharacterRigTarget.CharacterEmoji_Track,
            dir = CharRDirection.Down,
            distance = 10f,
            duration = 0.55f,
            strict = true
        });

        specs.Add(new WaitCommandSpec
        {
            roleKey = characterKey,
            seconds = 0.42f
        });

        specs.Add(new FadeOutByCharacterCommandSpecCharR
        {
            characterKey = characterKey,
            targetMask = CharRigRootLayerMask.CharacterEmoji_Root,
            duration = 0.12f,
            strict = true
        });
    }
}