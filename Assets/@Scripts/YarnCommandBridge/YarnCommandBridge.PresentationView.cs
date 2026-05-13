using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    public void RegisterPresentationCommands()
    {
        _dialogueRunner.AddCommandHandler("presentation_setup", EnqueueSetupPresentationViewSpec);

        _dialogueRunner.AddCommandHandler<string, string>("bg_spawn", EnqueueSpawnBackgroundSpec);
        _dialogueRunner.AddCommandHandler<string, string, string, string>("bg_spawn_bound", EnqueueSpawnBackgroundBoundSpec);
        _dialogueRunner.AddCommandHandler<string, string>("bg_sprite", EnqueueSetBackgroundSpriteSpec);
        _dialogueRunner.AddCommandHandler<string>("bg_destroy", EnqueueDestroyBackgroundSpec);
        _dialogueRunner.AddCommandHandler<string, float, float>("bg_fade", EnqueueFadeBackgroundSpec);
        
        _dialogueRunner.AddCommandHandler<string, float, float>("fade_to", EnqueueFadeToPresentationSpec);
        _dialogueRunner.AddCommandHandler<string, float, float, float>("move_by_p", EnqueueMoveByPresentationSpec);
        _dialogueRunner.AddCommandHandler<string, float, float>("scale_to_p", EnqueueScaleToPresentationSpec);
        
        _dialogueRunner.AddCommandHandler("box_hide", EnqueueHideDialogueBoxSpec);
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

    private void EnqueueSetupPresentationViewSpec()
    {
        var spec = new SetupPresentationViewCommandSpec
        {
            strict = true
        };

        Collect(spec);
    }

    private void EnqueueSpawnBackgroundSpec(string bgKey, string viewPrefabKey)
    {
        var spec = new SpawnBackgroundCommandSpec
        {
            bgKey = bgKey.Trim(),
            viewPrefabKey = viewPrefabKey.Trim()
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

        if (!TryParsePresentationTarget(parentTargetName, out PresentationTarget parentTarget))
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
        if (!TryParsePresentationTarget(targetName, out PresentationTarget target))
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

    private void EnqueueMoveByPresentationSpec(string targetName, float x, float y, float duration = 0.35f)
    {
        if (!TryParsePresentationTarget(targetName, out PresentationTarget target))
        {
            Debug.LogError($"[YarnCommandBridge] move_by_p: Unknown target '{targetName}'.");
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
        if (!TryParsePresentationTarget(targetName, out PresentationTarget target))
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

    private bool TryParsePresentationTarget(string raw, out PresentationTarget target)
    {
        target = default;

        if (string.IsNullOrWhiteSpace(raw))
            return false;

        string s = raw.Trim();

        switch (s.ToLowerInvariant())
        {
            case "fullscreenfade":
            case "fullscreenfade_root":
            case "fade":
            case "black":
                target = PresentationTarget.FullscreenFade_Root;
                return true;

            case "letterbox":
            case "letterbox_root":
                target = PresentationTarget.Letterbox_Root;
                return true;

            case "flash":
            case "flash_root":
                target = PresentationTarget.Flash_Root;
                return true;

            case "screenoverlay":
            case "screenoverlay_root":
            case "overlay":
                target = PresentationTarget.ScreenOverlay_Root;
                return true;

            case "stageshot":
            case "stageshot_root":
                target = PresentationTarget.StageShot_Root;
                return true;

            case "stagepan":
            case "stagepan_root":
            case "pan":
                target = PresentationTarget.StagePan_Root;
                return true;

            case "stagezoom":
            case "stagezoom_root":
            case "zoom":
                target = PresentationTarget.StageZoom_Root;
                return true;

            case "stage":
            case "stage_root":
                target = PresentationTarget.Stage00_Root;
                return true;

            case "backgroundsystem":
            case "backgroundsystem_root":
                target = PresentationTarget.Stage00BackgroundSystem_Root;
                return true;

            case "bgshot":
            case "bgshot_root":
            case "bg":
                target = PresentationTarget.Stage00BGShot_Root;
                return true;

            case "bgcontent":
            case "bgcontent_root":
                target = PresentationTarget.Stage00BGContent_Root;
                return true;

            case "bgoverlay":
            case "bgoverlay_root":
                target = PresentationTarget.Stage00BGOverlay_Root;
                return true;

            case "charactersystem":
            case "charactersystem_root":
                target = PresentationTarget.Stage00CharacterSystem_Root;
                return true;

            case "foreground":
            case "foreground_root":
                target = PresentationTarget.Stage00Foreground_Root;
                return true;

            case "dialogueui":
            case "dialogueui_root":
                target = PresentationTarget.DialogueUI_Root;
                return true;

            case "dialoguebox":
            case "dialoguebox_root":
                target = PresentationTarget.DialogueBox_Root;
                return true;

            case "namebox":
            case "namebox_root":
                target = PresentationTarget.NameBox_Root;
                return true;

            case "narrationbox":
            case "narrationbox_root":
                target = PresentationTarget.NarrationBox_Root;
                return true;

            case "choice":
            case "choice_root":
                target = PresentationTarget.Choice_Root;
                return true;

            case "systemui":
            case "systemui_root":
                target = PresentationTarget.SystemUI_Root;
                return true;
        }

        return System.Enum.TryParse(s, true, out target);
    }
}