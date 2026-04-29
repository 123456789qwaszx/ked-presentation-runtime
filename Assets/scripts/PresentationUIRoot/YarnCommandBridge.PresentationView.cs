using UnityEngine;
using Yarn.Unity;

public sealed partial class YarnCommandBridge
{
    [Header("Presentation")]
    [Tooltip("SetBackgroundSprite Yarn 명령에서 Resources.Load<Sprite>()로 사용할 기본 prefix. 예: 'ui/bg/'")]
    public string backgroundSpriteResourcesPrefix = "";

    public void RegisterPresentationCommands()
    {
        _dialogueRunner.AddCommandHandler("presentation_setup", EnqueueSetupPresentationViewSpec);

        _dialogueRunner.AddCommandHandler<string, string>("bg_spawn", EnqueueSpawnBackgroundSpec);
        _dialogueRunner.AddCommandHandler<string, string>("bg_sprite", EnqueueSetBackgroundSpriteSpec);
        _dialogueRunner.AddCommandHandler<string>("bg_destroy", EnqueueDestroyBackgroundSpec);
        _dialogueRunner.AddCommandHandler<string, float, float>("bg_fade", EnqueueFadeBackgroundSpec);

        _dialogueRunner.AddCommandHandler<string, string>("dlg_spawn", EnqueueSpawnDialogueBoxSpec);
        _dialogueRunner.AddCommandHandler<string, float, float>("dlg_fade", EnqueueFadeDialogueBoxSpec);
        _dialogueRunner.AddCommandHandler<string, string>("dlg_text", EnqueueSetDialogueBoxBodyTextSpec);
        _dialogueRunner.AddCommandHandler<string, string, string>("dlg_text_name",
            EnqueueSetDialogueBoxTextWithNameSpec);
        _dialogueRunner.AddCommandHandler<string>("dlg_destroy", EnqueueDestroyDialogueBoxSpec);

        _dialogueRunner.AddCommandHandler<string, float, float>("fade_to", EnqueueFadeToPresentationSpec);
        _dialogueRunner.AddCommandHandler<string, float, float, float>("move_by_p", EnqueueMoveByPresentationSpec);
        _dialogueRunner.AddCommandHandler<string, float, float>("scale_to_p", EnqueueScaleToPresentationSpec);
    }

    private void EnqueueSpawnDialogueBoxSpec(string dialogueKey, string viewPrefabKey)
    {
        var spec = new SpawnDialogueBoxCommandSpec
        {
            dialogueKey = string.IsNullOrWhiteSpace(dialogueKey) ? "main" : dialogueKey.Trim(),
            viewPrefabKey = string.IsNullOrWhiteSpace(viewPrefabKey) ? "default" : viewPrefabKey.Trim(),
            parentTarget = PresentationTarget.DialogueBox_Root,
            initialAlpha = 1f,
            strict = true
        };

        Collect(spec);
    }

    private void EnqueueFadeDialogueBoxSpec(string dialogueKey, float alpha, float duration = 0.25f)
    {
        var spec = new FadeDialogueBoxCommandSpec
        {
            dialogueKey = string.IsNullOrWhiteSpace(dialogueKey) ? "main" : dialogueKey.Trim(),
            targetAlpha = Mathf.Clamp01(alpha),
            duration = duration,
            wait = false,
            killTween = true,
            strict = true
        };

        Collect(spec);
    }

    private void EnqueueSetDialogueBoxBodyTextSpec(string dialogueKey, string bodyText)
    {
        var spec = new SetDialogueBoxTextCommandSpec
        {
            dialogueKey = string.IsNullOrWhiteSpace(dialogueKey) ? "main" : dialogueKey.Trim(),
            bodyText = bodyText ?? string.Empty,
            setNameText = false,
            strict = true
        };

        Collect(spec);
    }

    private void EnqueueSetDialogueBoxTextWithNameSpec(string dialogueKey, string nameText, string bodyText)
    {
        var spec = new SetDialogueBoxTextCommandSpec
        {
            dialogueKey = string.IsNullOrWhiteSpace(dialogueKey) ? "main" : dialogueKey.Trim(),
            bodyText = bodyText ?? string.Empty,
            setNameText = true,
            nameText = nameText ?? string.Empty,
            strict = true
        };

        Collect(spec);
    }

    private void EnqueueDestroyDialogueBoxSpec(string dialogueKey = "main")
    {
        var spec = new DestroyDialogueBoxCommandSpec
        {
            dialogueKey = string.IsNullOrWhiteSpace(dialogueKey) ? "main" : dialogueKey.Trim(),
            killTween = true,
            removeRefEntry = true,
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
                target = PresentationTarget.Stage_Root;
                return true;

            case "backgroundsystem":
            case "backgroundsystem_root":
                target = PresentationTarget.BackgroundSystem_Root;
                return true;

            case "bgshot":
            case "bgshot_root":
            case "bg":
                target = PresentationTarget.BGShot_Root;
                return true;

            case "bgcontent":
            case "bgcontent_root":
                target = PresentationTarget.BGContent_Root;
                return true;

            case "bgoverlay":
            case "bgoverlay_root":
                target = PresentationTarget.BGOverlay_Root;
                return true;

            case "charactersystem":
            case "charactersystem_root":
                target = PresentationTarget.CharacterSystem_Root;
                return true;

            case "foreground":
            case "foreground_root":
                target = PresentationTarget.Foreground_Root;
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