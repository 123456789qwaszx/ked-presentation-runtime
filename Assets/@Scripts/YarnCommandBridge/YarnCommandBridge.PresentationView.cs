using DG.Tweening;
using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    public void RegisterPresentationCommands()
    {
        _dialogueRunner.AddCommandHandler("presentation_setup", EnqueueSetupPresentationViewSpec);

        _dialogueRunner.AddCommandHandler<string, string, string>("bg_spawn", EnqueueSpawnBackgroundSpec);
        _dialogueRunner.AddCommandHandler<string, string, string, string>(
            "bg_spawn_bound",
            EnqueueSpawnBackgroundBoundSpec);

        _dialogueRunner.AddCommandHandler<string, string>("bg_sprite", EnqueueSetBackgroundSpriteSpec);
        _dialogueRunner.AddCommandHandler<string>("bg_destroy", EnqueueDestroyBackgroundSpec);
        _dialogueRunner.AddCommandHandler<string, float, float>("bg_fade", EnqueueFadeBackgroundSpec);

        _dialogueRunner.AddCommandHandler<string, float, float>("fade_to", EnqueueFadeToPresentationSpec);
        _dialogueRunner.AddCommandHandler<string, float, float, float>("move_by_p", EnqueueMoveByPresentationSpec);
        _dialogueRunner.AddCommandHandler<string, float, float>("scale_to_p", EnqueueScaleToPresentationSpec);

        // PresentationTarget direct transform
        _dialogueRunner.AddCommandHandler<string>("p_reset", EnqueueResetPresentationTargetSpec);
        _dialogueRunner.AddCommandHandler<string, float, float>("p_offset", EnqueueApplyPresentationTargetOffsetSpec);
        _dialogueRunner.AddCommandHandler<string, float, float, float>("p_move_by", EnqueueMoveByPresentationTargetSpec);
        _dialogueRunner.AddCommandHandler<string, float, float, float>("p_move_to", EnqueueMoveToPresentationTargetSpec);
        _dialogueRunner.AddCommandHandler<string, float, float>("p_scale_to", EnqueueScaleToPresentationTargetUniformSpec);
        _dialogueRunner.AddCommandHandler<string, float, float, float, float>("p_scale_to_xyz", EnqueueScaleToPresentationTargetXYZSpec);
        _dialogueRunner.AddCommandHandler<string, float, float, float, float>("p_rotate_to", EnqueueRotateToPresentationTargetSpec);
        _dialogueRunner.AddCommandHandler<string, string, float, float>("p_slide_in", EnqueueSlideInPresentationTargetSpec);
        
        _dialogueRunner.AddCommandHandler<string, float>("p_slant_cut_in", EnqueueSlantedMaskCutInSpec);
        _dialogueRunner.AddCommandHandler<string, float>("p_slant_cut_out", EnqueueSlantedMaskCutOutSpec);
        
        _dialogueRunner.AddCommandHandler<string, float>("p_strip_cover", EnqueueVerticalStripCoverSpec);
        _dialogueRunner.AddCommandHandler<string, float>("p_strip_clear", EnqueueVerticalStripClearSpec);

        _dialogueRunner.AddCommandHandler("box_hide", EnqueueHideDialogueBoxSpec);
    }
    
    private void EnqueueVerticalStripCoverSpec(
        string targetName,
        float duration = 0f)
    {
        if (!PresentationTargetParser.TryParse(targetName, out PresentationTarget target))
        {
            Debug.LogError($"[YarnCommandBridge] p_strip_cover: Unknown target '{targetName}'.");
            return;
        }

        var spec = new VerticalStripWipeCommandSpec
        {
            target = target,

            mode = VerticalStripWipeMode.Cover,
            order = VerticalStripWipeOrder.LeftToRight,

            stripCount = 20,
            stripDelay = 0.02f,
            stripFillDuration = 0.08f,

            color = Color.black,

            duration = duration,
            ease = Ease.Linear,

            wait = true,
            killTween = true,
            disableWhenClear = true,
            strict = true
        };

        Collect(spec);
    }

    private void EnqueueVerticalStripClearSpec(
        string targetName,
        float duration = 0f)
    {
        if (!PresentationTargetParser.TryParse(targetName, out PresentationTarget target))
        {
            Debug.LogError($"[YarnCommandBridge] p_strip_clear: Unknown target '{targetName}'.");
            return;
        }

        var spec = new VerticalStripWipeCommandSpec
        {
            target = target,

            mode = VerticalStripWipeMode.Clear,
            order = VerticalStripWipeOrder.LeftToRight,

            stripCount = 20,
            stripDelay = 0.02f,
            stripFillDuration = 0.08f,

            color = Color.black,

            duration = duration,
            ease = Ease.Linear,

            wait = true,
            killTween = true,
            disableWhenClear = true,
            strict = true
        };

        Collect(spec);
    }

    private void EnqueueSetupPresentationViewSpec()
    {
        var spec = new SetupPresentationViewCommandSpec
        {
            strict = true
        };

        Collect(spec);
    }

    private void EnqueueHideDialogueBoxSpec()
    {
        var spec = new HideDialogueBoxCommandSpec
        {
            hideAll = true,
            targetKind = DialogueBoxKind.Speaker,
            duration = 0.18f,
            wait = false
        };

        Collect(spec);
    }

    private void EnqueueSpawnBackgroundSpec(string bgKey, string viewPrefabKey, string stageKey)
    {
        if (string.IsNullOrWhiteSpace(bgKey))
        {
            Debug.LogError("[YarnCommandBridge] bg_spawn: bgKey is null or empty.");
            return;
        }

        if (string.IsNullOrWhiteSpace(viewPrefabKey))
        {
            Debug.LogError("[YarnCommandBridge] bg_spawn: viewPrefabKey is null or empty.");
            return;
        }

        if (!PresentationTargetParser.TryParseBackgroundStageContent(stageKey, out PresentationTarget parentTarget))
        {
            Debug.LogError($"[YarnCommandBridge] bg_spawn: Unknown stage key '{stageKey}'. Use 's0', 's1', or 's2'.");
            return;
        }

        var spec = new SpawnBackgroundCommandSpec
        {
            bgKey = bgKey.Trim(),
            viewPrefabKey = viewPrefabKey.Trim(),
            parentTarget = parentTarget
        };

        Collect(spec);
    }

    private void EnqueueSpawnBackgroundBoundSpec(
        string bgKey,
        string viewPrefabKey,
        string parentTargetName,
        string profileName)
    {
        if (string.IsNullOrWhiteSpace(bgKey))
        {
            Debug.LogError("[YarnCommandBridge] bg_spawn_bound: bgKey is null or empty.");
            return;
        }

        if (string.IsNullOrWhiteSpace(viewPrefabKey))
        {
            Debug.LogError("[YarnCommandBridge] bg_spawn_bound: viewPrefabKey is null or empty.");
            return;
        }

        if (!PresentationTargetParser.TryParseBackgroundStageContent(parentTargetName, out PresentationTarget parentTarget) &&
            !PresentationTargetParser.TryParse(parentTargetName, out parentTarget))
        {
            Debug.LogError($"[YarnCommandBridge] bg_spawn_bound: Unknown parent target '{parentTargetName}'.");
            return;
        }

        if (!TryParsePresentationResponseProfile(profileName, out PresentationResponseProfile responseProfile))
        {
            Debug.LogError($"[YarnCommandBridge] bg_spawn_bound: Unknown response profile '{profileName}'.");
            return;
        }

        var spec = new SpawnBackgroundCommandSpec
        {
            bgKey = bgKey.Trim(),
            viewPrefabKey = viewPrefabKey.Trim(),
            parentTarget = parentTarget,
            responseProfile = responseProfile
        };

        Collect(spec);
    }

    private void EnqueueSetBackgroundSpriteSpec(string bgKey, string spritePath)
    {
        if (string.IsNullOrWhiteSpace(bgKey))
        {
            Debug.LogError("[YarnCommandBridge] bg_sprite: bgKey is null or empty.");
            return;
        }

        if (string.IsNullOrWhiteSpace(spritePath))
        {
            Debug.LogError("[YarnCommandBridge] bg_sprite: spritePath is null or empty.");
            return;
        }

        Sprite sprite = Resources.Load<Sprite>(spritePath);

        if (sprite == null)
        {
            Debug.LogError($"[YarnCommandBridge] bg_sprite: Sprite not found. path='{spritePath}'");
            return;
        }

        var spec = new SetBackgroundSpriteCommandSpec
        {
            bgKey = bgKey.Trim(),
            sprite = sprite,
            setPreserveAspect = true,
            preserveAspect = true,
            setNativeSize = false,
            strict = true
        };

        Collect(spec);
    }

    private void EnqueueFadeBackgroundSpec(string bgKey, float alpha, float duration = 0.35f)
    {
        if (string.IsNullOrWhiteSpace(bgKey))
        {
            Debug.LogError("[YarnCommandBridge] bg_fade: bgKey is null or empty.");
            return;
        }

        var spec = new FadeBackgroundCommandSpec
        {
            bgKey = bgKey.Trim(),
            targetAlpha = Mathf.Clamp01(alpha),
            duration = duration,
            wait = false,
            killTween = true,
            strict = true
        };

        Collect(spec);
    }

    private void EnqueueDestroyBackgroundSpec(string bgKey = "current")
    {
        var spec = new DestroyBackgroundCommandSpec
        {
            bgKey = string.IsNullOrWhiteSpace(bgKey) ? "current" : bgKey.Trim(),
            killTween = true,
            removeRefEntry = true,
            strict = true
        };

        Collect(spec);
    }

    private void EnqueueFadeToPresentationSpec(string targetName, float alpha, float duration = 0.35f)
    {
        if (!PresentationTargetParser.TryParse(targetName, out PresentationTarget target))
        {
            Debug.LogError($"[YarnCommandBridge] fade_to: Unknown target '{targetName}'.");
            return;
        }

        var spec = new FadeToPresentationCommandSpec
        {
            target = target,
            targetAlpha = Mathf.Clamp01(alpha),
            duration = duration,
            wait = false,
            killTween = true
        };

        Collect(spec);
    }

    private void EnqueueMoveByPresentationSpec(string stageKey, float x, float y, float duration = 0.35f)
    {
        if (!PresentationTargetParser.TryParseStageRoot(stageKey, out PresentationTarget target))
        {
            Debug.LogError($"[YarnCommandBridge] move_by_p: Unknown stage key '{stageKey}'. Use 's0', 's1', or 's2'.");
            return;
        }

        var spec = new MoveByPresentationCommandSpec
        {
            target = target,
            delta = new Vector2(x, y),
            duration = duration,
            wait = false,
            killTween = true
        };

        Collect(spec);
    }

    private void EnqueueScaleToPresentationSpec(string targetName, float xyValue, float duration = 0.35f)
    {
        if (!PresentationTargetParser.TryParse(targetName, out PresentationTarget target))
        {
            Debug.LogError($"[YarnCommandBridge] scale_to_p: Unknown target '{targetName}'.");
            return;
        }

        var spec = new ScaleToPresentationCommandSpec
        {
            target = target,
            toScale = new Vector2(xyValue, xyValue),
            duration = duration,
            wait = false,
            killTween = true
        };

        Collect(spec);
    }

    private void EnqueueResetPresentationTargetSpec(string targetName)
    {
        if (!PresentationTargetParser.TryParse(targetName, out PresentationTarget target))
        {
            Debug.LogError($"[YarnCommandBridge] p_reset: Unknown target '{targetName}'.");
            return;
        }

        var spec = new ResetPresentationTargetTransformCommandSpec
        {
            target = target,
            resetAnchoredPosition = true,
            anchoredPosition = Vector2.zero,
            resetRotation = true,
            localEulerAngles = Vector3.zero,
            resetScale = true,
            localScale = Vector3.one,
            resetSizeDelta = false,
            killTween = true,
            strict = true
        };

        Collect(spec);
    }

    private void EnqueueApplyPresentationTargetOffsetSpec(string targetName, float x, float y)
    {
        if (!PresentationTargetParser.TryParse(targetName, out PresentationTarget target))
        {
            Debug.LogError($"[YarnCommandBridge] p_offset: Unknown target '{targetName}'.");
            return;
        }

        var spec = new ApplyPresentationTargetOffsetCommandSpec
        {
            target = target,
            offset = new Vector2(x, y),
            applyFromZero = true,
            killTween = true,
            strict = true
        };

        Collect(spec);
    }

    private void EnqueueMoveByPresentationTargetSpec(string targetName, float x, float y, float duration = 0.35f)
    {
        if (!PresentationTargetParser.TryParse(targetName, out PresentationTarget target))
        {
            Debug.LogError($"[YarnCommandBridge] p_move_by: Unknown target '{targetName}'.");
            return;
        }

        var spec = new MoveByPresentationTargetCommandSpec
        {
            target = target,
            delta = new Vector2(x, y),
            duration = duration,
            wait = false,
            killTween = true,
            strict = true
        };

        Collect(spec);
    }

    private void EnqueueMoveToPresentationTargetSpec(string targetName, float x, float y, float duration = 0.35f)
    {
        if (!PresentationTargetParser.TryParse(targetName, out PresentationTarget target))
        {
            Debug.LogError($"[YarnCommandBridge] p_move_to: Unknown target '{targetName}'.");
            return;
        }

        var spec = new MoveToPresentationTargetCommandSpec
        {
            target = target,
            to = new Vector2(x, y),
            overrideFrom = false,
            duration = duration,
            wait = false,
            killTween = true,
            strict = true
        };

        Collect(spec);
    }

    private void EnqueueScaleToPresentationTargetUniformSpec(string targetName, float scale, float duration = 0.35f)
    {
        if (!PresentationTargetParser.TryParse(targetName, out PresentationTarget target))
        {
            Debug.LogError($"[YarnCommandBridge] p_scale_to: Unknown target '{targetName}'.");
            return;
        }

        var spec = new ScaleToPresentationTargetCommandSpec
        {
            target = target,
            toScale = new Vector3(scale, scale, 1f),
            overrideFromScale = false,
            duration = duration,
            wait = false,
            killTween = true,
            strict = true
        };

        Collect(spec);
    }

    private void EnqueueScaleToPresentationTargetXYZSpec(
        string targetName,
        float x,
        float y,
        float z,
        float duration = 0.35f)
    {
        if (!PresentationTargetParser.TryParse(targetName, out PresentationTarget target))
        {
            Debug.LogError($"[YarnCommandBridge] p_scale_to_xyz: Unknown target '{targetName}'.");
            return;
        }

        var spec = new ScaleToPresentationTargetCommandSpec
        {
            target = target,
            toScale = new Vector3(x, y, z),
            overrideFromScale = false,
            duration = duration,
            wait = false,
            killTween = true,
            strict = true
        };

        Collect(spec);
    }

    private void EnqueueRotateToPresentationTargetSpec(
        string targetName,
        float x,
        float y,
        float z,
        float duration = 0.35f)
    {
        if (!PresentationTargetParser.TryParse(targetName, out PresentationTarget target))
        {
            Debug.LogError($"[YarnCommandBridge] p_rotate_to: Unknown target '{targetName}'.");
            return;
        }

        var spec = new RotateToPresentationTargetCommandSpec
        {
            target = target,
            toEuler = new Vector3(x, y, z),
            overrideFromEuler = false,
            duration = duration,
            wait = false,
            killTween = true,
            strict = true
        };

        Collect(spec);
    }

    private void EnqueueSlideInPresentationTargetSpec(
        string targetName,
        string directionName,
        float distance,
        float duration = 0.55f)
    {
        if (!PresentationTargetParser.TryParse(targetName, out PresentationTarget target))
        {
            Debug.LogError($"[YarnCommandBridge] p_slide_in: Unknown target '{targetName}'.");
            return;
        }

        if (!PresentationDirectionParser.TryParse(directionName, out PresentationDirection direction))
        {
            Debug.LogError(
                $"[YarnCommandBridge] p_slide_in: Unknown direction '{directionName}'. Use 'left', 'right', 'up', or 'down'.");
            return;
        }

        var spec = new SlideInPresentationTargetCommandSpec
        {
            target = target,
            direction = direction,
            distance = distance,
            duration = duration,
            ease = Ease.OutCubic,
            punch = 24f,
            wait = false,
            killTween = true,
            strict = true
        };

        Collect(spec);
    }
    
    private void EnqueueSlantedMaskCutInSpec(
        string targetName,
        float duration = 0.65f)
    {
        if (!PresentationTargetParser.TryParse(targetName, out PresentationTarget target))
        {
            Debug.LogError($"[YarnCommandBridge] p_slant_mask_in: Unknown target '{targetName}'.");
            return;
        }

        var spec = new SlantedMaskSlideInCommandSpec
        {
            target = target,

            fromOffset = new Vector2(-2200f, 0f),
            toOffset = new Vector2(-770f, 0f),

            slantToRight = false,
            flipVertical = true,

            duration = duration,
            ease = Ease.OutCubic,

            overshootPixels = 72f,
            overshootStart = 0.72f,

            wait = false,
            killTween = true,
            strict = true
        };

        Collect(spec);
    }
    
    private void EnqueueSlantedMaskCutOutSpec(
        string targetName,
        float duration = 0.45f)
    {
        if (!PresentationTargetParser.TryParse(targetName, out PresentationTarget target))
        {
            Debug.LogError($"[YarnCommandBridge] p_slant_cut_out: Unknown target '{targetName}'.");
            return;
        }

        var spec = new SlantedMaskSlideOutCommandSpec
        {
            target = target,

            fromOffset = new Vector2(-770f, 0f),
            toOffset = new Vector2(-2200f, 0f),

            slantToRight = false,
            flipVertical = true,

            duration = duration,
            ease = Ease.InCubic,

            pullPixels = 0f,
            pullEnd = 0.28f,

            wait = false,
            killTween = true,
            strict = true
        };

        Collect(spec);
    }

    private bool TryParsePresentationResponseProfile(string raw, out PresentationResponseProfile profile)
    {
        profile = null;

        if (string.IsNullOrWhiteSpace(raw))
            return false;

        string s = raw.Trim().ToLowerInvariant();

        switch (s)
        {
            case "background":
            case "bg":
                profile = PresentationResponseProfile.Background;
                return true;

            case "prop":
                profile = PresentationResponseProfile.Prop;
                return true;

            case "characterslot":
            case "character_slot":
            case "char":
            case "slot":
                profile = PresentationResponseProfile.CharacterSlot;
                return true;

            case "foreground":
            case "fg":
                profile = PresentationResponseProfile.Foreground;
                return true;
        }

        return false;
    }
}