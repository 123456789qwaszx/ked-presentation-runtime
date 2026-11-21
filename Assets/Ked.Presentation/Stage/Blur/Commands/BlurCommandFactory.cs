using System;

// 블러의 커맨드 — 초점(depth_focus_on · depth_focus_clear)과 층 블러(stage_blur · stage_blur_reset).
// 둘 다 장면 전역 의도라 커맨드는 RigBlurIntentSystem에 의도만 넘기고, 리그마다 블러를 정해 줄 머리에서 다시 맞추는 것은 시스템이 한다.
// 화면 효과 리그(덮개 · 플래시 · 노이즈 · 비네트)가 아니다 — 캐릭터 리그마다 거는 셰이더 블러다(Documentation~/BLUR-DECISIONS.md).
public sealed class BlurCommandFactory : INodeCommandFactory
{
    private readonly RigBlurIntentSystem _rigBlur;

    public BlurCommandFactory(RigBlurIntentSystem rigBlur)
    {
        _rigBlur = rigBlur ?? throw new ArgumentNullException(nameof(rigBlur));
    }

    public bool TryCreate(CommandSpecBase spec, out ISequenceCommand command)
    {
        command = spec switch
        {
            // 초점 — 지명한 배역 밖을 깊이 차이에 비례해 흐린다
            DepthFocusCommandSpecCharR s => new DepthFocusCommandCharR(_rigBlur, s),

            // 층 블러 — 그 무대 · 층 아래 리그를 함께 흐린다
            StageLayerBlurCommandSpec s => new StageLayerBlurCommand(_rigBlur, s),

            _ => null
        };

        return command != null;
    }
}
