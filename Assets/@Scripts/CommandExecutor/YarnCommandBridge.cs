using UnityEngine;
using Yarn.Unity;

public sealed class YarnCommandBridge : MonoBehaviour
{
    private DialogueRunner _dialogueRunner;
    private ImmediateCommandRunner _runner;

    public void Initialize(DialogueRunner dialogueRunner, ImmediateCommandRunner runner)
    {
        _dialogueRunner = dialogueRunner;
        _runner = runner;
        
        _dialogueRunner.AddCommandHandler<string>("char_rig", SetCharRig);
        _dialogueRunner.AddCommandHandler<string, int>("anchor", SetAnchorPosition);
        
        _dialogueRunner.AddCommandHandler<string, float, float>("originsize", SetOriginSize);
        
        _dialogueRunner.AddCommandHandler<string, string, string, string>("portrait", SetPortrait);

        _dialogueRunner.AddCommandHandler<string, string>("slide_in", SlideIn);
        _dialogueRunner.AddCommandHandler<string>("slide_out", SlideOut);

        _dialogueRunner.AddCommandHandler<string>("fade_in", FadeIn);
        _dialogueRunner.AddCommandHandler<float>("fade_out", FadeOut);

        _dialogueRunner.AddCommandHandler<string, float>("shake", Shake);
        _dialogueRunner.AddCommandHandler<string, string>("emoji", ShowEmoji);
    }

    // ──────────────────────────────────────────────────
    // 캐릭터 Rig
    // ──────────────────────────────────────────────────

    public GameObject _rigPrefab;
    public void SetCharRig(string roleKey)
    {
        if (string.IsNullOrWhiteSpace(roleKey))
        {
            Debug.LogError("[YarnCommandBridge] char_rig: roleKey is null or empty.");
            return;
        }

        var spec = new SetCharRigCommandSpec
        {
            roleKey = roleKey,
            rigPrefab = _rigPrefab
        };

        _runner.Run(spec);
    }
    
    
    public CharStageTuningSO globalTuning;

    public void SetAnchorPosition(string roleKey, int positionPreset)
    {
        if (string.IsNullOrWhiteSpace(roleKey))
        {
            Debug.LogError("[YarnCommandBridge] anchor: roleKey is null or empty.");
            return;
        }

        RectAnchorPreset3CharR preset = positionPreset switch
        {
            1 => RectAnchorPreset3CharR.Left,
            2 => RectAnchorPreset3CharR.Center,
            3 => RectAnchorPreset3CharR.Right,
            _ => RectAnchorPreset3CharR.Center
        };

        var spec = new SetAnchorCommandSpecCharR
        {
            roleKey = roleKey.Trim(),
            preset = preset,
            globalTuning = globalTuning
        };

        _runner.Run(spec);
    }
    // ──────────────────────────────────────────────────
    // 포트레이트
    // ──────────────────────────────────────────────────

    public void SetOriginSize(string roleKey, float x, float y)
    {
        var spec = new SetOriginSizeCommandSpecCharR
        {
            roleKey = roleKey,
            toScale = new Vector2(x,y)
        };
        
        _runner.Run(spec);
    }

    // ──────────────────────────────────────────────────
    // 포트레이트
    // ──────────────────────────────────────────────────

    public void SetPortrait(string roleKey, string character, string variant, string emotion)
    {
        var portraitIdentity = new PortraitIdentity
        {
            character = character,
            variant = variant,
            emotion = emotion
        };
        
        var spec = new SetPortraitSpriteCommandSpecCharR
        {
            roleKey = roleKey,
            portrait = portraitIdentity
        };
        
        _runner.Run(spec);
    }

    // ──────────────────────────────────────────────────
    // 슬라이드 인
    // ──────────────────────────────────────────────────

    public Coroutine SlideIn(string roleKey, string direction = "left")
    {
        var spec = new SlideInCommandSpecCharR
        {
            roleKey = roleKey,
        };
        
        return _runner.Run(spec);
    }

    public Coroutine SlideOut(string rigId)
    {
        var spec = new SlideOutCommandSpecCharR {};
        return _runner.Run(spec, blocking: true);
    }

    // ──────────────────────────────────────────────────
    // 페이드
    // ──────────────────────────────────────────────────

    public Coroutine FadeIn(string roleKey)
    {
        var spec = new FadeInCommandSpecCharR
        {
            roleKey = roleKey
        };
        
        return _runner.Run(spec, blocking: true);
    }

    public Coroutine FadeOut(float duration = 0.4f)
    {
        var spec = new FadeOutCommandSpecCharR { duration = duration };
        return _runner.Run(spec, blocking: true);
    }

    // ──────────────────────────────────────────────────
    // 감정/이펙트 (non-blocking — 연출과 동시에 진행)
    // ──────────────────────────────────────────────────

    public void Shake(string rigId, float intensity = 1f)
    {
        var spec = new ShakeCommandSpecCharR
        {
        };
        _runner.Run(spec, blocking: false);  // 대사와 동시 실행
    }

    public void ShowEmoji(string rigId, string emojiId)
    {
        var spec = new ShowEmojiCommandSpecCharR
        { };
        _runner.Run(spec, blocking: false);
    }
}