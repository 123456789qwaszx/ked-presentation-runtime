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
        
        _dialogueRunner.AddCommandHandler<string, string, string, string>(
            "portrait",
            SetPortrait
        );

        _dialogueRunner.AddCommandHandler<string, string>("slide_in", SlideIn);
        _dialogueRunner.AddCommandHandler<string>("slide_out", SlideOut);

        _dialogueRunner.AddCommandHandler<float>("fade_in", FadeIn);
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

    public Coroutine SlideIn(string rigId, string direction = "left")
    {
        var spec = new SlideInCommandSpecCharR
        {
        };
        return _runner.Run(spec, blocking: true);  // 연출 끝날 때까지 Yarn 대기
    }

    public Coroutine SlideOut(string rigId)
    {
        var spec = new SlideOutCommandSpecCharR {};
        return _runner.Run(spec, blocking: true);
    }

    // ──────────────────────────────────────────────────
    // 페이드
    // ──────────────────────────────────────────────────

    public Coroutine FadeIn(float duration = 0.4f)
    {
        var spec = new FadeInCommandSpecCharR { duration = duration };
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